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
public readonly struct Operand
{
    public Operand(int f)
    {
        Type = OperandType.Int;
        IntValue = f;
    }

    public Operand(params float[] v)
    {
        Type = v.Length == 1 ? OperandType.Float : OperandType.Vector;
        Xyzw = v;
    }
    
    /// <summary>
    /// Gets the type of operand (e.g., variable, constant, label).
    /// </summary>
    public OperandType Type { get; init; }

    /// <summary>
    /// Gets the variable or label name (only applicable for operands of type Variable or Label).
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the float value if the operand is a float constant.
    /// </summary>
    public float FloatValue => Xyzw[0];

    /// <summary>
    /// Gets or sets the integer value if the operand is an integer constant.
    /// </summary>
    public int IntValue { get; }
    
    /// <summary>
    /// Gets the vector value if the operand is a vector constant.
    /// </summary>
    public float[] Xyzw { get; }

    public float AsFloat() =>
        Type switch
        {
            OperandType.Float => FloatValue,
            OperandType.Int => IntValue,
            _ => throw new RuntimeException($"Cannot convert operand '{ToString()}' ({Type}) to a float.")
        };

    public override string ToString() =>
        Type switch
        {
            OperandType.Float => $"{FloatValue:0.0###}f",
            OperandType.Int => IntValue.ToString(CultureInfo.InvariantCulture),
            OperandType.Vector => $"vec{Xyzw.Length}({Xyzw.Select(o => $"{o:0.0###}").ToCsv()})",
            OperandType.Label => Name,
            OperandType.Variable => $"${Name}",
            _ => throw new ArgumentOutOfRangeException()
        };
}