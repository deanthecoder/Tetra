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
    
    public Program WithInstructions(Instruction[] instructions) =>
        new Program(instructions, SymbolTable, LabelTable);

    public void Dump(bool addLineNumbers = true)
    {
        var instructions = Instructions.Select((o, i) => (Line: (int?)i, Instr: o.ToString())).ToList();
        
        // Re-add labels.
        foreach (var (label, index) in LabelTable.OrderByDescending(o => o.Value))
        {
            var s = label.StartsWith('_') ? label : $"\n{label}";
            instructions.Insert(index, (null, $"{s}:"));
        }

        // Write out.
        var jmpKeywords = new[] { OpCode.Call, OpCode.Jmp, OpCode.Jmpz, OpCode.Jmpnz }.Select(o => o.ToString().ToLower()).ToArray();
        var jmpTargetRegex = new Regex(@"(\d+)$");
        foreach (var instruction in instructions)
        {
            var s = instruction;

            foreach (var keyword in jmpKeywords)
            {
                if (!s.Instr.Contains(keyword))
                    continue;
                var match = jmpTargetRegex.Match(s.Instr);
                if (!match.Success)
                    continue;

                var target = int.Parse(match.Groups[^1].Value);
                var labels = LabelTable.Where(o => o.Value == target).Select(o => o.Key).ToArray();
                if (labels.Length == 0)
                    continue;
                
                var label = labels[0];
                if (labels.Length > 1)
                {
                    // Could have a jmp target and a call target.
                    var isJmp = s.Instr.StartsWith(nameof(OpCode.Jmp), StringComparison.OrdinalIgnoreCase);
                    if (isJmp)
                    {
                        // Jmps are more likely to target a name with underscore prefix.
                        label = labels.FirstOrDefault(o => o.StartsWith('_')) ?? labels[0];
                    }
                    else
                    {
                        // Calls are more likely to target a name without underscore prefix.
                        label = labels.FirstOrDefault(o => !o.StartsWith('_')) ?? labels[0];
                    }
                }

                s = (s.Line, s.Instr.Replace(match.Groups[^1].Value, label));
                break;
            }

            Console.WriteLine(s.Line == null || !addLineNumbers ? s.Instr : $"{s.Line}: {s.Instr}");
        }
    }
}