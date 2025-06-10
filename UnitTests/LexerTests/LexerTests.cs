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
using DTC.Core.Extensions;
using DTC.Core.UnitTesting;
using DTC.GLSLLexer;

namespace UnitTests.LexerTests;

[TestFixture]
public class LexerTests : TestsBase
{
    private Lexer m_lexer;

    [SetUp]
    public void Setup() =>
        m_lexer = new Lexer();

    [Test]
    public void CheckValidConstruction()
    {
        Assert.That(() => new Lexer(), Throws.Nothing);
    }
    
    [Test]
    public void GivenNullStringCheckTokenizingThrows()
    {
        Assert.That(() => m_lexer.Tokenize(null), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    [TestCase("2369", TokenType.IntLiteral)]
    [TestCase("123.456", TokenType.FloatLiteral)]
    [TestCase(".456", TokenType.FloatLiteral)]
    [TestCase("2.", TokenType.FloatLiteral)]
    [TestCase("123.456e-12", TokenType.FloatLiteral)]
    [TestCase("123.456e+12", TokenType.FloatLiteral)]
    [TestCase("123.456e12", TokenType.FloatLiteral)]
    [TestCase("123.456E12", TokenType.FloatLiteral)]
    [TestCase("true", TokenType.TrueLiteral)]
    [TestCase("false", TokenType.FalseLiteral)]
    public void CheckTokenizingLiterals(string code, TokenType type)
    {
        var token = m_lexer.Tokenize(code).Single();

        Assert.That(token.Type, Is.EqualTo(type));
        Assert.That(token.Value, Is.EqualTo(code));
    }

    [Test]
    public void CheckTokenizingSwizzleAfterVariable()
    {
        const string code = "a.stpq";
        var tokens = m_lexer.Tokenize(code);

        Assert.That(tokens, Has.Length.EqualTo(3));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Dot));
        Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Identifier));
    }

    [Test]
    public void CheckTokenizingSwizzleAfterCall()
    {
        const string code = "vec3(1).stpq";
        var tokens = m_lexer.Tokenize(code);

        Assert.That(tokens, Has.Length.EqualTo(6));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Keyword));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.LeftParen));
        Assert.That(tokens[2].Type, Is.EqualTo(TokenType.IntLiteral));
        Assert.That(tokens[3].Type, Is.EqualTo(TokenType.RightParen));
        Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Dot));
        Assert.That(tokens[5].Type, Is.EqualTo(TokenType.Identifier));
    }

    [Test]
    public void CheckTokenizingNumberWithFloatSuffix()
    {
        var token = m_lexer.Tokenize("123.456f").Single();
        
        Assert.That(token.Type, Is.EqualTo(TokenType.FloatLiteral));
        Assert.That(token.Value, Is.EqualTo("123.456"));
    }
    
    [Test]
    public void CheckTokenizingBracketedFloat()
    {
        var tokens = m_lexer.Tokenize("(1.2)");
        
        Assert.That(tokens, Has.Length.EqualTo(3));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.LeftParen));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.FloatLiteral));
        Assert.That(tokens[2].Type, Is.EqualTo(TokenType.RightParen));
    }
    
    [Test]
    public void CheckTokenizingBracketedDotFloat()
    {
        var tokens = m_lexer.Tokenize("(.2)");
        
        Assert.That(tokens, Has.Length.EqualTo(3));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.LeftParen));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.FloatLiteral));
        Assert.That(tokens[2].Type, Is.EqualTo(TokenType.RightParen));
    }

    [Test]
    [TestCase("float")]
    [TestCase("vec2")]
    [TestCase("uniform")]
    [TestCase("return")]
    [TestCase("if")]
    [TestCase("else")]
    public void CheckTokenizingKeywords(string code)
    {
        var token = m_lexer.Tokenize(code).Single();

        Assert.That(token.Type, Is.EqualTo(TokenType.Keyword));
        Assert.That(token.Value, Is.EqualTo(code));
    }

    [Test]
    [TestCase("abc")]
    [TestCase("_abc")]
    [TestCase("a_b_c")]
    [TestCase("a2b3")]
    [TestCase("_123")]
    public void CheckTokenizingIdentifiers(string code)
    {
        var token = m_lexer.Tokenize(code).Single();

        Assert.That(token.Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(token.Value, Is.EqualTo(code));
    }

    [Test]
    [TestCase("++", TokenType.Increment)]
    [TestCase("--", TokenType.Decrement)]
    [TestCase("+=", TokenType.PlusEqual)]
    [TestCase("-=", TokenType.MinusEqual)]
    [TestCase("*=", TokenType.AsteriskEqual)]
    [TestCase("/=", TokenType.SlashEqual)]
    [TestCase("<=", TokenType.LessThanOrEqual)]
    [TestCase(">=", TokenType.GreaterThanOrEqual)]
    [TestCase("%", TokenType.Percent)]
    [TestCase("%=", TokenType.PercentEqual)]
    [TestCase("^", TokenType.Caret)]
    [TestCase("^=", TokenType.CaretEquals)]
    [TestCase("!", TokenType.Exclamation)]
    [TestCase("~", TokenType.Tilde)]
    [TestCase("?", TokenType.Question)]
    [TestCase(":", TokenType.Colon)]
    public void CheckTokenizingOperators(string code, TokenType expectedType)
    {
        var token = m_lexer.Tokenize(code).Single();

        Assert.That(token.Type, Is.EqualTo(expectedType));
        Assert.That(token.Value, Is.EqualTo(code));
    }

    [Test]
    [TestCase("@")]
    [TestCase("#")]
    [TestCase("$")]
    public void CheckUnknownCharactersAreTokenized(string code)
    {
        var token = m_lexer.Tokenize(code).Single();

        Assert.That(token.Type, Is.EqualTo(TokenType.Unknown));
        Assert.That(token.Value, Is.EqualTo(code));
    }

    [Test]
    public void CheckMultiLineCode()
    {
        const string code =
            """
            123
            45.6
            """;
        var tokens = m_lexer.Tokenize(code);
        
        Assert.That(tokens, Has.Length.EqualTo(2));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.IntLiteral));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.FloatLiteral));
    }

    [Test]
    public void CheckTokenizingSingleLineComments()
    {
        const string code =
            """
            // This is a comment with 1 number.
            23.4 // End of line comment.
            """;
        var tokens = m_lexer.Tokenize(code);
        
        Assert.That(tokens, Has.Length.EqualTo(3));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Comment));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.FloatLiteral));
        Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Comment));
    }

    [Test]
    public void CheckTokenizingMultiLineComments()
    {
        const string code =
            """
            /* Comment //
            Continuing
            ... */
            23.4
            """;
        var tokens = m_lexer.Tokenize(code);
        
        Assert.That(tokens, Has.Length.EqualTo(2));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Comment));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.FloatLiteral));
    }
    
    [Test]
    public void CheckTokenizingForLoop()
    {
        const string code =
            """
            for (int i = 0; i < 10; i++)
            {
                sum += i;
            }
            """;
        var tokens = m_lexer.Tokenize(code);

        Assert.That(tokens, Has.Length.GreaterThan(1));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Keyword));
        Assert.That(tokens[0].Value, Is.EqualTo("for"));
        Assert.That(tokens.Select(o => o.Type), Does.Not.Contain(TokenType.Unknown));
    }

    [Test]
    public void CheckTokenizingOneSmallStep()
    {
        var directoryInfo = ProjectDir.Parent.GetDir("TetraShade/Examples");
        var code = directoryInfo.GetFile("OneSmallStep.glsl").ReadAllText();
        var tokens = m_lexer.Tokenize(code);
        Assert.That(tokens.Select(o => o.Type), Does.Not.Contain(TokenType.Unknown));
    }
}