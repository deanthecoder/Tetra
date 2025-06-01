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
public class ClampTests
{
    [Test]
    public void CheckClampingFloatInRange()
    {
        const string code =
            """
            ld $a, 5.0
            clamp $a, 1.1, 10.2
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(5.0f).Within(0.001));
    }
    
    [Test]
    public void CheckClampingInteger()
    {
        const string code =
            """
            ld $a, 5
            clamp $a, 3, 6
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Int));
        Assert.That(vm["a"].Float, Is.EqualTo(5));
    }

    [Test]
    public void CheckClampingFloatBelowRange()
    {
        const string code =
            """
            ld $a, -3.0
            ld $from, 1.0
            ld $to, 10.0
            clamp $a, $from, $to
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.0f).Within(0.001));
    }

    [Test]
    public void CheckClampingFloatAboveRange()
    {
        const string code =
            """
            ld $a, 12.0
            ld $from, 1.0
            ld $to, 10.0
            clamp $a, $from, $to
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(10.0f).Within(0.001));
    }

    [Test]
    public void CheckClampingFloatEqualToFrom()
    {
        const string code =
            """
            ld $a, 1.0
            ld $from, 1.0
            ld $to, 10.0
            clamp $a, $from, $to
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.0f).Within(0.001));
    }

    [Test]
    public void CheckClampingFloatEqualToTo()
    {
        const string code =
            """
            ld $a, 10.0
            ld $from, 1.0
            ld $to, 10.0
            clamp $a, $from, $to
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(10.0f).Within(0.001));
    }
    
    [Test]
    public void CheckClampingVector()
    {
        const string code =
            """
            ld $a, 0.5, 2.5, 5.0
            ld $from, 1.0, 2.0, 4.0
            ld $to,   2.0, 3.0, 6.0
            clamp $a, $from, $to
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(3));
        Assert.That(vm["a"].Floats[0], Is.EqualTo(1.0f).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(2.5f).Within(0.001));
        Assert.That(vm["a"].Floats[2], Is.EqualTo(5.0f).Within(0.001));
    }
    
    [Test]
    public void CheckClampingMismatchedTargetVectorThrows()
    {
        const string code =
            """
            ld $a, 0.5, 2.5
            ld $from, 1.0, 2.0, 4.0
            ld $to,   2.0, 3.0, 6.0
            clamp $a, $from, $to
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }
    
    [Test]
    public void CheckClampingMismatchTargetFromToVectorsThrows()
    {
        const string code =
            """
            ld $a, 0.5, 2.5, 1.2
            ld $from, 1.0, 2.0
            ld $to,   2.0, 3.0, 6.0
            clamp $a, $from, $to
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }
}