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
public class TrigTests
{
    [Test]
    public void GivenFloatConstantCheckSin()
    {
        const string code = "sin $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.93).Within(0.01));
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

        Assert.That(vm["a"].Float, Is.EqualTo(0.93).Within(0.01));
    }

    [Test]
    public void GivenVectorCheckSin()
    {
        const string code =
            """
            ld $theta, 1.2, 2.0
            sin $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.93).Within(0.01));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(0.91).Within(0.01));
    }

    [Test]
    public void GivenVectorCheckSinh()
    {
        const string code =
            """
            ld $theta, 1.2, 2.0
            sinh $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(1.51).Within(0.01));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(3.63).Within(0.01));
    }

    [Test]
    public void GivenVectorCheckAsin()
    {
        const string code =
            """
            ld $theta, 0.5, 0.3
            asin $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.52).Within(0.01));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(0.30).Within(0.01));
    }

    [Test]
    public void GivenVectorCheckCos()
    {
        const string code =
            """
            ld $theta, 1.2, 2.0
            cos $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.36).Within(0.01));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(-0.42).Within(0.01));
    }

    [Test]
    public void GivenVectorCheckCosh()
    {
        const string code =
            """
            ld $theta, 1.2, 2.0
            cosh $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(1.81).Within(0.01));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(3.76).Within(0.01));
    }

    [Test]
    public void GivenVectorCheckAcos()
    {
        const string code =
            """
            ld $theta, 0.5, 0.3
            acos $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(1.05).Within(0.01));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(1.27).Within(0.01));
    }

    [Test]
    public void GivenVectorCheckTan()
    {
        const string code =
            """
            ld $theta, 1.2, 2.0
            tan $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(2.57).Within(0.01));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(-2.19).Within(0.01));
    }

    [Test]
    public void GivenVectorCheckTanh()
    {
        const string code =
            """
            ld $theta, 1.2, 2.0
            tanh $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.833).Within(0.01));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(0.964).Within(0.01));
    }

    [Test]
    public void GivenVectorCheckAtan()
    {
        const string code =
            """
            ld $theta, 1.2, 2.0
            atan $a, $theta
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.876).Within(0.01));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(1.107).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckSinh()
    {
        const string code = "sinh $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.51).Within(0.01));
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

        Assert.That(vm["a"].Float, Is.EqualTo(1.51).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckAsin()
    {
        const string code = "asin $a, 0.5";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.524).Within(0.01));
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

        Assert.That(vm["a"].Float, Is.EqualTo(0.524).Within(0.01));
    }

    [Test]
    public void GivenOutOfRangeValueCheckAsinThrows()
    {
        const string code = "asin $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());   
    }
    
    [Test]
    public void GivenFloatConstantCheckCos()
    {
        const string code = "cos $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.36).Within(0.01));
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

        Assert.That(vm["a"].Float, Is.EqualTo(0.36).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckCosh()
    {
        const string code = "cosh $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.81).Within(0.01));
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

        Assert.That(vm["a"].Float, Is.EqualTo(1.81).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckAcos()
    {
        const string code = "acos $a, 0.5";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.047).Within(0.01));
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

        Assert.That(vm["a"].Float, Is.EqualTo(1.047).Within(0.01));
    }

    [Test]
    public void GivenOutOfRangeValueCheckAcosThrows()
    {
        const string code = "acos $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void GivenFloatConstantCheckTan()
    {
        const string code = "tan $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(2.57).Within(0.01));
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

        Assert.That(vm["a"].Float, Is.EqualTo(2.57).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckTanh()
    {
        const string code = "tanh $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.833).Within(0.01));
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

        Assert.That(vm["a"].Float, Is.EqualTo(0.833).Within(0.01));
    }

    [Test]
    public void GivenFloatConstantCheckAtan()
    {
        const string code = "atan $a, 1.2";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.876).Within(0.01));
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

        Assert.That(vm["a"].Float, Is.EqualTo(0.876).Within(0.01));
    }
}