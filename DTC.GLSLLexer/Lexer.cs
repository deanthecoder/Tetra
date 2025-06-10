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


/// <summary>
/// A lexical analyzer for GLSL (OpenGL Shading Language) source code.
/// Breaks down source code into a linear sequence of tokens, identifying keywords, operators,
/// literals, identifiers, and comments while maintaining line number information.
/// </summary>
public class Lexer
{
    public static readonly HashSet<string> TypeNames =
    [
        "float", "int", "uint", "bool", "void",
        "vec2", "vec3", "vec4",
        "ivec2", "ivec3", "ivec4",
        "mat2", "mat3", "mat4",
        "mat2x2", "mat2x3", "mat2x4",
        "mat3x2", "mat3x3", "mat3x4",
        "mat4x2", "mat4x3", "mat4x4"
    ];
    private static readonly HashSet<string> Keywords =
    [
        "return", "if", "else", "for", "while", "uniform",
        "const", "in", "out", "inout",
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
        { "!=", TokenType.NotEqual },
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
        { "+=", TokenType.PlusEqual },
        { "-=", TokenType.MinusEqual },
        { "*=", TokenType.AsteriskEqual },
        { "/=", TokenType.SlashEqual },
        { "<=", TokenType.LessThanOrEqual },
        { ">=", TokenType.GreaterThanOrEqual },
        { "<<", TokenType.ShiftLeft },
        { ">>", TokenType.ShiftRight },
        { "!", TokenType.Exclamation },
        { "%", TokenType.Percent },
        { "%=", TokenType.PercentEqual },
        { "^", TokenType.Caret },
        { "^=", TokenType.CaretEquals },
        { "~", TokenType.Tilde },
        { "?", TokenType.Question },
        { ":", TokenType.Colon }
    };
    
    private readonly List<Token> m_tokens = [];
    private int m_line;
    private string m_code;

    public Token[] Tokenize(string code)
    {
        if (code == null)
            throw new ArgumentNullException(nameof(code));

        m_tokens.Clear();
        m_line = 1;
        
        // Normalize different line endings to \n.
        code = code.Replace("\r\n", "\n");
        code = code.Replace("\r", "\n");
        m_code = code;

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
            
            if (ConsumeBoolean(ref i))
                continue;

            if (char.IsLetter(ch) || ch == '_')
            {
                ConsumeIdentifierOrKeyword(ref i);
                continue;
            }
            
            if (char.IsDigit(ch) || ch == '.')
            {
                if (ch == '.')
                {
                    var nxtCh = i + 1 < code.Length ? code[i + 1] : '\0';
                    if (nxtCh == '\0' || !char.IsDigit(nxtCh))
                    {
                        // Process swizzle '.'.
                        ConsumeDot(ref i);
                        continue;
                    }
                }
                
                ConsumeNumber(ref i);
                continue;
            }

            if (ch == '/' && Peek(i + 1, out var nextCh))
            {
                switch (nextCh)
                {
                    case '/':
                        ConsumeComment(ref i, multiLine: false);
                        continue;
                    case '*':
                        ConsumeComment(ref i, multiLine: true);
                        continue;
                }
            }
            
            if (ConsumeOperator(ref i))
                continue;

            AppendToken(TokenType.Unknown, i, i + 1);
            i++;
        }
        
        return m_tokens.ToArray();
    }
    
    private void AppendToken(TokenType tokenType, int startIndex, int endIndex) =>
        m_tokens.Add(new Token(tokenType, m_line, m_code, startIndex, endIndex));

    private bool Peek(int i, out char ch)
    {
        ch = i < m_code.Length ? m_code[i] : '\0';
        return ch != '\0';
    }

    private void ConsumeNumber(ref int i)
    {
        var startIndex = i;
        var isFloat = false;
        char ch;
        while (Peek(i, out ch) && (ch == '.' || char.IsDigit(ch) || ch == 'e' || ch == 'E'))
        {
            isFloat |= ch == '.';
            i++;

            if (ch is 'e' or 'E' && Peek(i, out ch) && (ch == '+' || ch == '-'))
                i++;
        }

        if (!isFloat)
        {
            AppendToken(TokenType.IntLiteral, startIndex, i);
            return;
        }

        AppendToken(TokenType.FloatLiteral, startIndex, i);

        if (Peek(i, out ch) && ch == 'f')
            i++;
    }

    private bool ConsumeBoolean(ref int i)
    {
        var startIndex = i;
        if (startIndex + 4 <= m_code.Length && m_code.Substring(startIndex, 4) == "true")
        {
            AppendToken(TokenType.TrueLiteral, startIndex, startIndex + 4);
            i += 4;
            return true;
        }
        if (startIndex + 5 <= m_code.Length && m_code.Substring(startIndex, 5) == "false")
        {
            AppendToken(TokenType.FalseLiteral, startIndex, startIndex + 5);
            i += 5;
            return true;
        }
        return false;
    }

    private void ConsumeIdentifierOrKeyword(ref int i)
    {
        var startIndex = i;
        while (Peek(i, out var ch) && (char.IsLetterOrDigit(ch) || ch == '_'))
            i++;

        var value = m_code.Substring(startIndex, i - startIndex);
        var tokenType = Keywords.Contains(value) || TypeNames.Contains(value) ? TokenType.Keyword : TokenType.Identifier;
        AppendToken(tokenType, startIndex, i);
    }
    
    private void ConsumeDot(ref int i)
    {
        AppendToken(TokenType.Dot, i, i);
        i++;
    }

    private bool ConsumeOperator(ref int i)
    {
        // Try two-character operators first.
        if (i + 1 < m_code.Length)
        {
            var twoCharOp = m_code.Substring(i, 2);
            if (Operators.TryGetValue(twoCharOp, out var tokenType))
            {
                AppendToken(tokenType, i, i + 2);
                i += 2;
                return true;
            }
        }

        // Try single-character operators.
        var oneCharOp = m_code[i].ToString();
        if (Operators.TryGetValue(oneCharOp, out var singleTokenType))
        {
            AppendToken(singleTokenType, i, i + 1);
            i++;
            return true;
        }

        return false;
    }
    
    private void ConsumeComment(ref int i, bool multiLine)
    {
        var lineSpan = 0;
        var startIndex = i;
        if (multiLine)
        {
            while (Peek(i, out var ch1) && Peek(i + 1, out var ch2))
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
            while (Peek(i, out var ch) && ch != '\n')
                i++;
        }
        AppendToken(TokenType.Comment, startIndex, i);
        m_line += lineSpan;
    }
}