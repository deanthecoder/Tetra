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

namespace TetraCore;

/// <summary>
/// Represents a single instruction in the Tetra virtual machine. 
/// Each instruction includes its op code, operands, and the source line number it originated from.
/// </summary>
public readonly struct Instruction
{
    private readonly SymbolTable m_symbolTable;
    
    public int LineNumber { get; init; }
    public OpCode OpCode { get; init; }
    public Operand[] Operands { get; init; }

    public Instruction(SymbolTable symbolTable)
    {
        m_symbolTable = symbolTable;
    }
    
    public override string ToString()
    {
        var table = m_symbolTable;
        return $"[Line {LineNumber}] {OpCodeToStringMap.GetString(OpCode)} {Operands.Select(o => o.ToUiString(table)).ToCsv()}".TrimEnd();
    }
}