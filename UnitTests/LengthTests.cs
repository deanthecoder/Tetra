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

namespace UnitTests;

[TestFixture]
public class LengthTests
{
    [Test]
    public void CheckLengthOfVector()
    {
        const string code =
            """
            ld $a, 1.0, 2.0, 2.0
            length $a, $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Float, Is.EqualTo(3.0f).Within(0.001));
    }

    [Test]
    public void CheckLengthOfZeroVector()
    {
        const string code =
            """
            ld $a, 0, 0
            length $a, $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Float, Is.EqualTo(0.0f).Within(0.001));
    }

    [Test]
    public void CheckLengthOfFloat()
    {
        const string code = "length $a, 7.1";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Float, Is.EqualTo(7.1f).Within(0.001));
    }
    
    [Test]
    public void CheckLengthOfInt()
    {
        const string code = "length $a, 3, 4";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Int, Is.EqualTo(5));
    }

    [Test]
    public void CheckLengthOfConstantThrows()
    {
        Assert.That(() => Assembler.Assemble("length 6.9, 1.2"), Throws.TypeOf<SyntaxErrorException>());
        Assert.That(() => Assembler.Assemble("length 6, 1.2"), Throws.TypeOf<SyntaxErrorException>());
    }
}