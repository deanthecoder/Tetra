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
using System.Text.RegularExpressions;

namespace TetraCore;

/// <summary>
/// A Tetra program (Instruction list and debugging symbol table).
/// </summary>
public class Program
{
    public Instruction[] Instructions { get; }
    public SymbolTable SymbolTable { get; }
    public LabelTable LabelTable { get; }

    public Program(Instruction[] instructions, SymbolTable symbolTable, LabelTable labelTable)
    {
        Instructions = instructions;
        SymbolTable = symbolTable;
        LabelTable = labelTable;
    }

    public void Dump()
    {
        var instructions = Instructions.Select(o => o.ToString()).ToList();
        
        // Re-add labels.
        foreach (var (label, index) in LabelTable.OrderByDescending(o => o.Value))
        {
            var line = label.StartsWith('_') ? label : $"\n{label}()";
            instructions.Insert(index, $"{line}:");
        }

        // Write out.
        foreach (var instruction in instructions)
        {
            var updatedInstruction = instruction;

            foreach (var keyword in new[] { OpCode.Call, OpCode.Jmp, OpCode.Jmpz, OpCode.Jmpnz }.Select(o => o.ToString().ToLower()))
            {
                var match = Regex.Match(instruction, $@"\b{keyword} (\d+)$");
                if (!match.Success)
                    continue;
                
                var target = int.Parse(match.Groups[1].Value);
                var label = LabelTable.FirstOrDefault(o => o.Value == target).Key;
                if (label == null)
                    continue;
                
                updatedInstruction = instruction.Replace($"{keyword} {target}", $"{keyword} {label}");
                break;
            }

            Console.WriteLine(updatedInstruction);
        }
    }
}