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

namespace UnitTests;

[TestFixture]
public class SmoothStepTests
{
    [Test]
    public void CheckSmoothStepMidRange()
    {
        const string code =
            """
            ld $a, 1.5
            smoothstep $a, 1.0, 2.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.5f).Within(0.01f)); // smoothstep(0.5) ≈ 0.5
    }

    [Test]
    public void CheckSmoothStepBelowEdge0()
    {
        const string code =
            """
            ld $a, 0.5
            ld $edge0, 1.0
            ld $edge1, 2.0
            smoothstep $a, $edge0, $edge1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.0f).Within(0.001));
    }

    [Test]
    public void CheckSmoothStepAboveEdge1()
    {
        const string code =
            """
            ld $a, 2.5
            ld $edge0, 1.0
            ld $edge1, 2.0
            smoothstep $a, $edge0, $edge1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.0f).Within(0.001));
    }
}