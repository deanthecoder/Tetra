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
public class RefractTests
{
    [Test]
    public void CheckRefractVector()
    {
        const string code =
            """
            ld $a, 0.0, -1.0, 0.0       # Incident vector
            ld $n, 0.0, 1.0, 0.0        # Surface normal
            ld $eta, 0.5                # Refraction index ratio
            refract $a, $n, $eta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(3));
        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.0f).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(-1.0f).Within(0.001));
        Assert.That(vm["a"].Floats[2], Is.EqualTo(0.0f).Within(0.001));
    }

    [Test]
    public void CheckRefractFloatThrows()
    {
        const string code =
            """
            ld $a, 1.0
            ld $n, 0.0, 1.0, 0.0
            ld $eta, 0.5
            refract $a, $n, $eta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckRefractInvalidNormalThrows()
    {
        const string code =
            """
            ld $a, 0.0, -1.0, 0.0
            ld $n, 0.0, 1.0             # Invalid: only 2D
            ld $eta, 0.5
            refract $a, $n, $eta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }
}