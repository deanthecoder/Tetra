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
}