// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using TetraCore.Exceptions;

namespace TetraCore;

/// <summary>
/// Represents a single stack frame in the Tetra virtual machine, responsible for storing and resolving variables.
/// Variables are stored in fixed slots indexed by <see cref="VarName.Slot"/> and may optionally support vector-style indexing.
/// Each frame maintains a reference to an optional parent scope, allowing variable shadowing and resolution through chained scopes.
/// </summary>
[DebuggerDisplay("{m_scopeType}")]
public class ScopeFrame
{
    public const int MaxSlots = 256;
    public const int RetvalSlot = 0;

    private static readonly Dictionary<char, int> SwizzleMap = new Dictionary<char, int>
    {
        {'x', 0},
        {'y', 1},
        {'z', 2},
        {'w', 3},
        {'r', 0},
        {'g', 1},
        {'b', 2},
        {'a', 3},
        {'s', 0},
        {'t', 1},
        {'p', 2},
        {'q', 3}
    };
    
    private readonly ScopeType m_scopeType;
    private readonly ScopeFrame m_parent;
    private readonly Operand[] m_slots = new Operand[MaxSlots];

    public bool IsRoot => m_scopeType == ScopeType.Global;
    
    public Operand Retval
    {
        get => m_slots[0];
        set => m_slots[0] = value;
    }

    public ScopeFrame CallerFrame
    {
        get
        {
            // Find root scope for the current function.
            var frame = this;
            while (frame.m_scopeType != ScopeType.Function)
            {
                frame = frame.m_parent;
                if (frame == null)
                    return null;
            }
            
            return frame.m_parent;
        }
    }
    
    public ScopeFrame()
    {
        m_scopeType = ScopeType.Global;
    }

