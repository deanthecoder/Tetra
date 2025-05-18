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

using DTC.Core.Extensions;
using TetraCore.Exceptions;

namespace TetraCore;

/// <summary>
/// Converts Tetra source code into a list of validated <see cref="Instruction"/> objects.
/// Handles comment stripping, instruction parsing, operand typing, and basic syntax validation.
/// </summary>
public static class Assembler
{
    public static Instruction[] Assemble(string code)
    {
        try
        {
            return AssembleImpl(code);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }
    
    private static Instruction[] AssembleImpl(string code)
    {
        var labels = new Dictionary<string, int>();
        
        if (code == null)
            throw new ArgumentNullException(nameof(code));

        var instructions = new List<Instruction>();
        var lines = code.Split('\n').Select(RemoveComments).ToArray();
        
        // First pass: Quickly find all labels.
        var labelNames = lines.Where(o => o.EndsWith(':')).Select(o => o[..^1]).ToArray();
        var duplicateLabels = labelNames.Where(o => labelNames.Count(n => n == o) > 1).ToArray();
        if (duplicateLabels.Length > 0)
            throw new SyntaxErrorException($"Error: Duplicate labels found: {duplicateLabels.ToCsv()}.");
        labelNames.ForEach(o => labels[o] = -1);
        
        // Second pass: Compile the instructions.
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                continue; // Blank line.
            
            // Split the line into words.
            var words =
                lines[lineIndex]
                    .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(o => o.Trim(','))
                    .ToArray();
            
            // Have we found a label?
            if (words[0].EndsWith(':'))
            {
                if (words.Length > 1)
                    throw new SyntaxErrorException($"[Line {lineIndex + 1}] Error: Label '{words[0]}' cannot have operands.");
                var labelName = words[0][..^1];

                var ip = instructions.Count;
                labels[labelName] = ip;
                continue;
            }
            
            // Find the OpCode.
            var opCode = GetOpCode(words[0], lineIndex);
                
            // Find the operands.
            var operands = words.Skip(1).Select(o => GetOperand(o, lineIndex, labels.Keys)).ToArray();
            
            // Build the instruction.
            var instruction = new Instruction
            {
                LineNumber = lineIndex + 1,
                OpCode = opCode,
                Operands = operands
            }; 
            ValidateInstruction(instruction, lineIndex);
            
            instructions.Add(instruction);
        }
        
        // Third pass: Resolve labels.
        foreach (var instr in instructions)
        {
            for (var j = 0; j < instr.Operands.Length; j++)
            {
                if (instr.Operands[j].Type != OperandType.Label)
                    continue; // Instruction doesn't reference a label.
                
                // Replace the label with the actual value.
                var labelName = instr.Operands[j].Name;
                if (!labels.TryGetValue(labelName, out var labelIp))
                    throw new SyntaxErrorException($"[Line {instr.LineNumber}] Error: Label '{labelName}' not found.");
                instr.Operands[j] = new Operand(labelIp);
            }
        }
        
        return instructions.ToArray();
    }

    private static string RemoveComments(string line)
    {
        var commentIndex = line.IndexOf("#", StringComparison.OrdinalIgnoreCase);
        return commentIndex >= 0 ? line[..commentIndex].Trim() : line;
    }

