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
namespace DTC.GLSLParser;

/// <summary>
/// A scope-based temporary variable block that restores the counter to its original value upon disposal.
/// Used to manage temporary variable numbering within function scope.
/// </summary>
public readonly ref struct TempVariableBlock
{
    private readonly ref int m_counter;
    private readonly int m_originalCounter;

    public TempVariableBlock(ref int counter)
    {
        m_counter = ref counter;
        m_originalCounter = counter;
    }

    public void Dispose()
    {
        m_counter = m_originalCounter;
    }
}