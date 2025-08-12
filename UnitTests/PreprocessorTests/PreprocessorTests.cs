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

using DTC.Preprocessor;

namespace UnitTests.PreprocessorTests;

[TestFixture]
public class PreprocessorTests
{
    [Test]
    public void CheckCodeWithNoDefinitions()
    {
        const string code = """
                            void main()
                            {
                            }
                            """;
        var result = new Preprocessor().Preprocess(code);
        Assert.That(result, Is.EqualTo(code));
    }

    [Test]
    public void CheckStandaloneDefineStatement()
    {
        const string code = "#define FOO 23";
        var result = new Preprocessor().Preprocess(code);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void CheckSimpleDefineInCode()
    {
        const string code = """
                            #define FOO 23
                            void main() { int x = FOO; }
                            """;
        var result = new Preprocessor().Preprocess(code);
        Assert.That(result, Is.EqualTo("void main() { int x = 23; }"));
    }
    
    [Test]
    public void CheckMultipleDefinitionUsage()
    {
        const string code = """
                            #define FOO 23
                            void main() { int x = FOO + FOO; }
                            """;
        var result = new Preprocessor().Preprocess(code);
        Assert.That(result, Is.EqualTo("void main() { int x = 23 + 23; }"));
    }
    
    [Test]
    public void CheckDefineWithComment()
    {
        const string code = """
                            #define FOO 23 // a comment
                            void main() { int x = FOO; }
                            """;
        var result = new Preprocessor().Preprocess(code);
        Assert.That(result, Is.EqualTo("void main() { int x = 23; }"));
    }
    
    [Test]
    public void CheckDefineWithSingleArgumentSingleUseArg()
    {
        const string code = """
                            #define FOO(x) x + 2
                            void main() { int x = FOO(5); }
                            """;
        var result = new Preprocessor().Preprocess(code);
        Assert.That(result, Is.EqualTo("void main() { int x = 5 + 2; }"));
    }
    
    [Test]
    public void CheckDefineWithSingleArgumentMultiUseArg()
    {
        const string code = """
                            #define FOO(x) x * x
                            void main() { int x = FOO(5); }
                            """;
        var result = new Preprocessor().Preprocess(code);
        Assert.That(result, Is.EqualTo("void main() { int x = 5 * 5; }"));
    }
    
    [Test]
    public void CheckDefiningMacroUsedOnLineWithBrackets()
    {
        const string code = """
                            #define sat(x) clamp(x, 0.0, 1.0)
                            void main(float a) { float x = sat(0.5) + (0.5 * 2); }
                            """;
        var result = new Preprocessor().Preprocess(code);
        Assert.That(result, Is.EqualTo("void main(float a) { float x = clamp(0.5, 0.0, 1.0) + (0.5 * 2); }"));
    }
    
    [Test]
    public void CheckDefiningNestedMacro()
    {
        const string code = """
                            #define sat(x) clamp(x, 0.0, 1.0)
                            void main(float a) { float x = sat(sat(0.5)); }
                            """;
        var result = new Preprocessor().Preprocess(code);
        Assert.That(result, Is.EqualTo("void main(float a) { float x = clamp(clamp(0.5, 0.0, 1.0), 0.0, 1.0); }"));
    }
}