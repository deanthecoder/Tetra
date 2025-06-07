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
using TetraCore.Exceptions;

namespace UnitTests.TetraCoreTests;

[TestFixture]
public class DotTests
{
    [Test]
    public void CheckDotProduct3D()
    {
        const string code =
            """
            ld $a, 1.0, 2.0, 3.0
            ld $b, 4.0, 5.0, 6.0
            dot $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Float, Is.EqualTo(32.0f).Within(0.001)); // 1*4 + 2*5 + 3*6
    }

    [Test]
    public void CheckDotProduct2D()
    {
        const string code =
            """
            ld $a, 1.0, 3.0
            dot $a, 4.0, -2.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Float));
        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Float, Is.EqualTo(-2.0f).Within(0.001));
    }

    [Test]
    public void CheckDotMismatchedLengthsThrow()
    {
        const string code =
            """
            ld $a, 1.0, 2.0, 3.0
            ld $b, 4.0, 5.0
            dot $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }
}