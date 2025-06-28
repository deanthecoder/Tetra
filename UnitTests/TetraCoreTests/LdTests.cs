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
public class LdTests
{
    [Test]
    public void CheckLoadingFloat()
    {
        const string code = "ld $a, 1.0";
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Float));
        Assert.That(vm["a"].Float, Is.EqualTo(1.0f));
    }

    [Test]
    public void CheckLoadingInt()
    {
        const string code = "ld $a, 1";
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Int));
        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }

    [Test]
    public void CheckLoadingVector()
    {
        const string code = "ld $v, 1.1, -2.0, 3.0, 4.2";
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["v"].Type, Is.EqualTo(OperandType.Vector));
        Assert.That(vm["v"].Floats[0], Is.EqualTo(1.1).Within(0.001));
        Assert.That(vm["v"].Floats[1], Is.EqualTo(-2.0).Within(0.001));
        Assert.That(vm["v"].Floats[2], Is.EqualTo(3.0).Within(0.001));
        Assert.That(vm["v"].Floats[3], Is.EqualTo(4.2).Within(0.001));
    }

    [Test]
    public void CheckLoadingVectorFromVariable()
    {
        const string code =
            """
            ld $a, 1.0
            ld $b, 2.0
            ld $v, $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Type, Is.EqualTo(OperandType.Vector));
        Assert.That(vm["v"].Floats[0], Is.EqualTo(1.0).Within(0.001));
        Assert.That(vm["v"].Floats[1], Is.EqualTo(2.0).Within(0.001));
    }
    
    [Test]
    public void CheckLoadingVectorFromMixedNumericTypes()
    {
        const string code =
            """
            ld $a, 1.0
            ld $v, $a, 2.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Type, Is.EqualTo(OperandType.Vector));
        Assert.That(vm["v"].Floats[0], Is.EqualTo(1.0).Within(0.001));
        Assert.That(vm["v"].Floats[1], Is.EqualTo(2.0).Within(0.001));
    }
    
    [Test]
    public void CheckLoadingVectorFromSmallerVectors()
    {
        const string code =
            """
            ld $a, 1.0
            ld $b, 2.0, 3.0
            ld $v, $a, $b, 4.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Length, Is.EqualTo(4));
    }

    [Test]
    public void CheckExpandingFloatToVector()
    {
        const string code =
            """
            ld $a, 1.0
            dim $a, 4
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Vector));
        Assert.That(vm["a"].Floats, Is.EqualTo(new[] { 1.0, 1.0, 1.0, 1.0 }).Within(0.001));
    }

    [Test]
    public void CheckTruncatingVectorToFloat()
    {
        const string code =
            """
            ld $a, 1.0, 2.0
            dim $a, 1
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Type, Is.EqualTo(OperandType.Float));
        Assert.That(vm["a"].Float, Is.EqualTo(1.0).Within(0.001));
    }

    [Test]
    public void CheckExpandingVec2ToVec4Throws()
    {
        const string code =
            """
            ld $a, 1.0, 2.0
            dim $a, 4
            """;
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckSettingZeroVectorLengthThrows()
    {
        const string code =
            """
            ld $a, 1.0, 2.0
            dim $a, 0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
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

        Assert.That(vm["b"].Type, Is.EqualTo(OperandType.Int));
        Assert.That(vm["b"].Int, Is.EqualTo(69));
    }
    
    [Test]
    public void CheckCopyingVectorVariable()
    {
        const string code =
            """
            ld $a, 69.0, 23.2
            ld $b, $a
            """;
        var instructions = Assembler.Assemble(code);
        var vm = new TetraVm(instructions);

        vm.Run();

        Assert.That(vm["b"].Length, Is.EqualTo(2));
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

        Assert.That(vm["a"].Int, Is.EqualTo(2));
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

        Assert.That(vm["a"].Float, Is.EqualTo(2.3).Within(0.001));
    }
    
    [Test]
    public void CheckOverwritingVectorWithFloat()
    {
        const string code =
            """
                ld $a, 1.1, 2.2
                ld $a, 3.3
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(1));
    }
    
    [Test]
    public void CheckOverwritingFloatWithVector()
    {
        const string code =
            """
                ld $a, 1.1
                ld $a, 2.2, 3.3
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
    }

    [Test]
    public void CheckLoadingFromUndefinedVariableThrows()
    {
        const string code = "ld $a, $b";
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckLoadingNegativeNumber()
    {
        const string code = "ld $a, -42.69";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        var variable = vm["a"];
        Assert.That(variable.Float, Is.EqualTo(-42.69).Within(0.001));
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

        Assert.That(vm["a"].Int, Is.EqualTo(1));
        Assert.That(vm["A"].Int, Is.EqualTo(-2));
    }

    [Test]
    public void CheckAssigningFloatToSingleVectorComponent()
    {
        const string code =
            """
            ld $v, 1.0, 2.0
            ld $v.y, 3.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Floats, Is.EqualTo(new[] { 1.0f, 3.0f }));
    }
    
    [Test]
    public void CheckAssigningFloatToMultiVectorComponent()
    {
        const string code =
            """
            ld $v, 1.0, 2.0
            ld $v.xy, 3.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Floats, Is.EqualTo(new[] { 3.0f, 3.0f }));
    }

    [Test]
    public void CheckAssigningFloatsToMultiVectorComponent()
    {
        const string code =
            """
            ld $v, 1.0, 2.0
            ld $v.yx, 3.0, 4.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["v"].Floats, Is.EqualTo(new[] { 4.0f, 3.0f }));
    }
}