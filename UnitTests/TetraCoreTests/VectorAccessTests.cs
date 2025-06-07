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
public class VectorAccessTests
{
    [Test]
    public void CheckLdWithVectorElement()
    {
        const string code =
            """
            ld $v, 1.1, 2.2, 3.3
            ld $a, $v[1]
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Float, Is.EqualTo(2.2).Within(0.001));
    }

    [Test]
    public void CheckLdWithVectorElements()
    {
        const string code =
            """
            ld $v, 1.1, 2.2, 3.3
            ld $a, $v[1], $v[0]
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
        Assert.That(vm["a"].Floats[0], Is.EqualTo(2.2).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(1.1).Within(0.001));
    }

    [Test]
    public void CheckLdWithMixedVectorElements()
    {
        const string code =
            """
            ld $v, 1.1, 2.2, 3.3
            ld $a, 3.141, $v[0]
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
        Assert.That(vm["a"].Floats[0], Is.EqualTo(3.141).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(1.1).Within(0.001));
    }

    [Test]
    public void CheckLdIntoVectorElement()
    {
        const string code =
            """
            ld $v, 1.1, 2.2, 3.3
            ld $v[1], 3.141
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Length, Is.EqualTo(3));
        Assert.That(vm["v"].Floats[0], Is.EqualTo(1.1).Within(0.001));
        Assert.That(vm["v"].Floats[1], Is.EqualTo(3.141).Within(0.001));
        Assert.That(vm["v"].Floats[2], Is.EqualTo(3.3).Within(0.001));
    }

    [Test]
    public void CheckLdIntoVectorElementOfNonVectorThrows()
    {
        const string code =
            """
            ld $a, 1.1
            ld $a[1], 3.141
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.Exception.TypeOf<RuntimeException>());
    }
    
    [Test]
    public void CheckAccessingVectorElementOfNonVectorThrows()
    {
        const string code =
            """
            ld $a, 1.1
            print $a[1]
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.Exception.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckIncVectorElement()
    {
        const string code =
            """
            ld $v, 1.1, 2.2, 3.3
            inc $v[2]
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Length, Is.EqualTo(3));
        Assert.That(vm["v"].Floats[0], Is.EqualTo(1.1).Within(0.001));
        Assert.That(vm["v"].Floats[1], Is.EqualTo(2.2).Within(0.001));
        Assert.That(vm["v"].Floats[2], Is.EqualTo(4.3).Within(0.001));
    }

    [Test]
    public void CheckMulVectorElements()
    {
        const string code =
            """
            ld $v, 1.1, 2.2, 3.3
            ld $a, 3.4
            mul $a, $v[2]
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(1));
        Assert.That(vm["a"].Float, Is.EqualTo(11.22).Within(0.001));
    }
    
    [Test]
    public void CheckJmpEqWithVectorElement()
    {
        const string code =
            """
                ld $v, 1.1, 2.2, 3.3
                ld $a, 3.4
            loop:
                jmp_eq $a, $v[2], loop
                halt
            """;
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.Nothing);
    }
}