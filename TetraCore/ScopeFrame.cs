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

using System.Text;
using TetraCore.Exceptions;

namespace TetraCore;

/// <summary>
/// Represents a single stack frame in the Tetra virtual machine, responsible for storing and resolving variables.
/// Variables are stored in fixed slots indexed by <see cref="VarName.Slot"/> and may optionally support vector-style indexing.
/// Each frame maintains a reference to an optional parent scope, allowing variable shadowing and resolution through chained scopes.
/// </summary>
public class ScopeFrame
{
    public const int MaxSlots = 32;
    public const int RetvalSlot = 0;

    private readonly ScopeFrame m_parent;
    private readonly Operand[] m_slots = new Operand[MaxSlots];
    
    public Operand Retval
    {
        get => m_slots[0];
        set => m_slots[0] = value;
    }

    public ScopeFrame(ScopeFrame parent = null)
    {
        m_parent = parent;
    }

    public bool IsDefined(VarName name) =>
        m_slots[name.Slot] != null || (m_parent?.IsDefined(name) ?? false);

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
        if (!varName.ArrIndex.HasValue)
        {
            m_slots[varName.Slot] = value;
            return;
        }

        var v = m_slots[varName.Slot];
        if (v.Type != OperandType.Vector)
            throw new RuntimeException($"Cannot apply subscript to non-vector type: {varName} ({v.Type})");

        v.Floats[varName.ArrIndex.Value] = value.AsFloat();
    }

    /// <summary>
    /// Sets the value of an existing variable in this scope.
    /// </summary>
    public void SetVariable(VarName varName, Operand value, bool defineIfMissing = false)
    {
        var isLocal = m_slots[varName.Slot] != null;
        var definedInParent = !isLocal && m_parent?.IsDefined(varName) == true;

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

    public Operand GetVariable(VarName varName)
    {
        var frame = this;
        while (frame != null)
        {
            var variable = frame.m_slots[varName.Slot];
            if (variable != null)
            {
                if (!varName.ArrIndex.HasValue)
                    return variable;

                if (variable.Type != OperandType.Vector)
                    throw new RuntimeException($"Cannot apply subscript to non-vector type: {varName} ({variable.Type})");
                return new Operand(variable.Floats[varName.ArrIndex.Value]);
            }

            frame = frame.m_parent;
        }

        throw new RuntimeException($"Variable '{varName}' is not defined.");
    }

    public override string ToString() =>
        ToUiString(null);

    public string ToUiString(SymbolTable symbolTable)
    {
        var sb = new StringBuilder();
        if (m_parent != null)
        {
            sb.Append(m_parent);
            sb.AppendLine("---");
        }

        for (var slotIndex = 0; slotIndex < m_slots.Length; slotIndex++)
        {
            if (m_slots[slotIndex] == null)
                continue;
            var variable = m_slots[slotIndex];
            
            var varName = symbolTable?[slotIndex] ?? $"${slotIndex}";
            sb.AppendLine($"{varName} = {variable}");
        }
        if (Retval != null)
            sb.AppendLine($"retval = {Retval}");

        return sb.ToString();
    }
}