    private static void ValidateInstruction(Instruction instr, int lineIndex)
    {
        var expectedValues = new (OpCode opCode, OperandType[] types)[] 
        {
            new (OpCode.Ld, [OperandType.Variable, OperandType.Int]),
            new (OpCode.Ld, [OperandType.Variable, OperandType.Float]),
            new (OpCode.Ld, [OperandType.Variable, OperandType.Variable]),
            new (OpCode.Halt, []),
            new (OpCode.Add, [OperandType.Variable, OperandType.Int]),
            new (OpCode.Add, [OperandType.Variable, OperandType.Float]),
            new (OpCode.Add, [OperandType.Variable, OperandType.Variable]),
            new (OpCode.Sub, [OperandType.Variable, OperandType.Int]),
            new (OpCode.Sub, [OperandType.Variable, OperandType.Float]),
            new (OpCode.Sub, [OperandType.Variable, OperandType.Variable]),
            new (OpCode.Mul, [OperandType.Variable, OperandType.Int]),
            new (OpCode.Mul, [OperandType.Variable, OperandType.Float]),
            new (OpCode.Mul, [OperandType.Variable, OperandType.Variable]),
            new (OpCode.Div, [OperandType.Variable, OperandType.Int]),
            new (OpCode.Div, [OperandType.Variable, OperandType.Float]),
            new (OpCode.Div, [OperandType.Variable, OperandType.Variable]),
            new (OpCode.Inc, [OperandType.Variable]),
            new (OpCode.Dec, [OperandType.Variable]),
            new (OpCode.Neg, [OperandType.Variable]),
            new (OpCode.Jmp, [OperandType.Label]),
            new (OpCode.JmpEq, [OperandType.Variable, OperandType.Variable, OperandType.Label]),
            new (OpCode.JmpNe, [OperandType.Variable, OperandType.Variable, OperandType.Label]),
            new (OpCode.JmpLt, [OperandType.Variable, OperandType.Variable, OperandType.Label]),
            new (OpCode.JmpLe, [OperandType.Variable, OperandType.Variable, OperandType.Label]),
            new (OpCode.JmpGt, [OperandType.Variable, OperandType.Variable, OperandType.Label]),
            new (OpCode.JmpGe, [OperandType.Variable, OperandType.Variable, OperandType.Label]),
            new (OpCode.JmpEq, [OperandType.Variable, OperandType.Int, OperandType.Label]),
            new (OpCode.JmpNe, [OperandType.Variable, OperandType.Int, OperandType.Label]),
            new (OpCode.JmpLt, [OperandType.Variable, OperandType.Int, OperandType.Label]),
            new (OpCode.JmpLe, [OperandType.Variable, OperandType.Int, OperandType.Label]),
            new (OpCode.JmpGt, [OperandType.Variable, OperandType.Int, OperandType.Label]),
            new (OpCode.JmpGe, [OperandType.Variable, OperandType.Int, OperandType.Label]),
            new (OpCode.JmpEq, [OperandType.Variable, OperandType.Float, OperandType.Label]),
            new (OpCode.JmpNe, [OperandType.Variable, OperandType.Float, OperandType.Label]),
            new (OpCode.JmpLt, [OperandType.Variable, OperandType.Float, OperandType.Label]),
            new (OpCode.JmpLe, [OperandType.Variable, OperandType.Float, OperandType.Label]),
            new (OpCode.JmpGt, [OperandType.Variable, OperandType.Float, OperandType.Label]),
            new (OpCode.JmpGe, [OperandType.Variable, OperandType.Float, OperandType.Label]),
            new (OpCode.Print, [OperandType.Int]),
            new (OpCode.Print, [OperandType.Float]),
            new (OpCode.Print, [OperandType.Variable]),
            new (OpCode.PushFrame, []),
            new (OpCode.PopFrame, []),
            new (OpCode.Call, [OperandType.Label]),
            new (OpCode.Ret, []),
            new (OpCode.Ret, [OperandType.Int]),
            new (OpCode.Ret, [OperandType.Float]),
            new (OpCode.Ret, [OperandType.Variable]),
        };

        var matches = expectedValues.Where(o => o.opCode == instr.OpCode).ToArray();
        if (matches.Length == 0)
            throw new InvalidOperationException($"'{instr}': Unrecognized instruction."); // We need to add entry to the table.
        
        // Validate the number of operands.
        if (matches.All(o => o.types.Length != instr.Operands.Length))
            throw new SyntaxErrorException($"[Line {lineIndex + 1}] Error: '{instr.OpCode}' expected {matches[0].types.Length} operands, but got {instr.Operands.Length}.");
        
        // Validate the operand types.
        var actualTypes = instr.Operands.Select(o => o.Type).ToArray();
        var expectedTypes = matches.Select(o => o.types).ToArray();
        if (!expectedTypes.Any(o => o.SequenceEqual(actualTypes)))
            throw new SyntaxErrorException($"[Line {lineIndex + 1}] Error: '{instr.OpCode}' operand types do not match expected patterns.");
    }

    private static OpCode GetOpCode(string word, int lineIndex)
    {
        var opCode = OpCodeToStringMap.GetOpCode(word);
        if (!opCode.HasValue)
            throw new SyntaxErrorException($"[Line {lineIndex + 1}] Error: Unrecognized instruction '{word}'");
        return opCode.Value;
    }
    
    private static Operand GetOperand(string word, int lineIndex, IEnumerable<string> labels)
    {
        if (word.StartsWith('$'))
        {
            // Variable.
            return new Operand
            {
                Type = OperandType.Variable,
                Raw = word,
                Name = word[1..]
            };
        }

        if (labels.Contains(word))
        {
            // Label.
            return new Operand
            {
                Type = OperandType.Label,
                Raw = word,
                Name = word
            };
        }

        if (word.Contains('.') && float.TryParse(word, out var floatValue))
        {
            // Float.
            return new Operand
            {
                Type = OperandType.Float,
                Raw = word,
                FloatValue = floatValue
            };
        }

        if (int.TryParse(word, out var intValue))
        {
            // Integer.
            return new Operand
            {
                Type = OperandType.Int,
                Raw = word,
                IntValue = intValue
            };
        }
        
        throw new SyntaxErrorException($"[Line {lineIndex + 1}] Error: Unrecognized operand '{word}'");
    }
}