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
using DTC.GLSLParser;
using TetraCore;

namespace UnitTests.EmitterTests;

[TestFixture]
public class EmitPiApprpximationTest : TestsBase
{
    [Test]
    public void EmitPiApproximationCode()
    {
        var code = ProjectDir.GetDir("Examples").GetFile("PiApproximation.c").ReadAllText();

        string tetraCode = null;
        Assert.That(() => tetraCode = Compiler.CompileToTetraSource(code, "main"), Throws.Nothing);

        Assert.That(tetraCode, Is.Not.Null);
        Assert.That(tetraCode, Is.Not.Empty);

        Program program = null;
        Assert.That(() => program = Assembler.Assemble(tetraCode), Throws.Nothing);
        var vm = new TetraVm(program);
        vm.Debug = true;
        vm.Run();

        Assert.That(vm["retval"].AsFloat(), Is.EqualTo(3.141).Within(0.01));
    }
}