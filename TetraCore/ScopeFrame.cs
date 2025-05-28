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
/// ScopeFrame represents a single variable scope in the Tetra virtual machine.
/// Variables are stored by name, and each frame may shadow variables in lower frames on the stack.
/// </summary>
public class ScopeFrame
{
    private readonly ScopeFrame m_parent;
    private readonly Dictionary<string, Operand> m_variables = new Dictionary<string, Operand>(8);

    public ScopeFrame(ScopeFrame parent = null)
    {
        m_parent = parent;
    }
    
    public bool IsDefined(VarName name) =>
        m_variables.ContainsKey(name.Name) || (m_parent?.IsDefined(name) ?? false);

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
            m_variables[varName.Name] = value;
            return;
        }

        var v = m_variables[varName.Name];
        if (v.Type != OperandType.Vector)
            throw new RuntimeException($"Cannot apply subscript to non-vector type: {varName}");

        v.Floats[varName.ArrIndex.Value] = value.AsFloat();
    }

    /// <summary>
    /// Sets the value of an existing variable in this scope.
    /// </summary>
    public void SetVariable(VarName varName, Operand value, bool defineIfMissing = false)
    {
        var isLocal = m_variables.ContainsKey(varName.Name);
        
        if (!isLocal && !defineIfMissing && !IsDefined(varName))
            throw new RuntimeException($"Variable '{varName}' is undefined.");
        if (varName.Name == value.Name?.Name)
            throw new RuntimeException($"Cannot assign variable '{varName}' to itself.");

        // The value being set must be constant.
        if (value.Type == OperandType.Variable)
            value = GetVariable(value.Name);

        // If variable is set in a parent scope, we need to set it there.
        if (!isLocal && m_parent?.IsDefined(varName) == true)
        {
            m_parent.SetVariable(varName, value);
            return;
        }
        
        // Otherwise, we set the variable in this scope.
        SetValue(varName, value);
    }

    public Operand GetVariable(VarName varName)
    {
        var isLocal = m_variables.TryGetValue(varName.Name, out var variable);
        if (!isLocal)
        {
            if (m_parent == null)
                throw new RuntimeException($"Variable '{varName.Name}' not found in scope.");
            return m_parent.GetVariable(varName);
        }

        if (!varName.ArrIndex.HasValue)
            return variable;
        
        if (variable.Type != OperandType.Vector)
            throw new RuntimeException($"'{varName}': Variable ({variable.Type}) is not a vector.");
        return new Operand(variable.Floats[varName.ArrIndex.Value]);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (m_parent != null)
        {
            sb.Append(m_parent);
            sb.AppendLine("---");
        }
        
        foreach (var kvp in m_variables)
            sb.AppendLine($"{kvp.Key} = {kvp.Value}");
        
        return sb.ToString();
    }
}