    public ScopeFrame(ScopeType scopeType, [NotNull] ScopeFrame parent)
    {
        m_scopeType = scopeType;
        m_parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    public bool IsDefined(VarName name)
    {
        if (IsDefinedLocally(name))
            return true; // Found locally.
        
        if (m_parent == null)
            return false; // No parent, so not defined.

        if (m_scopeType == ScopeType.Function)
        {
            // Functions can't access variables in a parent scope - So jump to the root(/globals).
            return FindRoot()?.IsDefined(name) ?? false;
        }
        
        return m_parent?.IsDefined(name) ?? false;
    }

    public bool IsDefinedLocally(VarName name) =>
        m_slots[name.Slot] != null;

    /// <summary>
    /// It's dangerous for the same operand to be stored twice,
    /// as a modification to one instance will change others too.
    /// </summary>
    private bool IsReferenced(Operand operand)
    {
        if (m_slots.Any(o => o?.Floats == operand.Floats))
            return true; // Found locally.
        
        if (m_parent == null)
            return false; // No parent, so not defined.
        
        return m_parent?.IsReferenced(operand) ?? false;
    }

    private ScopeFrame FindRoot()
    {
        var frame = this;
        while (!frame.IsRoot && frame.m_parent != null)
            frame = frame.m_parent;
        return frame;
    }
    
    /// <summary>
    /// Creates a new variable in this scope.
    /// </summary>
    public void DefineVariable(VarName varName, Operand value)
    {
        // Array index specified, so the variable must already be defined to continue.
        if (varName.ArrIndex.HasValue && !IsDefined(varName))
            throw new RuntimeException($"Cannot define variable name containing array index: {varName}");

        // The value being set must be constant.
        if (value.Type == OperandType.Variable)
            value = GetVariable(value.Name);

        SetValue(varName, value);
    }

    private void SetValue(VarName varName, Operand value)
    {
        if (float.IsNaN(value.Float))
            throw new RuntimeException("Cannot assign NaN to a variable.");
        if (float.IsInfinity(value.Float))
            throw new RuntimeException("Cannot assign infinity to a variable.");

        var hasArrayIndex = varName.ArrIndex.HasValue;
        if (!hasArrayIndex && varName.Swizzle == null)
        {
            // Straight variable assignment - Simples.
            m_slots[varName.Slot] = value;
            return;
        }

        // We have to assign to a vector component.
        var v = m_slots[varName.Slot];
        if (v.Type != OperandType.Vector)
            throw new RuntimeException($"Subscript/swizzles requires a vector type: {varName} ({v.Type})");

        if (hasArrayIndex)
        {
            // Assignment to an array index.
            v.Floats[varName.ArrIndex.Value] = value.AsFloat();
            return;
        }
        
        // Assignment to swizzle.
        for (var i = 0; i < varName.Swizzle.Length; i++)
        {
            var swizzleIndex = SwizzleMap[varName.Swizzle[i]];
            v.Floats[swizzleIndex] = value.Floats[Math.Min(i, value.Floats.Length - 1)];
        }
    }

    /// <summary>
    /// Sets the value of an existing variable in this scope.
    /// </summary>
    public void SetVariable(VarName varName, Operand value, bool defineIfMissing = false)
    {
#if DEBUG
        if (IsReferenced(value))
            throw new InvalidOperationException($"Cannot assign variable '{varName}' as it is already referenced.");
#endif

        var isLocal = m_slots[varName.Slot] != null;
        var definedInParent = !isLocal && IsDefined(varName);

        if (!isLocal && !defineIfMissing && !definedInParent)
            throw new RuntimeException($"Variable '{varName}' is undefined.");
        if (varName.Slot == value.Name?.Slot)
            throw new RuntimeException($"Cannot assign variable '{varName}' to itself.");

        // The value being set must be constant.
        if (value.Type == OperandType.Variable)
            value = GetVariable(value.Name);

        // If variable is set in a parent scope, we need to set it there.
        if (definedInParent)
        {
            m_parent.SetVariable(varName, value);
            return;
        }
        
        // Otherwise, we set the variable in this scope.
        SetValue(varName, value);
    }

    public Operand GetVariable(VarName varName, SymbolTable symbolTable = null, bool allowUndefined = false)
    {
        var frame = this;
        while (frame != null)
        {
            var variable = frame.m_slots[varName.Slot];
            if (variable != null)
            {
                if (!allowUndefined && variable.IsUnassigned)
                    throw new RuntimeException($"Cannot use unassigned operand: {varName}");

                if (!varName.ArrIndex.HasValue)
                    return variable;

                if (variable.Type != OperandType.Vector)
                    throw new RuntimeException($"Cannot apply subscript to non-vector type: {varName} ({variable.Type})");
                if (variable.Floats.Length <= varName.ArrIndex.Value || varName.ArrIndex.Value < 0)
                    throw new RuntimeException("Index was outside the bounds of the array.");
                return new Operand(variable.Floats[varName.ArrIndex.Value]);
            }

            frame = frame.m_parent;
        }

        throw new RuntimeException($"Variable '{varName.ToUiString(symbolTable)}' is not defined.");
    }

    public override string ToString() =>
        ToUiString(null);

    public IEnumerable<(ScopeType scopeType, string name, string value)> GetVariables(SymbolTable symbolTable)
    {
        for (var slotIndex = 0; slotIndex < m_slots.Length; slotIndex++)
        {
            if (m_slots[slotIndex] == null)
                continue;
            var variable = m_slots[slotIndex];

            symbolTable.TryGetValue(slotIndex, out var varName);
            yield return (m_scopeType, varName ?? $"${slotIndex}", variable.ToUiString(symbolTable));
        }
        
        if (Retval != null)
            yield return (m_scopeType, "retval", Retval.ToUiString(symbolTable));

        var next = m_scopeType == ScopeType.Function ? FindRoot() : m_parent;
        if (next == null)
            yield break;
        foreach (var valueTuple in next.GetVariables(symbolTable))
            yield return valueTuple;
    }

    public string ToUiString(SymbolTable symbolTable)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{(m_scopeType == ScopeType.Function ? "Locals" : "Globals")}]");
        
        var localVars = new List<string>();
        for (var slotIndex = 0; slotIndex < m_slots.Length; slotIndex++)
        {
            if (m_slots[slotIndex] == null)
                continue;
            var variable = m_slots[slotIndex];

            symbolTable.TryGetValue(slotIndex, out var varName);
            varName ??= $"${slotIndex}";
            localVars.Add($"{varName} = {variable}");
        }
        if (Retval != null)
            sb.AppendLine($"retval = {Retval}");
        
        localVars.Sort();
        foreach (var localVar in localVars)
            sb.AppendLine(localVar);

        ScopeFrame next;
        if (m_scopeType == ScopeType.Function)
        {
            // Skip to the root(/globals).
            next = FindRoot();
        }
        else
        {
            next = m_parent;
        }

        if (next != null)
        {
            sb.AppendLine("---");
            sb.Append(next.ToUiString(symbolTable));
        }

        return sb.ToString();
    }
}