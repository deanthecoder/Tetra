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
namespace DTC.GLSLLexer;

/// <summary>
/// Represents a token produced during lexical analysis of source code.
/// Contains information about the token type, its value, and line number.
/// </summary>
public class Token
{
    private readonly int m_line;
    
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, int line, string code, int startIndex, int endIndex)
    {
        Type = type;
        m_line = line;
        Value = code.Substring(startIndex, endIndex - startIndex);
    }

    public override string ToString() =>
        $"[Line {m_line}] '{Value}' ({Type})";
}