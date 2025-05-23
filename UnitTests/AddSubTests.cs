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

        Assert.That(vm["a"].Int, Is.EqualTo(3));
        Assert.That(vm["b"].Int, Is.EqualTo(2));
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

        Assert.That(vm["a"].Int, Is.EqualTo(-2));
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

        Assert.That(vm["a"].Float, Is.EqualTo(3.75f).Within(0.001));
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

        Assert.That(vm["a"].Float, Is.EqualTo(-1.5f).Within(0.001));
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

        Assert.That(vm["a"].Float, Is.EqualTo(3.5f).Within(0.001));
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

        Assert.That(vm["a"].Float, Is.EqualTo(3.5f).Within(0.001));
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

    [Test, Sequential]
    public void CheckAddingSameSizeVectors([Values("0.2, 0.4, 0.6", "$b")] string rhs)
    {
        var code =
            $"""
             ld $a, 1.0, 2.0, 3.0
             ld $b, 0.2, 0.4, 0.6
             add $a, {rhs}
             """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Vector));
        Assert.That(vm["a"].Floats, Has.Length.EqualTo(3));
        Assert.That(vm["a"].Floats[0], Is.EqualTo(1.2).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(2.4).Within(0.001));
        Assert.That(vm["a"].Floats[2], Is.EqualTo(3.6).Within(0.001));
    }

    [Test, Sequential]
    public void CheckSubtractingSameSizeVectors([Values("0.2, 0.4, 0.6", "$b")] string rhs)
    {
        var code =
            $"""
             ld $a, 1.0, 2.0, 3.0
             ld $b, 0.2, 0.4, 0.6
             sub $a, {rhs}
             """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Vector));
        Assert.That(vm["a"].Floats, Has.Length.EqualTo(3));
        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.8).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(1.6).Within(0.001));
        Assert.That(vm["a"].Floats[2], Is.EqualTo(2.4).Within(0.001));
    }

    [Test, Sequential]
    public void CheckAddingDifferentSizeVectorsThrows([Values("0.2, 0.4", "$b")] string rhs)
    {
        var code =
            $"""
             ld $a, 1.0, 2.0, 3.0
             ld $b, 0.2, 0.4
             add $a, {rhs}
             """;
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test, Sequential]
    public void CheckSubtractingDifferentSizeVectorsThrows([Values("0.2, 0.4", "$b")] string rhs)
    {
        var code =
            $"""
             ld $a, 1.0, 2.0, 3.0
             ld $b, 0.2, 0.4
             sub $a, {rhs}
             """;
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckAddingVectorToFloatExpandsFloat()
    {
        var code =
            """
            ld $a, 1.0
            add $a, 0.2, 0.4, 0.6
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats, Has.Length.EqualTo(3));
    }

    [Test]
    public void CheckSubtractingVectorFromFloatExpandsFloat()
    {
        var code =
            """
            ld $a, 1.0
            sub $a, 0.2, 0.4, 0.6
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats, Has.Length.EqualTo(3));
    }

    [Test]
    public void CheckAddingFloatToVector()
    {
        var code =
            """
            ld $a, 0.2, 0.4, 0.6
            add $a, 1.0 
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats, Has.Length.EqualTo(3));
    }

    [Test]
    public void CheckSubtractingFloatFromVector()
    {
        var code =
            """
            ld $a, 0.2, 0.4, 0.6
            sub $a, 1.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats, Has.Length.EqualTo(3));
    }
}