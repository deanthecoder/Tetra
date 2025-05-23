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
public class IncDecTests
{
    [Test]
    public void CheckIncrementingInteger()
    {
        const string code =
            """
            ld $a, 1
            inc $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(2));
    }

    [Test]
    public void CheckDecrementingInteger()
    {
        const string code =
            """
            ld $a, 0
            dec $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(-1));
    }

    [Test]
    public void CheckIncrementingFloat()
    {
        const string code =
            """
            ld $a, 1.5
            inc $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(2.5f).Within(0.001));
    }

    [Test]
    public void CheckDecrementingFloat()
    {
        const string code =
            """
            ld $a, 2.1
            dec $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.1f).Within(0.001));
    }

    [Test]
    public void CheckIncrementingVector()
    {
        const string code =
            """
            ld $a, 2.1, 2.9
            inc $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(3.1f).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(3.9f).Within(0.001));
    }
    
    [Test]
    public void CheckDecrementingVector()
    {
        const string code =
            """
            ld $a, 2.1, 2.9
            dec $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(1.1f).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(1.9f).Within(0.001));   
    }
}