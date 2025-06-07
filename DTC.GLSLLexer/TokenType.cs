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
/// Represents the different types of tokens that can be identified during lexical analysis.
/// This includes literals, keywords, identifiers, operators, punctuation marks, and comments.
/// </summary>
public enum TokenType
{
    Unknown,
    
    // Literals
    IntLiteral,
    FloatLiteral,
    TrueLiteral,
    FalseLiteral,

    // Keywords
    Keyword,

    // Identifiers
    Identifier,

    // Operators
    Plus,
    Minus,
    Asterisk,
    Slash,
    Equals,
    EqualsEquals,
    NotEquals,
    LessThan,
    GreaterThan,
    Ampersand,
    AndAnd,
    OrOr,
    Increment,
    Decrement,
    PlusEquals,
    MinusEquals,
    AsteriskEquals,
    SlashEquals,
    LessThanEquals,
    GreaterThanEquals,
    ShiftLeft,
    ShiftRight,
    Exclamation,
    Percent,
    PercentEquals,
    Caret,
    CaretEquals,
    Tilde,
    Question,
    Colon,

    // Punctuation
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    LeftBrace,
    RightBrace,
    Semicolon,
    Comma,
    Dot,

    // Misc
    Comment
}