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
using TetraCore;

namespace UnitTests.EmitterTests;

[TestFixture]
public class EmitOneSmallStepTest : TestsBase
{
    [TestCase(0, 0, 0.086, 0.082, 0.079, 0.0)]
    [TestCase(320, 240, 0.360, 0.343, 0.334, 0.0)]
    [TestCase(639, 479, 0.117, 0.111, 0.108, 0.0)]
    public void EmitOneSmallStepCode(int x, int y, double r, double g, double b, double a)
    {
        var code = ProjectDir.GetDir("Examples").GetFile("OneSmallStep.glsl").ReadAllText();

        var codeWithHeader = new StringBuilder();
        codeWithHeader.AppendLine("uniform vec2 fragCoord;");
        codeWithHeader.AppendLine("uniform vec2 iResolution;");
        codeWithHeader.AppendLine("uniform float iTime;");
        codeWithHeader.AppendLine();
        codeWithHeader.AppendLine("vec4 main() {");
        codeWithHeader.AppendLine("    vec4 rgba;");
        codeWithHeader.AppendLine("    mainImage(rgba, fragCoord);");
        codeWithHeader.AppendLine("    return rgba;");
        codeWithHeader.AppendLine("}");
        codeWithHeader.AppendLine();
        codeWithHeader.AppendLine(code);

        string tetraCode = null;
        Assert.That(() => tetraCode = Compiler.CompileToTetraSource(codeWithHeader.ToString(), "main"), Throws.Nothing);
        Assert.That(tetraCode, Is.Not.Null);
        Assert.That(tetraCode, Is.Not.Empty);

        var program = Assembler.Assemble(tetraCode).Optimize();
        
        var vm = new TetraVm(program);
        vm.AddUniform("fragCoord", new Operand(x, y));
        vm.AddUniform("iResolution", new Operand(640.0f, 480.0f));
        vm.AddUniform("iTime", new Operand(0.0f));

        Assert.That(() => vm.Run(), Throws.Nothing);

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(vm["retval"].Floats[0], Is.EqualTo(r).Within(0.001));
                Assert.That(vm["retval"].Floats[1], Is.EqualTo(g).Within(0.001)); 
                Assert.That(vm["retval"].Floats[2], Is.EqualTo(b).Within(0.001));
                Assert.That(vm["retval"].Floats[3], Is.EqualTo(a).Within(0.001));
            });
        }
        catch (Exception)
        {
            program.Dump();
            throw;
        }
        
        program.Dump();
    }
}