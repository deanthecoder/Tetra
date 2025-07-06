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
using System.Text.RegularExpressions;

namespace TetraCore;

public class DebugSnap
{
    private readonly string m_inFunction;
    private readonly List<(ScopeType scopeType, string name, string value)> m_variables;
    private readonly Instruction m_instruction;

    public DebugSnap(Program program, int ip, Stack<(int functionLabel, int returnIp, int scopeFrameDepth)> callStack, ScopeFrame scopeFrame)
    {
        m_inFunction = callStack.Count > 0 ? $"{program.LabelTable.GetLabelFromInstructionPointer(callStack.Peek().functionLabel)}()" : "<Root>";
        m_variables = scopeFrame.GetVariables(program.SymbolTable).ToList();
        m_instruction = ip < program.Instructions.Length ? program.Instructions[ip] : new Instruction();
    }

    public string GetDiff(DebugSnap newSnap)
    {
        var sb = new StringBuilder();
        var instrStr = Regex.Replace(m_instruction.ToString(), @"^\[Line\s+\d+\]\s*", string.Empty);
        sb.AppendLine($">> {instrStr,-32} {m_inFunction}:{m_instruction.LineNumber}");

        var variables = new List<(ScopeType scopeType, string name, string value)>();
        
        // Report only modified variables.
        foreach (var (scopeType, name, value) in newSnap.m_variables)
        {
            var match = m_variables.FindIndex(v => v.name == name && v.scopeType == scopeType);
            if (match >= 0)
            {
                // Variable retained - Has it changed value?
                if (m_variables[match].value == value)
                {
                    // No change detected - Don't report it.
                    continue;
                }
                
                // Variable has changed.
                variables.Add((scopeType, $" {name}", value));
            }
            else
            {
                // Variable is new.
                variables.Add((scopeType, $"+{name}", value));
            }
        }
        
        // Display.
        foreach (var (scopeType, name, value) in variables
                     .OrderByDescending(o => o.scopeType)
                     .ThenBy(o => o.name))
        {
            var scopeName = scopeType == ScopeType.Global ? "🌍 " : "  ";
            var prefix = scopeName + name[0]; // + or *
            var varName = name[1..];          // actual variable name
            sb.AppendLine($"{prefix}{varName.PadRight(9)} = {value}");
        }
        if (variables.Count > 0)
            sb.AppendLine();
        
        return sb.ToString();
    }
}