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

using TetraCore.Exceptions;

namespace TetraCore;

/// <summary>
/// ScopeFrame represents a single variable scope in the Tetra virtual machine.
/// Variables are stored by name, and each frame may shadow variables in lower frames on the stack.
/// </summary>
public class ScopeFrame
{
    private readonly ScopeFrame m_parent;
    private readonly Dictionary<string, Operand> m_variables = [];

    public ScopeFrame(ScopeFrame parent = null)
    {
        m_parent = parent;
    }
    
    public bool IsDefined(string name) =>
        m_variables.ContainsKey(name) || (m_parent?.IsDefined(name) ?? false);
    
    /// <summary>
    /// Creates a new variable in this scope.
    /// </summary>
    public void DefineVariable(string name, Operand value)
    {
        // The value being set must be constant.
        if (value.Type == OperandType.Variable)
            value = GetVariable(value.Name);

        m_variables[name] = value;
    }

    /// <summary>
    /// Sets the value of an existing variable in this scope.
    /// </summary>
    public void SetVariable(string name, Operand value)
    {
        if (!IsDefined(name))
            throw new RuntimeException($"Variable '{name}' is undefined.");
        if (name == value.Name)
            throw new RuntimeException($"Cannot assign variable '{name}' to itself.");

        // The value being set must be constant.
        if (value.Type == OperandType.Variable)
            value = GetVariable(value.Name);

        // If variable is set in a parent scope, we need to set it there.
        if (!m_variables.ContainsKey(name))
        {
            m_parent.SetVariable(name, value);
            return;
        }
        
        // Otherwise, we set the variable in this scope.
        m_variables[name] = value;
    }

    public Operand GetVariable(string name)
    {
        if (!IsDefined(name))
            throw new RuntimeException($"Variable '{name}' not found in scope.");
        if (!m_variables.TryGetValue(name, out var variable))
            variable = m_parent.GetVariable(name);
        return variable;
    }
}