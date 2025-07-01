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
/// Represents a single operand used in a Tetra instruction, encapsulating values such as variables, constants, and labels.
/// </summary>
/// <remarks>
/// Internally, all numeric values are stored using a float array, supporting scalars and vectors.
/// Each operand also records its type, along with optional metadata like variable or label name.
/// </remarks>
public sealed class Operand
{
    public static readonly Operand Unassigned = new Operand(0.0f) { IsUnassigned = true };
    
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

    private Operand(OperandType type, VarName name, string label, float[] floats)
    {
        Type = type;
        Name = name;
        Label = label;
        Floats = floats;
    }
    
    /// <summary>
    /// Gets the type of operand (e.g., variable, constant, label).
    /// </summary>
    public OperandType Type { get; init; }

    /// <summary>
    /// Gets the variable name (only applicable for operands of type Variable).
    /// </summary>
    public VarName Name { get; init; }
    
    /// <summary>
    /// Gets the label name.
    /// </summary>
    public string Label { get; init; }

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

    public bool IsUnassigned { get; init; }

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
        ToUiString();

    public string ToUiString(SymbolTable symbolTable = null)
    {
        if (IsUnassigned)
            return "<UNASSIGNED>";
        return Type switch
        {
            OperandType.Float => $"{Float:0.0##}",
            OperandType.Int => Int.ToString(CultureInfo.InvariantCulture),
            OperandType.Vector => $"[{Floats.Select(o => $"{o:0.0##}").ToCsv()}]",
            OperandType.Label => Label,
            OperandType.Variable => Name.ToUiString(symbolTable),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// Turn a single-length operand into a multi-length operand (by repeating the value).
    /// </summary>
    public Operand GrowFromOneToN(int length)
    {
        if (IsUnassigned)
            throw new InvalidOperationException("Cannot grow an unassigned operand.");
        if (Length != 1)
            throw new InvalidOperationException("Only one-dimensional operands can be grown.");
        
        if (length == Length)
            return this; // No change.
        
        var floats = new float[length];
        Array.Fill(floats, Float);
        return new Operand(floats);
    }

    public Operand WithType(OperandType newType)
    {
        if (IsUnassigned)
            throw new InvalidOperationException("Cannot change the type of an unassigned operand.");
        return new Operand(newType, Name, Label, Floats);
    }
}