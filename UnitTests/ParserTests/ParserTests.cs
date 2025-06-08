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
using DTC.GLSLLexer;
using DTC.GLSLParser;

namespace UnitTests.ParserTests;

[TestFixture]
public class ParserTests
{
    private Lexer m_lexer;
    private Parser m_parser;

    [SetUp]
    public void Setup()
    {
        m_lexer = new Lexer();
        m_parser = new Parser();
    }
    
    [Test]
    public void CheckValidConstruction()
    {
        Assert.That(() => new Parser(), Throws.Nothing);
    }

    [Test]
    public void GivenNullInputCheckParsingThrows()
    {
        Assert.That(() => m_parser.Parse(null), Throws.ArgumentNullException);   
    }
    
    [Test]
    public void GivenEmptyInputCheckParsingThrows()
    {
        Assert.That(() => m_parser.Parse([]), Throws.ArgumentException);   
    }

    [Test]
    public void ParseSimpleAssignment()
    {
        var tokens = m_lexer.Tokenize("float a = 23.4f;");
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseAssignmentWithAddition()
    {
        var tokens = m_lexer.Tokenize("float b = a + 1.0;");
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseAssignmentWithAdditionAndMultiplication()
    {
        var tokens = m_lexer.Tokenize("float c = a + 1.0 * b;");
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseAssignmentWithMultiplicationAndAddition()
    {
        var tokens = m_lexer.Tokenize("float c = a * 2.0 + b;");
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseAssignmentWithBracketedMath()
    {
        var tokens = m_lexer.Tokenize("float a = (1.0 + 2.0) * 3.0;");
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseVariableDeclaration()
    {
        var tokens = m_lexer.Tokenize("int a;");
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseCodeBlock()
    {
        const string code =
            """
            {
                float a = 1.0;
                float b = a + 2.0;
            }
            """;
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseAssignmentWithBinaryExpression()
    {
        const string code =
            """
            float a = 1.0;
            float b = 2.0;
            a = a + b;
            """;
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(3));
    }
    
    [Test]
    public void ParseParameterlessFunctionReturningVoid()
    {
        const string code =
            """
            void main() {
            }
            """;
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseParameterlessFunctionReturningVoidStatement()
    {
        const string code =
            """
            void main() {
                return;
            }
            """;
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseParameterlessFunctionReturningFloatLiteral()
    {
        const string code =
            """
            float main() {
                return 2.3;
            }
            """;
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseParameterlessFunctionReturningFloatVariable()
    {
        const string code =
            """
            float main() {
                float a = 1.0;
                return a;
            }
            """;
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseFunctionWithParameters()
    {
        const string code = "float sum(int a, float b) { return a + b; }";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseFunctionCall()
    {
        const string code = "float sum = add(1.0, 2.0);";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseIfStatementWithBraces()
    {
        const string code = "if (true) { return 1.0; }";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseIfStatementWithoutBraces()
    {
        const string code = "if (true) return;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseIfStatementWithElse()
    {
        const string code = "if (doSomething() == 1.2) a = 1; else a = 2;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseWhileLoopWithBraces()
    {
        const string code = "while (a == 2) { a = a + 1; }";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseWhileLoopWithoutBraces()
    {
        const string code = "while (a == 2) a = a - 1;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
}