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
using DTC.GLSLParser;

namespace UnitTests.ParserTests;

[TestFixture]
public class ParserTests : TestsBase
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
    public void ParseConstVariableDeclaration()
    {
        var tokens = m_lexer.Tokenize("const float pi = 3.14159;");
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseMultipleVariableDeclarations()
    {
        var tokens = m_lexer.Tokenize("int a, b = 2, c;");
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
    
    [Test]
    public void ParseForLoopWithVariableDeclaration()
    {
        const string code =
            """
            for (int i = 0; i < 10; i = i + 1)
                a = a + 1.0;
            """;
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseForLoopWithoutVariableDeclaration()
    {
        const string code =
            """
            int i;
            for (i = 0; i < 10; i = i + 1) {
                a = a + 1.0;
            }
            """;
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(2));
    }

    [Test]
    public void ParsePostIncrementOperator()
    {
        const string code = "int a = 1; a++;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(2));
    }
    
    [Test]
    public void ParsePostDecrementOperator()
    {
        const string code = "int a = 1; a--;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(2));   
    }
    
    [Test]
    public void ParsePreIncrementOperator()
    {
        const string code = "int a = 1; ++a;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(2));
    }
    
    [Test]
    public void ParsePreDecrementOperator()
    {
        const string code = "int a = 1; --a;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(2));
    }
    
    [Test]
    public void ParseNegatingOperator()
    {
        const string code = "a = -b;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseNotOperator()
    {
        const string code = "a = !b;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseUnaryOperatorChaining()
    {
        const string code = "a = -- -b;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParsePlusEqualsOperator()
    {
        const string code = "a += 1;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseMinusEqualsOperator()
    {
        const string code = "a -= 1;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseMultiplyEqualsOperator()
    {
        const string code = "a *= 1;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseDivideEqualsOperator()
    {
        const string code = "a /= 1;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseModuloEqualsOperator()
    {
        const string code = "a %= 1;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseXorEqualsOperator()
    {
        const string code = "a ^= 1;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseLogicalAnd()
    {
        const string code = "a > 0 && a <= 100;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseLogicalOr()
    {
        const string code = "a <= 0 || a > 100;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseArrayIndexWithVariable()
    {
        const string code = "a = b[12];";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseArray2DIndexWithVariable()
    {
        const string code = "a = b[12][2];";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseArrayIndexWithExpression()
    {
        const string code = "a = b[3 + 4];";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseArrayDeclaration()
    {
        const string code = "float a[4];";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test, Sequential]
    [TestCase("float[] x = float[] (.1, 0.2, 0.3);")]
    [TestCase("float[] x = float[3] (.1, .2, .3);")]
    [TestCase("float[3] x = float[] (.1, 0.2, 0.3);")]
    [TestCase("float[3] x = float[3] (0.1, 0.2, 0.3);")]
    public void ParseArrayConstruction(string code)
    {
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseCastingIntToFloat()
    {
        const string code = "a = float(69);";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseCastingFloatToInt()
    {
        const string code = "a = int(2.3);";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test, Sequential]
    public void ParseVectorCreation([Values("vec2", "vec3", "vec4", "mat2", "mat3", "mat4", "mat2x2")] string type)
    {
        var code = $"a = {type}(2.3);";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseVectorCreationWithSubVector()
    {
        const string code = "vec4 b = vec4(vec2(1.0), 2.0, 3.0);";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void CheckAccessingRgbVectorComponents()
    {
        const string code = "a = vec3(1.2).rggab;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void CheckAccessingXyzVectorComponents()
    {
        const string code = "a = vec2(1.2).xyyx;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void CheckAccessingStpqVectorComponents()
    {
        const string code = "a = vec2(1.2).stpq;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void CheckAccessingRgbVectorComponentsOnVariable()
    {
        const string code = "a = b.abgr;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void CheckAssigningToSwizzle()
    {
        const string code = "a.xy = vec2(1, 2);";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseComment()
    {
        const string code = "// comment";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Is.Empty);
    }

    [Test]
    public void ParsePiApproximation()
    {
        var code = ProjectDir.GetDir("Examples").GetFile("PiApproximation.c").ReadAllText();
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(2));
    }
    
    [Test]
    public void ParseTernary()
    {
        const string code = "float a = b > 0 ? 1 : -1;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    [TestCase("in")]
    [TestCase("out")]
    [TestCase("inout")]
    public void ParseInOutParams(string modifier)
    {
        var code = $"void f({modifier} vec3 v) {{ }}";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }

    [Test]
    public void ParseShiftRight()
    {
        const string code = "int a = 8 >> 1;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());

        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
    
    [Test]
    public void ParseShiftLeft()
    {
        const string code = "int a = 4 << 2;";
        var tokens = m_lexer.Tokenize(code);
        var program = m_parser.Parse(tokens);
        Console.WriteLine(program.AsTree());
        
        Assert.That(program, Is.Not.Null);
        Assert.That(program.Statements, Has.Length.EqualTo(1));
    }
}