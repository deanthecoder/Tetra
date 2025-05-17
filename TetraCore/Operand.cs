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

namespace TetraCore;

/// <summary>
/// Represents a typed operand in a Tetra instruction. Operands may be variable names, 
/// constants (float or int), or label identifiers, and carry both raw and parsed forms.
/// </summary>
[DebuggerDisplay("{Type}: {Raw}")]
public struct Operand
{
    /// <summary>
    /// Gets the type of operand (e.g., variable, constant, label).
    /// </summary>
    public OperandType Type { get; init; }

    /// <summary>
    /// Gets the raw text representation of the operand, as it appeared in the source.
    /// </summary>
    public string Raw { get; init; }

    /// <summary>
    /// Gets the variable or label name (only applicable for operands of type Variable or Label).
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the float value if the operand is a float constant.
    /// </summary>
    public float FloatValue { get; init; }

    /// <summary>
    /// Gets or sets the integer value if the operand is an integer constant.
    /// </summary>
    public int IntValue { get; init; }
}