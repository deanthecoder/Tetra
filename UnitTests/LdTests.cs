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
public class LdTests
{
    [Test]
    public void CheckLoadingFloat()
    {
        const string code = "ld $a, 1.0";
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("a").Type, Is.EqualTo(OperandType.Float));
        Assert.That(vm.CurrentFrame.GetVariable("a").FloatValue, Is.EqualTo(1.0f));
    }

    [Test]
    public void CheckLoadingInt()
    {
        const string code = "ld $a, 1";
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("a").Type, Is.EqualTo(OperandType.Int));
        Assert.That(vm.CurrentFrame.GetVariable("a").IntValue, Is.EqualTo(1));
    }

    [Test]
    public void CheckCopyingVariable()
    {
        const string code =
            """
            ld $a, 69
            ld $b, $a
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("b").Type, Is.EqualTo(OperandType.Int));
        Assert.That(vm.CurrentFrame.GetVariable("b").IntValue, Is.EqualTo(69));
    }

    [Test]
    public void CheckLoadingVariableWithItselfThrows()
    {
        const string code = "ld $a, $a";
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckOverwritingVariableValue()
    {
        const string code =
            """
                ld $a, 1
                ld $a, 2
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("a").IntValue, Is.EqualTo(2));
    }

    [Test]
    public void CheckOverwritingVariableWithDifferentType()
    {
        const string code =
            """
                ld $a, 1
                ld $a, 2.3
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("a").FloatValue, Is.EqualTo(2.3).Within(0.001));
    }

    [Test]
    public void CheckLoadingFromUndefinedVariableThrows()
    {
        const string code = "ld $a, $b";
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckLoadingegativeNumber()
    {
        const string code = "ld $a, -42.69";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        var variable = vm.CurrentFrame.GetVariable("a");
        Assert.That(variable.FloatValue, Is.EqualTo(-42.69).Within(0.001));
    }

    [Test]
    public void CheckVariableNamesAreCaseSensitive()
    {
        const string code =
            """
                ld $a, 1
                ld $A, -2
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm.CurrentFrame.GetVariable("a").IntValue, Is.EqualTo(1));
        Assert.That(vm.CurrentFrame.GetVariable("A").IntValue, Is.EqualTo(-2));
    }
}