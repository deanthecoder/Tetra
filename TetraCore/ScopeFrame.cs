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
    private readonly Dictionary<string, Operand> m_variables = [];
    
    public bool IsDefined(string name) =>
        m_variables.ContainsKey(name);
    
    public void SetVariable(string name, Operand value)
    {
        if (name == value.Name)
            throw new RuntimeException($"Cannot assign variable '{name}' to itself.");
        if (value.Type == OperandType.Variable)
            value = GetVariable(value.Name);
        m_variables[name] = value;
    }

    public Operand GetVariable(string name)
    {
        if (!m_variables.TryGetValue(name, out var variable))
            throw new RuntimeException($"Variable '{name}' not found in scope.");
        return variable;
    }
}