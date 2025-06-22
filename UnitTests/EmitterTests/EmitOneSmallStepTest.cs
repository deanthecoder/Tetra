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
using System.Text;
using DTC.Core.Extensions;
using DTC.Core.UnitTesting;
using DTC.GLSLParser;

namespace UnitTests.EmitterTests;

[TestFixture]
public class EmitOneSmallStepTest : TestsBase
{
    [Test]
    public void EmitOneSmallStepCode()
    {
        var code = ProjectDir.GetDir("Examples").GetFile("OneSmallStep.glsl").ReadAllText();

        var codeWithHeader = new StringBuilder();
        codeWithHeader.AppendLine("vec4 main() {");
        codeWithHeader.AppendLine("    vec3 rgba;");
        codeWithHeader.AppendLine("    float iTime = 0.0;");
        codeWithHeader.AppendLine("    vec2 iResolution = vec2(640, 480);");
        codeWithHeader.AppendLine("    mainImage(rgba, vec2(0));");
        codeWithHeader.AppendLine("    return rgba;");
        codeWithHeader.AppendLine("}");
        codeWithHeader.AppendLine();
        codeWithHeader.AppendLine(code);

        string tetraCode = null;
        Assert.That(() => tetraCode = Compiler.CompileToTetraSource(codeWithHeader.ToString()), Throws.Nothing);
        Assert.That(tetraCode, Is.Not.Null);
        Assert.That(tetraCode, Is.Not.Empty);
    }
}