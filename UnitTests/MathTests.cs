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
public class MathTests
{
    [Test]
    public void GivenFloatConstantCheckPow()
    {
        const string code =
            """
            ld $a, 1.5
            pow $a, 2.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(2.25).Within(0.001));
    }

    [Test]
    public void GivenFloatVariableCheckPow()
    {
        const string code =
            """
            ld $a, 4.0
            ld $b, 2.2
            pow $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(21.112).Within(0.001));
    }

    [Test]
    public void GivenFloatConstantCheckExp()
    {
        const string code = "exp $a, 1.5";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(4.481).Within(0.001));
    }

    [Test]
    public void GivenFloatVariableCheckExp()
    {
        const string code =
            """
            ld $b, 2.0
            exp $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(7.389).Within(0.001));
    }

    [Test]
    public void GivenFloatConstantCheckLog()
    {
        const string code = "log $a, 20.0855";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(3.0).Within(0.001));
    }

    [Test]
    public void GivenFloatVariableCheckLog()
    {
        const string code =
            """
            ld $b, 7.3891
            log $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(2.0).Within(0.001));
    }

    [Test]
    public void GivenFloatConstantCheckMod()
    {
        const string code =
            """
            ld $a, 7.5
            mod $a, 2.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(1.5).Within(0.001));
    }

    [Test]
    public void GivenFloatVariableCheckMod()
    {
        const string code =
            """
            ld $a, 8.3
            ld $b, 2.5
            mod $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.8).Within(0.001));
    }

    [Test]
    public void GivenFloatConstantCheckMin()
    {
        const string code =
            """
            ld $a, 3.0
            min $a, 5.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(3.0).Within(0.001));
    }

    [Test]
    public void GivenFloatVariableCheckMin()
    {
        const string code =
            """
            ld $a, 8.0
            ld $b, 3.0
            min $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(3.0).Within(0.001));
    }
    
    [Test]
    public void GivenFloatConstantCheckMax()
    {
        const string code =
            """
            ld $a, 3.0
            max $a, 5.0
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(5.0).Within(0.001));
    }

    [Test]
    public void GivenFloatVariableCheckMax()
    {
        const string code =
            """
            ld $a, 2.0
            ld $b, 9.0
            max $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(9.0).Within(0.001));
    }

    [Test]
    public void GivenFloatVariableCheckSign()
    {
        const string code =
            """
            ld $b, 2.0
            ld $c, 0.0
            sign $a1, $b
            sign $a2, $c
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a1"].FloatValue, Is.EqualTo(1.0).Within(0.001));
        Assert.That(vm["a2"].FloatValue, Is.EqualTo(0.0).Within(0.001));
    }
}