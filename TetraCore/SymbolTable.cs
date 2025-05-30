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
namespace TetraCore;

/// <summary>
/// Maintains a mapping between slot indices and original variable names as assigned by the assembler.
/// </summary>
/// <remarks>
/// This is primarily used for debugging and tooling purposes to restore human-readable variable names
/// from their compiled slot-based representations.
/// </remarks>
public class SymbolTable : Dictionary<int, string>
{
    public int GetSlotFromName(string variableName) =>
        this.First(o => o.Value == variableName).Key;
}