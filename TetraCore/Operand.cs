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

using System.Globalization;
using DTC.Core.Extensions;
using TetraCore.Exceptions;

namespace TetraCore;

/// <summary>
/// Represents a typed operand in a Tetra instruction. Operands may be variable names, 
/// constants (float or int), or label identifiers, and carry both raw and parsed forms.
/// </summary>
/// <remarks>
/// Internally all values are stored in a float array.
/// </remarks>
public readonly struct Operand
{
    public Operand(int f)
    {
        Type = OperandType.Int;
        Floats = [f];
    }

    public Operand(params float[] v)
    {
        Type = v.Length == 1 ? OperandType.Float : OperandType.Vector;
        Floats = v;
    }
    
    /// <summary>
    /// Gets the type of operand (e.g., variable, constant, label).
    /// </summary>
    public OperandType Type { get; init; }

    /// <summary>
    /// Gets the variable or label name (only applicable for operands of type Variable or Label).
    /// </summary>
    public VarName Name { get; init; }

    /// <summary>
    /// Gets the float value if the operand is a float constant.
    /// </summary>
    public float Float => Floats[0];

    /// <summary>
    /// Gets the integer value if the operand is an integer constant.
    /// </summary>
    public int Int => (int)Floats[0];
    
    /// <summary>
    /// Gets the number(s) contained within the operand.
    /// </summary>
    public float[] Floats { get; }

    /// <summary>
    /// Number of components in this operand.
    /// </summary>
    public int Length => Floats?.Length ?? 1;

    public float AsFloat() =>
        Type switch
        {
            OperandType.Float => Floats[0],
            OperandType.Int => Floats[0],
            _ => throw new RuntimeException($"Cannot convert operand '{ToString()}' ({Type}) to a float.")
        };

    /// <summary>
    /// Return single value, or an array operand of numbers.
    /// </summary>
    public static Operand FromOperands(Operand[] operands)
    {
        // Just one operand? No change.
        if (operands.Length == 1)
            return operands[0];
        
        // All operands must be numeric.
        if (operands.All(o => o.Type is OperandType.Float or OperandType.Int or OperandType.Vector))
            return new Operand(operands.SelectMany(o => o.Floats).ToArray());
        
        var s = "Error: Multiple operands must all be numeric";
        s += "\nReceived:";
        s += $"\n  {operands.Select(op => $"<{op}>").ToCsv()}";
        throw new SyntaxErrorException(s);
    }

    public override string ToString() =>
        Type switch
        {
            OperandType.Float => $"{Float:0.0###}f",
            OperandType.Int => Int.ToString(CultureInfo.InvariantCulture),
            OperandType.Vector => $"[{Floats.Select(o => $"{o:0.0###}f").ToCsv()}]",
            OperandType.Label => Name.Name,
            OperandType.Variable => $"${Name}",
            _ => throw new ArgumentOutOfRangeException()
        };

    /// <summary>
    /// Turn a single-length operand into a multi-length operand (by repeating the value).
    /// </summary>
    public Operand GrowFromOneToN(int length)
    {
        if (Length != 1)
            throw new InvalidOperationException("Only one-dimensional operands can be grown.");
        
        if (length == Length)
            return this; // No change.
        
        var floats = new float[length];
        for (var i = 0; i < length; i++)
            floats[i] = Float;
        return new Operand(floats);
    }
}