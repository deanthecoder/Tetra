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
    public OperandType Type { get; init; }
    public string Raw { get; init; }
    public string Name { get; init; }
    public float FloatValue { get; init; }
    public int IntValue { get; set; }
}