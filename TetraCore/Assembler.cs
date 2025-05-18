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
/// Converts Tetra source code into a list of validated <see cref="Instruction"/> objects.
/// Handles comment stripping, instruction parsing, operand typing, and basic syntax validation.
/// </summary>
public static class Assembler
{
    public static Instruction[] Assemble(string code)
    {
        if (code == null)
            throw new ArgumentNullException(nameof(code));

        var instructions = new List<Instruction>();
        var lines = code.Split('\n').Select(RemoveComments).ToArray();
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
            
            // Find the OpCode.
            var opCode = GetOpCode(words[0], lineIndex);
                
            // Find the operands.
            var operands = words.Skip(1).Select(o => GetOperand(o, lineIndex)).ToArray();
            
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
            new (OpCode.Ld, [OperandType.Variable, OperandType.Variable]),
            new (OpCode.Ld, [OperandType.Variable, OperandType.Float]),
            new (OpCode.Ld, [OperandType.Variable, OperandType.Int]),
            new (OpCode.Add, [OperandType.Variable, OperandType.Int]),
            new (OpCode.Add, [OperandType.Variable, OperandType.Float]),
            new (OpCode.Add, [OperandType.Variable, OperandType.Variable]),
            new (OpCode.Sub, [OperandType.Variable, OperandType.Int]),
            new (OpCode.Sub, [OperandType.Variable, OperandType.Float]),
            new (OpCode.Sub, [OperandType.Variable, OperandType.Variable]),
            new (OpCode.Inc, [OperandType.Variable]),
            new (OpCode.Dec, [OperandType.Variable])
        };

        var matches = expectedValues.Where(o => o.opCode == instr.OpCode).ToArray();
        if (matches.Length == 0)
            throw new InvalidOperationException($"'{instr}': Unrecognized instruction."); // We need to add entry to the table.
        
        // Validate the number of operands.
        if (instr.Operands.Length != matches[0].types.Length)
            throw new SyntaxErrorException($"[Line {lineIndex + 1}] Error: '{instr.OpCode}' expected {matches[0].types.Length} operands, but got {instr.Operands.Length}.");
        
        // Validate the operand types.
        var actualTypes = instr.Operands.Select(o => o.Type).ToArray();
        var expectedTypes = matches.Select(o => o.types).ToArray();
        if (!expectedTypes.Any(o => o.SequenceEqual(actualTypes)))
            throw new SyntaxErrorException($"[Line {lineIndex + 1}] Error: '{instr.OpCode}' operand types do not match expected patterns.");
    }

    private static OpCode GetOpCode(string word, int lineIndex)
    {
        return word switch
        {
            "ld" => OpCode.Ld,
            "add" => OpCode.Add,
            "sub" => OpCode.Sub,
            "inc" => OpCode.Inc,
            "dec" => OpCode.Dec,
            _ => throw new SyntaxErrorException($"[Line {lineIndex + 1}] Error: Unrecognized instruction '{word}'")
        };
    }
    
    private static Operand GetOperand(string word, int lineIndex)
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

        if (word.EndsWith(':'))
        {
            // Label.
            return new Operand
            {
                Type = OperandType.Label,
                Raw = word,
                Name = word[..^1]
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