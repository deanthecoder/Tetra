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
    private readonly Dictionary<TokenType, int> m_precedence = new()
    {
        [TokenType.Asterisk] = 5,
        [TokenType.Slash] = 5,

        [TokenType.Plus] = 4,
        [TokenType.Minus] = 4,

        [TokenType.EqualsEquals] = 3,
        [TokenType.NotEqual] = 3,
        [TokenType.LessThan] = 3,
        [TokenType.GreaterThan] = 3,
        [TokenType.LessThanOrEqual] = 3,
        [TokenType.GreaterThanOrEqual] = 3,

        [TokenType.AndAnd] = 2,
        [TokenType.OrOr] = 2,

        [TokenType.Equals] = 1,
        [TokenType.PlusEqual] = 1,
        [TokenType.MinusEqual] = 1,
        [TokenType.AsteriskEqual] = 1,
        [TokenType.SlashEqual] = 1,
        [TokenType.PercentEqual] = 1,
        [TokenType.CaretEquals] =10
    };
    
    private Token[] m_tokens;
    private int m_tokenIndex;
    
    private Token CurrentToken
    {
        get
        {
            if (m_tokenIndex >= m_tokens.Length)
                throw new ParseException("Unexpected end of token stream reached.");
            return m_tokens[m_tokenIndex];
        }
    }

    public ProgramNode Parse(Token[] tokens)
    {
        if (tokens == null)
            throw new ArgumentNullException(nameof(tokens));
        if (tokens.Length == 0)
            throw new ArgumentException("Tokens array cannot be empty.", nameof(tokens));
        
        m_tokens = tokens.Where(o => o.Type != TokenType.Comment).ToArray();
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

    private bool Peek(TokenType checkType, int offset = 0)
    {
        if (m_tokenIndex + offset >= m_tokens.Length)
            return false;
        return m_tokens[m_tokenIndex + offset].Type == checkType;
    }

    private AstNode ParseStatement()
    {
        if (Peek(TokenType.Keyword))
        {
            var isConst = Peek(TokenType.Keyword) && CurrentToken.Value == "const";
            if (isConst)
                Consume(); // skip 'const'

            if (IsTypeKeyword(CurrentToken.Value))
            {
                // Peek ahead — function or variable?
                var isFunction = Peek(TokenType.LeftParen, 2);
                if (isFunction)
                {
                    // Parse as function declaration.
                    return ParseFunctionDeclaration();
                }
                
                // Parse as variable declaration (float a = ...)
                return ParseVariableDeclaration(isConst);
            }

            switch (CurrentToken.Value)
            {
                case "for":
                    return ParseForStatement();
                case "if":
                    return ParseIfStatement();
                case "return":
                    return ProcessReturnStatement();
                case "while":
                    return ParseWhileStatement();
            }
        }

        if (IsExpressionStart(CurrentToken.Type))
        {
            // Parse as expression (e.g. assignment 'a = a + b')
            var expr = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after expression.");
            return new ExpressionStatementNode(expr);
        }

        if (Peek(TokenType.LeftBrace))
            return ParseBlock();
        
        throw new ParseException($"Unexpected token '{CurrentToken.Value}' at start of statement.");
    }

    private static bool IsExpressionStart(TokenType type) =>
        type == TokenType.Identifier ||
        type == TokenType.IntLiteral ||
        type == TokenType.FloatLiteral ||
        type == TokenType.TrueLiteral ||
        type == TokenType.FalseLiteral ||
        type == TokenType.LeftParen ||
        type == TokenType.Minus ||
        type == TokenType.Exclamation ||
        type == TokenType.Increment ||
        type == TokenType.Decrement;

    private ReturnNode ProcessReturnStatement()
    {
        Consume(TokenType.Keyword, "Expected 'return'");
        
        var expr = Peek(TokenType.Semicolon) ? null : ParseExpression();
        Consume(TokenType.Semicolon, "Expected ';' after return statement.");
        
        return new ReturnNode(expr);
    }

    private IfNode ParseIfStatement()
    {
        Consume(TokenType.Keyword, "Expected 'if'");
        Consume(TokenType.LeftParen, "Expected '(' after 'if'");
        var condition = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after 'if' condition");

        var thenStmt = ParseStatement(); // block or single statement
        
        // See if there's an 'else'...
        AstNode elseStmt = null;
        if (Peek(TokenType.Keyword) && CurrentToken.Value == "else")
        {
            Consume(); // Consume 'else'
            elseStmt = ParseStatement(); // block or single statement
        }

        return new IfNode(condition, thenStmt, elseStmt);
    }
    
    private WhileNode ParseWhileStatement()
    {
        Consume(TokenType.Keyword, "Expected 'while'");
        Consume(TokenType.LeftParen, "Expected '(' after 'while'");
        var condition = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after 'while' condition");

        var loopStmt = ParseStatement(); // block or single statement
        
        return new WhileNode(condition, loopStmt);
    }

    private ForNode ParseForStatement()
    {
        Consume(TokenType.Keyword, "Expected 'for'");
        Consume(TokenType.LeftParen, "Expected '(' after 'for'");

        // Init: either a declaration or an expression statement
        AstNode init;
        if (CurrentToken.Type == TokenType.Keyword && IsTypeKeyword(CurrentToken.Value))
        {
            init = ParseVariableDeclaration();
        }
        else
        {
            init = new ExpressionStatementNode(ParseExpression());
            Consume(TokenType.Semicolon);
        }

        // Condition
        var condition = ParseExpression();
        Consume(TokenType.Semicolon, "Expected ';' after 'for' loop condition");

        // Step
        var step = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after 'for' loop clause");

        // Body
        var body = ParseStatement();

        return new ForNode(init, condition, step, body);
    }
    
    private static bool IsTypeKeyword(string token) =>
        token is "float" or "int" or "void" or "bool" or "vec2" or "vec3" or "vec4" or "mat2" or "mat3" or "mat4";

    private BlockNode ParseBlock()
    {
        Consume(TokenType.LeftBrace, "Expected '{' at start of block");
        
        var statements = new List<AstNode>();
        while (CurrentToken.Type != TokenType.RightBrace)
            statements.Add(ParseStatement());
        
        Consume(TokenType.RightBrace, "Expected '}' at end of block");
        
        return new BlockNode(statements.ToArray());
    }
    
    private ExprStatementNode ParseExpression(int parentPrecedence = 0)
    {
        var left = ParseUnaryExpression();

        while (true)
        {
            var op = CurrentToken;
            if (!m_precedence.TryGetValue(op.Type, out var precedence) || precedence <= parentPrecedence)
                break; // No operator, or operator precedence is lower than or equal to the current.

            Consume(); // Consume the operator

            var right = ParseExpression(precedence);

            left = op.Type switch
            {
                // Convert += (and friends) into a regular assignment.
                TokenType.PlusEqual or TokenType.MinusEqual or TokenType.AsteriskEqual or TokenType.SlashEqual or TokenType.PercentEqual or TokenType.CaretEquals =>
                    new AssignmentNode(left, ConvertCompoundToBinary(op), new BinaryExprNode(left, ConvertCompoundToBinary(op), right)),

                TokenType.Equals =>
                    new AssignmentNode(left, op, right),

                _ => new BinaryExprNode(left, op, right)
            };

            // Handle ternary after combining binary.
            if (!Peek(TokenType.Question))
                continue;
            
            Consume(); // consume '?'
            var thenExpr = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' in ternary expression");
            var elseExpr = ParseExpression();
            left = new TernaryNode(left, thenExpr, elseExpr);
        }

        return left;
    }


    private static Token ConvertCompoundToBinary(Token token) =>
        token.Type switch
        {
            TokenType.PlusEqual => new Token(TokenType.Plus, token.Line, "+"),
            TokenType.MinusEqual => new Token(TokenType.Minus, token.Line, "-"),
            TokenType.AsteriskEqual => new Token(TokenType.Asterisk, token.Line, "*"),
            TokenType.SlashEqual => new Token(TokenType.Slash, token.Line, "/"),
            TokenType.PercentEqual => new Token(TokenType.Percent, token.Line, "%"),
            TokenType.CaretEquals => new Token(TokenType.Caret, token.Line, "^"),
            _ => throw new InvalidOperationException($"Unexpected compound token: {token}")
        };

    /// <summary>
    /// Parses a unary expression such as -x, !x, ++x, or --x.
    /// Supports chaining (e.g., -- -x) by recursively parsing the operand.
    /// </summary>
    private ExprStatementNode ParseUnaryExpression()
    {
        var token = CurrentToken;

        if (token.Type is TokenType.Minus or TokenType.Exclamation or TokenType.Increment or TokenType.Decrement)
        {
            Consume();
            var operand = ParseUnaryExpression(); // allow chaining: `-- -x`
            return new UnaryExprNode(token, operand, isPostfix: false);
        }

        return ParsePrimaryExpression();
    }

    private ExprStatementNode ParseBasePrimaryExpression()
    {
        var token = Consume();
        switch (token.Type)
        {
            case TokenType.IntLiteral or TokenType.FloatLiteral or TokenType.TrueLiteral or TokenType.FalseLiteral:
                return new LiteralNode(token);
            
            case TokenType.Identifier:
                {
                    if (Peek(TokenType.LeftParen))
                    {
                        // We have a function call.
                        return ParseFunctionCall(token);
                    }
                    
                    ExprStatementNode variableNode = new VariableNode(token);

                    // Support postfix ++/-- operators.
                    if (Peek(TokenType.Increment) || Peek(TokenType.Decrement))
                    {
                        var op = Consume();
                        return new UnaryExprNode(op, variableNode, isPostfix: true);
                    }
                    
                    // Support array access.
                    while (Peek(TokenType.LeftBracket))
                    {
                        Consume(TokenType.LeftBracket);
                        var indexExpr = ParseExpression();
                        Consume(TokenType.RightBracket, "Expected ']' after array size expression");
                        variableNode = new IndexExprNode(variableNode, indexExpr);
                    }

                    // Variable reference.
                    return variableNode;
                }
            
            case TokenType.Keyword:
                // Support type constructors or array (E.g. vec3(...), or float[3])
                if (Lexer.TypeNames.Contains(token.Value))
                {
                    if (Peek(TokenType.LeftParen))
                    {
                        // We have a constructor call.
                        return ParseFunctionCall(token);
                    }

                    if (Peek(TokenType.LeftBracket))
                    {
                        // We have an array of an integrated type. (float[3]).
                        Consume(TokenType.LeftBracket);
                        var sizeExpr = Peek(TokenType.RightBracket) ? null : ParseExpression();
                        Consume(TokenType.RightBracket, "Expected ']' after array type size");

                        // Expect a constructor call after the type.
                        Consume(TokenType.LeftParen, "Expected '(' after sized array type");

                        var args = new List<ExprStatementNode>();
                        if (!Peek(TokenType.RightParen))
                        {
                            do
                            {
                                args.Add(ParseExpression());
                            } while (Peek(TokenType.Comma) && Consume() != null);
                        }

                        Consume(TokenType.RightParen, "Expected ')' to close constructor arguments");

                        return new ArrayConstructorCallNode(token, sizeExpr, args.ToArray());
                    }
                }

                break;
            
            case TokenType.LeftParen:
                return ParseParenthesizedExpression();
            
            case TokenType.Decrement or TokenType.Increment or TokenType.Minus or TokenType.Exclamation:
            {
                var operand = ParsePrimaryExpression();
                return new UnaryExprNode(token, operand, isPostfix: false);
            }
        }
        
        throw new ParseException($"Unexpected token '{token.Value}' in expression.");
    }
    
    private ExprStatementNode ParsePrimaryExpression()
    {
        var expr = ParseBasePrimaryExpression();
        
        // Support swizzle access on a variable (e.g. .xy, .rgba)
        while (Peek(TokenType.Dot) && Peek(TokenType.Identifier, 1))
        {
            Consume(TokenType.Dot);
            var swizzleToken = Consume(TokenType.Identifier, "Expected swizzle name after '.'");
            expr = new SwizzleExprNode(expr, swizzleToken);
        }
        
        return expr;
    }

    private CallExprNode ParseFunctionCall(Token nameToken)
    {
        Consume(TokenType.LeftParen, "Expected '(' after function name");

        var args = new List<ExprStatementNode>();
        if (!Peek(TokenType.RightParen))
        {
            do
            {
                var arg = ParseExpression();
                args.Add(arg);
            }
            while (Peek(TokenType.Comma) && Consume() != null);
        }

        Consume(TokenType.RightParen, "Expected ')' after argument list");

        var isTypeConstructor = Lexer.TypeNames.Contains(nameToken.Value);
        return isTypeConstructor ? new ConstructorCallNode(nameToken, args.ToArray()) : new CallExprNode(nameToken, args.ToArray());
    }

    private AstNode ParseVariableDeclaration(bool isConst = false)
    {
        var typeToken = Consume(TokenType.Keyword);
        
        // Squash any array brackets directly after the variable type (E.g. float[] a = ...)
        // Tetra won't care about them.
        if (Peek(TokenType.LeftBracket))
        {
            Consume(TokenType.LeftBracket);
            while (!Peek(TokenType.RightBracket))
                Consume();
            Consume(TokenType.RightBracket);
        }

        var assignments = new List<VariableDeclarationNode>();
        do
        {
            var nameToken = Consume(TokenType.Identifier, "Expected variable name");

            // Support array declarations.
            ExprStatementNode arraySize = null;
            if (Peek(TokenType.LeftBracket))
            {
                Consume(TokenType.LeftBracket);
                arraySize = ParseExpression();
                Consume(TokenType.RightBracket, "Expected ']' after array size expression");
            }

            VariableDeclarationNode node;
            if (CurrentToken.Type == TokenType.Equals)
            {
                Consume(TokenType.Equals, "Expected '=' after variable name");
                var expr = ParseExpression();
                node = new VariableDeclarationNode(typeToken, nameToken, expr, isConst: isConst);
            }
            else
            {
                // Declaration without an initializer.
                node = new VariableDeclarationNode(typeToken, nameToken, arraySize: arraySize, isConst: isConst);
            }
            assignments.Add(node);
        } while (Peek(TokenType.Comma) && Consume() != null);

        Consume(TokenType.Semicolon, "Expected ';' after variable declaration(s)");
        
        return assignments.Count == 1 ? assignments[0] : new MultiVariableDeclarationNode(assignments.ToArray());
    }

    private FunctionNode ParseFunctionDeclaration()
    {
        var returnType = Consume(TokenType.Keyword, "Expected return type");
        var name = Consume(TokenType.Identifier, "Expected function name");
        var parameters = new List<ParameterNode>();

        Consume(TokenType.LeftParen, "Expected '(' after function name");
        var hasParams = !Peek(TokenType.RightParen);
        if (hasParams)
        {
            do
            {
                Token modifier = null;
                if (Peek(TokenType.Keyword) && CurrentToken.Value is "in" or "out" or "inout")
                    modifier = Consume(TokenType.Keyword);
                
                var type = Consume(TokenType.Keyword, "Expected parameter type");
                var ident = Consume(TokenType.Identifier, "Expected parameter name");
                parameters.Add(new ParameterNode(modifier, type, ident));
            }
            while (Peek(TokenType.Comma) && Consume() != null);
        }

        Consume(TokenType.RightParen, "Expected ')' after parameter list");

        var body = ParseBlock();

        return new FunctionNode(returnType, name, parameters.ToArray(), body);
    }    
    
    private ExprStatementNode ParseParenthesizedExpression()
    {
        var expr = ParseExpression();
        
        Consume(TokenType.RightParen, "Expected ')' after expression");
        return expr;
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
/// Represents a variable declaration with or without an initializer.
/// Does not produce a value at runtime but assigns a computed expression to a named variable.
/// </summary>
public class VariableDeclarationNode : AstNode
{
    public bool IsConst { get; }
    public Token Type { get; }
    public Token Name { get; }
    public ExprStatementNode Value { get; }
    public ExprStatementNode ArraySize { get; }

    public VariableDeclarationNode(Token type, Token name, ExprStatementNode expr = null, ExprStatementNode arraySize = null, bool isConst = false)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = expr;
        ArraySize = arraySize;
        IsConst = isConst;
    }
    
    public override string ToString()
    {
        var arrayPart = ArraySize != null ? $"[{ArraySize}]" : string.Empty;
        var constPrefix = IsConst ? "const " : string.Empty;
        return Value == null ? $"{constPrefix}{Type.Value} {Name.Value}{arrayPart}" : $"{constPrefix}{Type.Value} {Name.Value}{arrayPart} = {Value}";
    }
}

/// <summary>
/// A multi-declaration version of VariableDeclarationNode.
/// </summary>
public class MultiVariableDeclarationNode : AstNode
{
    public Token Type => Declarations[0].Type;
    public VariableDeclarationNode[] Declarations { get; }

    public MultiVariableDeclarationNode(VariableDeclarationNode[] declarations)
    {
        Declarations = declarations;
    }

    public override string ToString() =>
        $"{Type.Value} {string.Join(", ", Declarations.Select(d => d.ToString())).Replace($"{Type.Value} ", string.Empty)};";
}

/// <summary>
/// Represents an assignment expression like `a = b + 1`.
/// </summary>
public class AssignmentNode : ExprStatementNode
{
    public ExprStatementNode Target { get; }
    public Token Operator { get; }
    public ExprStatementNode Value { get; }

    public AssignmentNode(ExprStatementNode target, Token op, ExprStatementNode value)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Operator = op ?? throw new ArgumentNullException(nameof(op));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString() => $"{Target} = {Value}";
}

/// <summary>
/// Represents a literal constant (23, 69.2, etc.) in the AST.
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

/// <summary>
/// Represents a variable reference in an expression.
/// </summary>
public class VariableNode : ExprStatementNode
{
    public Token Name { get; }
    
    public VariableNode(Token token)
    {
        Name = token;
    }

    public override string ToString() => Name.Value;
}

/// <summary>
/// Represents a binary expression like a + b or x * 2.
/// Produces a value at runtime.
/// </summary>
public class BinaryExprNode : ExprStatementNode
{
    public ExprStatementNode Left { get; }
    public Token Operator { get; }
    public ExprStatementNode Right { get; }

    public BinaryExprNode(ExprStatementNode left, Token op, ExprStatementNode right)
    {
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Operator = op ?? throw new ArgumentNullException(nameof(op));
        Right = right ?? throw new ArgumentNullException(nameof(right));
    }

    public override string ToString() => $"({Left} {Operator.Value} {Right})";
}

/// <summary>
/// Represents a block of statements enclosed in `{...}`.
/// Does not return a value; just groups statements together.
/// </summary>
public class BlockNode : AstNode
{
    public AstNode[] Statements { get; }

    public BlockNode(AstNode[] statements)
    {
        Statements = statements ?? throw new ArgumentNullException(nameof(statements));
    }

    public override string ToString() => "{ ... }";
}

/// <summary>
/// Wraps an expression when it appears as a standalone statement.
/// </summary>
public class ExpressionStatementNode : AstNode
{
    public ExprStatementNode Expression { get; }
    
    public ExpressionStatementNode(ExprStatementNode expr)
    {
        Expression = expr ?? throw new ArgumentNullException(nameof(expr));
    }

    public override string ToString() => Expression.ToString();
}


/// <summary>
/// Represents a function declaration in the AST.
/// Contains the function's return type, name, and body block of statements.
/// Example: float main() { ... }
/// </summary>
public class FunctionNode : AstNode
{
    public Token ReturnType { get; }
    public Token Name { get; }
    public ParameterNode[] Parameters { get; }
    public BlockNode Body { get; }

    public FunctionNode(Token returnType, Token name, ParameterNode[] parameters, BlockNode body)
    {
        ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        Body = body ?? throw new ArgumentNullException(nameof(body));
    }

    public override string ToString()
    {
        var paramList = string.Join(", ", Parameters.Select(o => o.ToString()));
        return $"{ReturnType.Value} {Name.Value}({paramList}) {{ ... }}";
    }
}

/// <summary>
/// Represents a single function parameter (e.g., float x)
/// </summary>
public class ParameterNode : AstNode
{
    public Token Modifier { get; }
    public Token Type { get; }
    public Token Name { get; }

    public ParameterNode(Token modifier, Token type, Token name)
    {
        Modifier = modifier;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public override string ToString() =>
        Modifier != null ? $"{Modifier.Value} {Type.Value} {Name.Value}" : $"{Type.Value} {Name.Value}";
}

/// <summary>
/// Represents a return statement, optionally with a return expression.
/// </summary>
public class ReturnNode : AstNode
{
    public ExprStatementNode Value { get; }

    public ReturnNode(ExprStatementNode value)
    {
        Value = value;
    }

    public override string ToString() => Value == null ? "return" : $"return {Value}";
}

/// <summary>
/// Represents a function call expression, e.g., add(x, y)
/// </summary>
public class CallExprNode : ExprStatementNode
{
    public Token FunctionName { get; }
    public ExprStatementNode[] Arguments { get; }

    public CallExprNode(Token name, ExprStatementNode[] args)
    {
        FunctionName = name ?? throw new ArgumentNullException(nameof(name));
        Arguments = args ?? throw new ArgumentNullException(nameof(args));
    }

    public override string ToString() =>
        $"{FunctionName.Value}({string.Join(", ", Arguments.Select(o => o.ToString()))})";
}

/// <summary>
/// A special case of a function call node, specifically for calling a type constructor (E.g. vec3(1, 2, 3))
/// </summary>
public class ConstructorCallNode : CallExprNode
{
    public ConstructorCallNode(Token name, ExprStatementNode[] args) : base(name, args)
    {
    }
}

/// <summary>
/// Represents an `if` or `if-else` statement.
/// </summary>
public class IfNode : AstNode
{
    public ExprStatementNode Condition { get; }
    public AstNode ThenBlock { get; }
    public AstNode ElseBlock { get; }

    public IfNode(ExprStatementNode condition, AstNode thenBlock, AstNode elseBlock)
    {
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        ThenBlock = thenBlock ?? throw new ArgumentNullException(nameof(thenBlock));
        ElseBlock = elseBlock; // null if no else
    }

    public override string ToString() =>
        ElseBlock == null ? $"if ({Condition}) {ThenBlock}" : $"if ({Condition}) {ThenBlock} else {ElseBlock}";
}

/// <summary>
/// Represents a conditional (ternary) expression in the form 'condition ? thenExpr : elseExpr'.
/// Returns one of two expressions based on a condition's evaluation.
/// </summary>
public class TernaryNode : ExprStatementNode
{
    public ExprStatementNode Condition { get; }
    public ExprStatementNode ThenExpr { get; }
    public ExprStatementNode ElseExpr { get; }

    public TernaryNode(ExprStatementNode condition, ExprStatementNode thenExpr, ExprStatementNode elseExpr)
    {
        Condition = condition;
        ThenExpr = thenExpr;
        ElseExpr = elseExpr;
    }

    public override string ToString() => $"{Condition} ? {ThenExpr} : {ElseExpr}";
}

/// <summary>
/// Represents a `while` statement.
/// </summary>
public class WhileNode : AstNode
{
    public ExprStatementNode Condition { get; }
    public AstNode LoopBlock { get; }

    public WhileNode(ExprStatementNode condition, AstNode loopBlock)
    {
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        LoopBlock = loopBlock ?? throw new ArgumentNullException(nameof(loopBlock));
    }

    public override string ToString() =>
        $"while ({Condition}) {LoopBlock}";
}

/// <summary>
/// Represents a `for` loop, including init, condition, step, and body.
/// </summary>
public class ForNode : AstNode
{
    public AstNode Init { get; }
    public ExprStatementNode Condition { get; }
    public ExprStatementNode Step { get; }
    public AstNode Body { get; }

    public ForNode(AstNode init, ExprStatementNode condition, ExprStatementNode step, AstNode body)
    {
        Init = init ?? throw new ArgumentNullException(nameof(init));
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        Step = step ?? throw new ArgumentNullException(nameof(step));
        Body = body ?? throw new ArgumentNullException(nameof(body));
    }

    public override string ToString() =>
        $"for ({Init}; {Condition}; {Step}) {Body}";
}

/// <summary>
/// Represents a unary expression like ++x, x--, !x, etc.
/// </summary>
public class UnaryExprNode : ExprStatementNode
{
    public Token Operator { get; }
    public ExprStatementNode Operand { get; }
    public bool IsPostfix { get; }

    public UnaryExprNode(Token op, ExprStatementNode operand, bool isPostfix = false)
    {
        Operator = op ?? throw new ArgumentNullException(nameof(op));
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
        IsPostfix = isPostfix;
    }

    public override string ToString() =>
        IsPostfix ? $"{Operand}{Operator.Value}" : $"{Operator.Value}{Operand}";
}


/// <summary>
/// Represents an array index expression like arr[i].
/// Used for accessing array elements by their index.
/// </summary>
public class IndexExprNode : ExprStatementNode
{
    public ExprStatementNode Target { get; }
    public ExprStatementNode Index { get; }

    public IndexExprNode(ExprStatementNode target, ExprStatementNode index)
    {
        Target = target;
        Index = index;
    }

    public override string ToString() => $"{Target}[{Index}]";
}

/// <summary>
/// Represents a swizzle expression like vec.xy or color.rgba
/// </summary>
public class SwizzleExprNode : ExprStatementNode
{
    public ExprStatementNode Target { get; }
    public Token Swizzle { get; }

    public SwizzleExprNode(ExprStatementNode target, Token swizzle)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Swizzle = swizzle ?? throw new ArgumentNullException(nameof(swizzle));
    }

    public override string ToString() => $"{Target}.{Swizzle.Value}";
}

/// <summary>
/// Represents a constructor call for a fixed-size array, like float[3](1.0, 2.0, 3.0)
/// </summary>
public class ArrayConstructorCallNode : ExprStatementNode
{
    public Token ElementType { get; }
    public ExprStatementNode Size { get; }
    public ExprStatementNode[] Arguments { get; }

    public ArrayConstructorCallNode(Token elementType, ExprStatementNode size, ExprStatementNode[] args)
    {
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        Size = size;
        Arguments = args;
    }

    public override string ToString() =>
        $"{ElementType.Value}[{Size}]({string.Join(", ", Arguments.Select(a => a.ToString()))})";
}

