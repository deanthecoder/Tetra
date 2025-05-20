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
public class TrigTests
{
    [Test]
    public void GivenFloatConstantCheckSin()
    {
        const string code = "sin $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.93).Within(0.01));
    }
    
    [Test]
    public void GivenFloatVariableCheckSin()
    {
        const string code =
            """
            ld $theta, 1.2
            sin $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.93).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckSinh()
    {
        const string code = "sinh $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(1.51).Within(0.01));
    }

    [Test]
    public void GivenFloatVariableCheckSinh()
    {
        const string code =
            """
            ld $theta, 1.2
            sinh $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(1.51).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckAsin()
    {
        const string code = "asin $a, 0.5";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.524).Within(0.01));
    }

    [Test]
    public void GivenFloatVariableCheckAsin()
    {
        const string code =
            """
            ld $theta, 0.5
            asin $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.524).Within(0.01));
    }
    
    [Test]
    public void GivenFloatConstantCheckCos()
    {
        const string code = "cos $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.36).Within(0.01));
    }

    [Test]
    public void GivenFloatVariableCheckCos()
    {
        const string code =
            """
            ld $theta, 1.2
            cos $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.36).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckCosh()
    {
        const string code = "cosh $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(1.81).Within(0.01));
    }

    [Test]
    public void GivenFloatVariableCheckCosh()
    {
        const string code =
            """
            ld $theta, 1.2
            cosh $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(1.81).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckAcos()
    {
        const string code = "acos $a, 0.5";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(1.047).Within(0.01));
    }

    [Test]
    public void GivenFloatVariableCheckAcos()
    {
        const string code =
            """
            ld $theta, 0.5
            acos $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(1.047).Within(0.01));
    }
    
    [Test]
    public void GivenFloatConstantCheckTan()
    {
        const string code = "tan $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(2.57).Within(0.01));
    }

    [Test]
    public void GivenFloatVariableCheckTan()
    {
        const string code =
            """
            ld $theta, 1.2
            tan $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(2.57).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckTanh()
    {
        const string code = "tanh $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.833).Within(0.01));
    }

    [Test]
    public void GivenFloatVariableCheckTanh()
    {
        const string code =
            """
            ld $theta, 1.2
            tanh $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.833).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckAtan()
    {
        const string code = "atan $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.876).Within(0.01));
    }

    [Test]
    public void GivenFloatVariableCheckAtan()
    {
        const string code =
            """
            ld $theta, 1.2
            atan $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].FloatValue, Is.EqualTo(0.876).Within(0.01));
    }
}