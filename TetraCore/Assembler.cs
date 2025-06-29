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
using DTC.Core;
using DTC.Core.Extensions;
using TetraCore.Exceptions;

namespace TetraCore;

/// <summary>
/// Converts Tetra source code into a list of validated <see cref="Instruction"/> objects.
/// Handles comment stripping, instruction parsing, operand typing, and basic syntax validation.
/// </summary>
public class Assembler
{
    private readonly Dictionary<string, int> m_operandSlots = [];

    public static Program Assemble(string code) =>
        new Assembler().Assemble(code);

    public Program Assemble(string code, params string[] uniforms)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            Logger.Instance.Info("Assembling Tetra source...");
            var program = AssembleImpl(code, uniforms);
            Logger.Instance.Info($"Assembled {program.Instructions.Length:N0} Tetra instructions.");
            return program;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            Logger.Instance.Info($"Assembled in {stopwatch.ElapsedMilliseconds}ms.");
        }
    }
    
    private Program AssembleImpl(string code, string[] uniforms)
    {
        if (code == null)
            throw new ArgumentNullException(nameof(code));
        
        m_operandSlots.Clear();
        m_operandSlots["retval"] = ScopeFrame.RetvalSlot;
        uniforms.ForEach(o => m_operandSlots[o] = GetNextOperandSlot());
        var symbolTable = new SymbolTable();

        var instructions = new List<Instruction>();
        var lines =
            code
                .Split(["\r\n", "\n"], StringSplitOptions.None)
                .Select(RemoveComments)
                .Select(o => o.Trim())
                .ToArray();
        
        // First pass: Quickly find all labels.
        var labelNames = lines.Where(o => o.EndsWith(':')).Select(o => o[..^1]).ToArray();
        var duplicateLabels = labelNames.Where(o => labelNames.Count(n => n == o) > 1).ToArray();
        if (duplicateLabels.Length > 0)
            throw new SyntaxErrorException($"Error: Duplicate labels found: {duplicateLabels.ToCsv()}.");
        var labelTable = new LabelTable();
        labelNames.ForEach(o => labelTable.Add(o, -1));

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
                labelTable[labelName] = ip;
                continue;
            }
            
            // Find the OpCode.
            var opCode = GetOpCode(words[0], lineIndex, lines[lineIndex]);
                
            // Find the operands.
            var operands = words.Skip(1).Select(o => GetOperand(o, opCode, lineIndex, lines[lineIndex], labelTable.Keys, symbolTable)).ToArray();
            
            // Build the instruction.
            var instruction = new Instruction(symbolTable)
            {
                LineNumber = lineIndex + 1,
                OpCode = opCode,
                Operands = operands
            }; 
            
            ValidateInstruction(instruction);
            
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
                var labelName = instr.Operands[j].Label;
                if (!labelTable.TryGetValue(labelName, out var labelIp))
                    throw new SyntaxErrorException($"[Line {instr.LineNumber}] Error: Label '{labelName}' not found.");
                instr.Operands[j] = new Operand(labelIp);
            }
        }

        return new Program(instructions.ToArray(), symbolTable, labelTable);
    }

    private static string RemoveComments(string line)
    {
        var commentIndex = line.IndexOf("#", StringComparison.OrdinalIgnoreCase);
        return commentIndex >= 0 ? line[..commentIndex].Trim() : line;
    }

    private static void ValidateInstruction(Instruction instr)
    {
        // Validate the operands.
        var expectedTypes = GetExpectedOperandTypes(instr.OpCode);
        var actualTypes = instr.Operands.Select(o => o.Type).ToArray();
        foreach (var expected in expectedTypes)
        {
            if (actualTypes.Length < expected.Length)
                continue; // Not enough operands.
            
            var allMatch = true;
            for (var i = 0; i < expected.Length; i++)
            {
                // Match if either:
                // 1. Types are identical, OR
                // 2. Expected is numeric (float/int) and actual is a variable reference.
                var typesMatch = actualTypes[i] == expected[i] ||
                                 (actualTypes[i] == OperandType.Variable && expected[i] is OperandType.Float or OperandType.Int or OperandType.Vector);
                allMatch &= typesMatch;
            }

            if (allMatch)
                return; // Found matching operand types - instruction is valid.
        }
        
        // Invalid operands.
        var s = "Error: Instruction has unexpected operands.";
        s += $"\n  {instr}";
        s += "\nReceived:";
        s += $"\n  {OpCodeToStringMap.GetString(instr.OpCode)} {actualTypes.Select(op => $"<{op}>").ToCsv()}";
        s += "\nExpected:";
        expectedTypes.ForEach(o => s += $"\n  {OpCodeToStringMap.GetString(instr.OpCode)} {o.Select(op => $"<{op}>").ToCsv()}");
        throw new SyntaxErrorException(s);
    }

    private static OperandType[][] GetExpectedOperandTypes(OpCode opCode)
    {
        var expectedValues = new (OpCode opCode, OperandType[] types)[]
        {
            new(OpCode.Nop, []),
            new(OpCode.Decl, [OperandType.Variable]),
            new(OpCode.Ld, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Ld, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Ldc, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Ldc, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Halt, []),
            new(OpCode.Add, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Add, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Sub, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Sub, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Mul, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Mul, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Div, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Div, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Inc, [OperandType.Variable]),
            new(OpCode.Dec, [OperandType.Variable]),
            new(OpCode.Neg, [OperandType.Variable]),
            new(OpCode.Dim, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Shiftl, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Shiftr, [OperandType.Variable, OperandType.Int]),
            new(OpCode.BitAnd, [OperandType.Variable, OperandType.Int]),
            new(OpCode.BitOr, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Lt, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Lt, [OperandType.Variable, OperandType.Float]), 
            new(OpCode.Le, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Le, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Gt, [OperandType.Variable, OperandType.Int]), 
            new(OpCode.Gt, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Ge, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Ge, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Eq, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Eq, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Ne, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Ne, [OperandType.Variable, OperandType.Float]),
            new(OpCode.And, [OperandType.Variable, OperandType.Int]),
            new(OpCode.And, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Or, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Or, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Not, [OperandType.Variable]),
            new(OpCode.Test, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Test, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Jmp, [OperandType.Label]),
            new(OpCode.Jmpz, [OperandType.Variable, OperandType.Label]),
            new(OpCode.Jmpnz, [OperandType.Variable, OperandType.Label]),
            new(OpCode.Print, [OperandType.Int]),
            new(OpCode.Print, [OperandType.Float]),
            new(OpCode.PushFrame, []),
            new(OpCode.PopFrame, []),
            new(OpCode.Call, [OperandType.Label]),
            new(OpCode.Ret, []),
            new(OpCode.Ret, [OperandType.Int]),
            new(OpCode.Ret, [OperandType.Float]),
            new(OpCode.Sin, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Sinh, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Asin, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Cos, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Cosh, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Acos, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Tan, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Tanh, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Atan, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Pow, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Sqrt, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Exp, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Log, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Abs, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Sign, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Mod, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Min, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Max, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Ceil, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Floor, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Fract, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Length, [OperandType.Variable, OperandType.Int]),
            new(OpCode.Length, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Normalize, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Clamp, [OperandType.Variable, OperandType.Int, OperandType.Int]),
            new(OpCode.Clamp, [OperandType.Variable, OperandType.Float, OperandType.Float]),
            new(OpCode.Mix, [OperandType.Variable, OperandType.Float, OperandType.Float]),
            new(OpCode.Smoothstep, [OperandType.Variable, OperandType.Float, OperandType.Float]),
            new(OpCode.Dot, [OperandType.Variable, OperandType.Float]),
            new(OpCode.Reflect, [OperandType.Vector, OperandType.Vector]),
            new(OpCode.Refract, [OperandType.Vector, OperandType.Vector, OperandType.Float]),
            new(OpCode.Cross, [OperandType.Vector, OperandType.Float]),
        };

        var matches = expectedValues.Where(o => o.opCode == opCode).ToArray();
        if (matches.Length == 0)
            throw new InvalidOperationException($"'{opCode}': Unrecognized instruction."); // We need to add entry to the table.

        return matches.Select(o => o.types).ToArray();
    }

    private static OpCode GetOpCode(string word, int lineIndex, string line)
    {
        var opCode = OpCodeToStringMap.GetOpCode(word);
        if (!opCode.HasValue)
            throw new SyntaxErrorException($"Error: Unrecognized instruction '{word}'.\n  {lineIndex + 1}: {line.Trim()}");
        return opCode.Value;
    }

    /// <summary>
    /// Parses and validates a single operand from a Tetra instruction.
    /// </summary>
    /// <param name="word">The raw operand text to parse.</param>
    /// <param name="opCode">The instruction's OpCode for validation context.</param>
    /// <param name="lineIndex">Current line number for error reporting (zero-based).</param>
    /// <param name="line">The complete source line for error context.</param>
    /// <param name="labels">Collection of valid label names for reference validation.</param>
    /// <param name="symbolTable">A map of variable names to slot index.</param>
    /// <returns>A typed <see cref="Operand"/> representing the parsed value.</returns>
    /// <exception cref="SyntaxErrorException">Thrown when the operand cannot be parsed or is invalid for the instruction.</exception>
    private Operand GetOperand(
        string word,
        OpCode opCode,
        int lineIndex,
        string line,
        IEnumerable<string> labels,
        SymbolTable symbolTable)
    {
        if (word.StartsWith('$'))
        {
            var bracketIndex = word.IndexOf('[');
            var brackets = bracketIndex >= 0 ? word[bracketIndex..] : string.Empty;
            var variableName = bracketIndex < 0 ? word[1..] : word[1..bracketIndex];
            
            // Support names like 'v.x' (Treat as 'v').
            var swizzle = string.Empty;
            if (variableName.Contains('.'))
            {
                swizzle = variableName[variableName.IndexOf('.')..];
                variableName = variableName[..variableName.IndexOf('.')];
            }

            if (!m_operandSlots.TryGetValue(variableName, out var opSlot))
                opSlot = m_operandSlots[variableName] = GetNextOperandSlot();
            symbolTable[opSlot] = variableName;
            
            // Variable.
            return new Operand
            {
                Type = OperandType.Variable,
                Name = $"{opSlot}{swizzle}{brackets}"
            };
        }

        // Float.
        if (word.Contains('.') && float.TryParse(word, out var floatValue))
            return new Operand(floatValue);

        // Integer.
        if (int.TryParse(word, out var intValue))
            return new Operand(intValue);

        if (labels.Contains(word))
        {
            // Label.
            return new Operand
            {
                Type = OperandType.Label,
                Label = word
            };
        }
        
        // Unrecognized operand.
        var s = $"Error: Unrecognized operand '{word}'.\n  {lineIndex + 1}: {line.Trim()}\nExpected:";
        GetExpectedOperandTypes(opCode).ForEach(o => s += $"\n  {OpCodeToStringMap.GetString(opCode)} {o.Select(op => $"<{op}>").ToCsv()}");

        throw new SyntaxErrorException(s);
    }

    private int GetNextOperandSlot()
    {
        if (m_operandSlots.Count == ScopeFrame.RetvalSlot - 1)
            throw new InvalidOperationException("Variable limit reached.");
        return m_operandSlots.Count;
    }
}