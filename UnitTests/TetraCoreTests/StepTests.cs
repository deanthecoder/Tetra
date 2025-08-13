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
using TetraCore;

namespace UnitTests.TetraCoreTests;

[TestFixture]
public class StepTests
{
    [Test]
    public void CheckStepOnEdge()
    {
        const string code =
            """
            ld $a, 1.0
            step $a, 1.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.0f).Within(0.01f));
    }

    [Test]
    public void CheckStepBelowEdge()
    {
        const string code =
            """
            ld $a, 0.5
            step $a, 0.6
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.0f).Within(0.001));
    }

    [Test]
    public void CheckStepAboveEdge()
    {
        const string code =
            """
            ld $a, 0.5
            step $a, 0.2
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.0f).Within(0.01f));
    }
}