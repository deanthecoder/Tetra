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
public class AddSubTests
{
    [Test]
    public void CheckAddingIntegers()
    {
        const string code =
            """
            ld $a, 1
            ld $b, 2
            add $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].IntValue, Is.EqualTo(3));
        Assert.That(vm["b"].IntValue, Is.EqualTo(2));
    }

    [Test]
    public void CheckSubtractingIntegers()
    {
        const string code =
            """
            ld $a, 3
            ld $b, 5
            sub $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].IntValue, Is.EqualTo(-2));
    }

    [Test]
    public void CheckAddingFloats()
    {
        const string code =
            """
            ld $a, 1.5
            ld $b, 2.25
            add $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(3.75f).Within(0.001));
    }

    [Test]
    public void CheckSubtractingFloats()
    {
        const string code =
            """
            ld $a, 2.0
            ld $b, 3.5
            sub $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(-1.5f).Within(0.001));
    }

    [Test]
    public void CheckAddWithMixedTypes()
    {
        const string code =
            """
            ld $a, 2
            add $a, 1.5
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(3.5f).Within(0.001));
    }

    [Test]
    public void CheckSubWithMixedTypes()
    {
        const string code =
            """
            ld $a, 5.5
            sub $a, 2
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(3.5f).Within(0.001));
    }

    [Test]
    public void CheckAddingIntegerAndFloatUpdatesType()
    {
        const string code =
            """
            ld $a, 1
            add $a, 2.3
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Float));
    }

    [Test]
    public void CheckSubtractingIntegerAndFloatUpdatesType()
    {
        const string code =
            """
            ld $a, 1
            sub $a, 0.3
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Float));
    }
}