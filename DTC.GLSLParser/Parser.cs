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
using System.Linq;
using System.Text;
using DTC.GLSLLexer;

namespace DTC.GLSLParser;


/// <summary>
/// Parses a flat stream of GLSL tokens into an Abstract Syntax Tree (AST).
/// </summary>
public class Parser
{
    private Token[] m_tokens;
    private int m_tokenIndex;
    
    private Token CurrentToken => m_tokens[m_tokenIndex];

    public ProgramNode Parse(Token[] tokens)
    {
        if (tokens == null)
            throw new ArgumentNullException(nameof(tokens));
        if (tokens.Length == 0)
            throw new ArgumentException("Tokens array cannot be empty.", nameof(tokens));
        
        m_tokens = tokens;
        m_tokenIndex = 0;

        var statements = new List<AstNode>();
        var startLine = 1;
        try
        {
            while (m_tokenIndex < m_tokens.Length)
            {
                startLine = CurrentToken.Line;

                var stmt = ParseStatement();
                statements.Add(stmt);
            }
        }
        catch (ParseException e)
        {
            var tokensAtLine = m_tokens.Where(o => o.Line == startLine).Select(o => o.Value);
            var line = string.Join(" ", tokensAtLine).Replace(" ;", ";");
            Console.WriteLine($"[Line {startLine}] {line}");
            Console.WriteLine(e.Message);
            throw;
        }
        
        return new ProgramNode(statements);
    }
    
    private Token Consume(TokenType expectedType = TokenType.Unknown, string expectation = null)
    {
        if (m_tokenIndex >= m_tokens.Length)
            throw new ParseException("Cannot consume token. No more tokens to consume.");
        
        var token = m_tokens[m_tokenIndex++];
        if (token.Type == expectedType || expectedType == TokenType.Unknown)
            return token; // Success
        
        var message = $"Expected {expectedType}, but got {token.Type} ({token.Value}).";
        if (expectation != null)
            message = $"{expectation}\n({message})";
        throw new ParseException(message);
    }

    private AstNode ParseStatement()
    {
        if (CurrentToken.Type == TokenType.Keyword)
        {
            if (CurrentToken.Value is "float" or "int")
                return ParseVariableDeclaration();
        }

        throw new ParseException($"Unexpected token '{CurrentToken}' at start of statement.");
    }
    
    private ExprStatementNode ParseExpression()
    {
        var token = Consume();
        switch (token.Type)
        {
            case TokenType.IntLiteral:
            case TokenType.FloatLiteral:
                return new LiteralNode(token);
            
            default:
                throw new ParseException($"Unexpected token '{token}' in expression.");
        }
    }

    private AssignmentNode ParseVariableDeclaration()
    {
        var typeToken = Consume(TokenType.Keyword);
        var nameToken = Consume(TokenType.Identifier, "Expected variable name");
        Consume(TokenType.Equals, "Expected '=' after variable name");
        var expr = ParseExpression();
        Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
        
        return new AssignmentNode(typeToken, nameToken, expr);
    }
}

/// <summary>
/// Base class for all nodes in the AST.
/// May represent expressions, statements, or program structure.
/// </summary>
public abstract class AstNode
{
}

/// <summary>
/// Represents the root of the AST, containing a list of statements.
/// </summary>
public class ProgramNode : AstNode
{
    public AstNode[] Statements { get; }
    
    public ProgramNode(IEnumerable<AstNode> statements)
    {
        Statements = statements.ToArray();
    }
    
    public override string ToString() =>
        string.Join("\n", Statements.Select(o => o.ToString()));

    public string AsTree()
    {
        var sb = new StringBuilder();
        Recurse(this, string.Empty, true);
        return sb.ToString();

        void Recurse(AstNode node, string indent, bool isLast)
        {
            var marker = isLast ? "└── " : "├── ";
            sb.Append(indent);
            if (sb.Length > 0)
                sb.Append(marker);
            sb.AppendLine(node is ProgramNode ? node.GetType().Name : $"{node.GetType().Name} ({node})");

            var props = node.GetType()
                .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(p => typeof(AstNode).IsAssignableFrom(p.PropertyType) || typeof(IEnumerable<AstNode>).IsAssignableFrom(p.PropertyType));

            var childList = new List<AstNode>();

            foreach (var prop in props)
            {
                var val = prop.GetValue(node);
                if (val is AstNode child)
                    childList.Add(child);
                else if (val is IEnumerable<AstNode> list)
                    childList.AddRange(list);
            }

            for (var i = 0; i < childList.Count; i++)
            {
                var child = childList[i];
                Recurse(child, indent + (isLast ? "    " : "│   "), i == childList.Count - 1);
            }
        }
    }
}

/// <summary>
/// Base class for expression nodes that return a value.
/// All expressions should inherit from this to indicate value-producing behavior.
/// </summary>
public abstract class ExprStatementNode : AstNode
{
}

/// <summary>
/// Represents a variable declaration with an initializer.
/// Does not produce a value at runtime, but assigns a computed expression to a named variable.
/// </summary>
public class AssignmentNode : AstNode
{
    public Token Type { get; }
    public Token Name { get; }
    public ExprStatementNode Value { get; }

    public AssignmentNode(Token type, Token name, ExprStatementNode expr)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = expr;
    }
    
    public override string ToString() =>
        Value == null ? $"{Type.Value} {Name.Value};" : $"{Type.Value} {Name.Value} = {Value}";
}

/// <summary>
/// Represents a literal constant (23, 69.2, etc) in the AST.
/// Produces a value at runtime and is used in expressions.
/// </summary>
public class LiteralNode : ExprStatementNode
{
    public Token Value { get; }

    public LiteralNode(Token literal)
    {
        Value = literal;
    }
    
    public override string ToString() => Value.Value;
}