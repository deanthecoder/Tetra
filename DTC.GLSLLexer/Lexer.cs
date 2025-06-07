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
using System;
using System.Collections.Generic;

namespace DTC.GLSLLexer;

public class Lexer
{
    private static readonly HashSet<string> Keywords = [
        "float", "int", "uint", "bool", "void",
        "return", "if", "else", "for", "while", "uniform",
        "const", "in", "out", "inout",
        "vec2", "vec3", "vec4",
        "ivec2", "ivec3", "ivec4",
        "mat2", "mat3", "mat4",
        "mat2x2", "mat2x3", "mat2x4",
        "mat3x2", "mat3x3", "mat3x4",
        "mat4x2", "mat4x3", "mat4x4",
        "break", "continue", "switch", "case",
        "struct"
    ];
    private static readonly Dictionary<string, TokenType> Operators = new()
    {
        { "+", TokenType.Plus },
        { "-", TokenType.Minus },
        { "*", TokenType.Asterisk },
        { "/", TokenType.Slash },
        { "=", TokenType.Equals },
        { "==", TokenType.EqualsEquals },
        { "!=", TokenType.NotEquals },
        { "<", TokenType.LessThan },
        { ">", TokenType.GreaterThan },
        { "&", TokenType.Ampersand },
        { "&&", TokenType.AndAnd },
        { "||", TokenType.OrOr },
        { "(", TokenType.LeftParen },
        { ")", TokenType.RightParen },
        { "[", TokenType.LeftBracket },
        { "]", TokenType.RightBracket },
        { "{", TokenType.LeftBrace },
        { "}", TokenType.RightBrace },
        { ";", TokenType.Semicolon },
        { ",", TokenType.Comma },
        { ".", TokenType.Dot },
        { "++", TokenType.Increment },
        { "--", TokenType.Decrement },
        { "+=", TokenType.PlusEquals },
        { "-=", TokenType.MinusEquals },
        { "*=", TokenType.AsteriskEquals },
        { "/=", TokenType.SlashEquals },
        { "<=", TokenType.LessThanEquals },
        { ">=", TokenType.GreaterThanEquals },
        { "<<", TokenType.ShiftLeft },
        { ">>", TokenType.ShiftRight },
        { "!", TokenType.Exclamation },
        { "%", TokenType.Percent },
        { "%=", TokenType.PercentEquals },
        { "^", TokenType.Caret },
        { "^=", TokenType.CaretEquals },
        { "~", TokenType.Tilde },
        { "?", TokenType.Question },
        { ":", TokenType.Colon }
    };
    
    private readonly List<Token> m_tokens = [];
    private int m_line;

    public Token[] Tokenize(string code)
    {
        if (code == null)
            throw new ArgumentNullException(nameof(code));

        m_tokens.Clear();
        m_line = 1;
        
        // Normalize different line endings to \n.
        code = code.Replace("\r\n", "\n");
        code = code.Replace("\r", "\n");

        var i = 0;
        while (i < code.Length)
        {
            var ch = code[i];
            if (ch == '\n')
            {
                m_line++;
                i++;
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue; // Skip whitespace
            }
            
            if (ConsumeBoolean(code, ref i))
                continue;

            if (char.IsLetter(ch) || ch == '_')
            {
                ConsumeIdentifierOrKeyword(code, ref i);
                continue;
            }
            
            if (char.IsDigit(ch) || ch == '.')
            {
                ConsumeNumber(code, ref i);
                continue;
            }

            if (ch == '/' && Peek(code, i + 1, out var nextCh))
            {
                switch (nextCh)
                {
                    case '/':
                        ConsumeComment(code, ref i, multiLine: false);
                        continue;
                    case '*':
                        ConsumeComment(code, ref i, multiLine: true);
                        continue;
                }
            }
            
            if (ConsumeOperator(code, ref i))
                continue;

            AppendToken(TokenType.Unknown, code, i, i + 1);
            i++;
        }
        
        return m_tokens.ToArray();
    }
    
    private void AppendToken(TokenType tokenType, string code, int startIndex, int endIndex) =>
        m_tokens.Add(new Token(tokenType, m_line, code, startIndex, endIndex));

    private static bool Peek(string code, int i, out char ch)
    {
        ch = i < code.Length ? code[i] : '\0';
        return ch != '\0';
    }

    private void ConsumeNumber(string code, ref int i)
    {
        var startIndex = i;
        var isFloat = false;
        char ch;
        while (Peek(code, i, out ch) && (ch == '.' || char.IsDigit(ch) || ch == 'e' || ch == 'E'))
        {
            isFloat |= ch == '.';
            i++;

            if (ch is 'e' or 'E' && Peek(code, i, out ch) && (ch == '+' || ch == '-'))
                i++;
        }

        if (!isFloat)
        {
            AppendToken(TokenType.IntLiteral, code, startIndex, i);
            return;
        }

        AppendToken(TokenType.FloatLiteral, code, startIndex, i);

        if (Peek(code, i, out ch) && ch == 'f')
            i++;
    }

    private bool ConsumeBoolean(string code, ref int i)
    {
        var startIndex = i;
        if (startIndex + 4 <= code.Length && code.Substring(startIndex, 4) == "true")
        {
            AppendToken(TokenType.TrueLiteral, code, startIndex, startIndex + 4);
            i += 4;
            return true;
        }
        if (startIndex + 5 <= code.Length && code.Substring(startIndex, 5) == "false")
        {
            AppendToken(TokenType.FalseLiteral, code, startIndex, startIndex + 5);
            i += 5;
            return true;
        }
        return false;
    }

    private void ConsumeIdentifierOrKeyword(string code, ref int i)
    {
        var startIndex = i;
        while (Peek(code, i, out var ch) && (char.IsLetterOrDigit(ch) || ch == '_'))
            i++;

        var value = code.Substring(startIndex, i - startIndex);
        var tokenType = Keywords.Contains(value) ? TokenType.Keyword : TokenType.Identifier;
        AppendToken(tokenType, code, startIndex, i);
    }

    private bool ConsumeOperator(string code, ref int i)
    {
        // Try two-character operators first.
        if (i + 1 < code.Length)
        {
            var twoCharOp = code.Substring(i, 2);
            if (Operators.TryGetValue(twoCharOp, out var tokenType))
            {
                AppendToken(tokenType, code, i, i + 2);
                i += 2;
                return true;
            }
        }

        // Try single-character operators.
        var oneCharOp = code[i].ToString();
        if (Operators.TryGetValue(oneCharOp, out var singleTokenType))
        {
            AppendToken(singleTokenType, code, i, i + 1);
            i++;
            return true;
        }

        return false;
    }
    
    private void ConsumeComment(string code, ref int i, bool multiLine)
    {
        var lineSpan = 0;
        var startIndex = i;
        if (multiLine)
        {
            while (Peek(code, i, out var ch1) && Peek(code, i + 1, out var ch2))
            {
                if (ch1 == '*' && ch2 == '/')
                {
                    i += 2;
                    break;
                }

                if (ch1 == '\n')
                    lineSpan++;
                i++;
            }
        }
        else
        {
            while (Peek(code, i, out var ch) && ch != '\n')
                i++;
        }
        AppendToken(TokenType.Comment, code, startIndex, i);
        m_line += lineSpan;
    }
}

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