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

        Assert.That(vm["a"].Float, Is.EqualTo(2.25).Within(0.001));
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

        Assert.That(vm["a"].Float, Is.EqualTo(21.112).Within(0.001));
    }

    [Test]
    public void GivenVectorsCheckPow()
    {
        const string code =
            """
            ld $a, 4.0, 2.0
            ld $b, 2.2, 3.0
            pow $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(21.112).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(8.0).Within(0.001));
    }
    
    [Test]
    public void GivenFloatAndVectorsCheckPow()
    {
        const string code =
            """
            ld $a, 2.0
            ld $b, 2.2, 3.0
            pow $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
    }

    [Test]
    public void GivenOutOfRangeValueCheckPowThrows()
    {
        const string code =
            """
            ld $a, 0
            pow $a, -1.2
            """;
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void GivenFloatConstantCheckExp()
    {
        const string code = "exp $a, 1.5";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(4.481).Within(0.001));
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

        Assert.That(vm["a"].Float, Is.EqualTo(7.389).Within(0.001));
    }

    [Test]
    public void GivenVectorCheckExp()
    {
        const string code =
            """
            ld $b, 2.0, 3.0
            exp $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(7.389).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(20.086).Within(0.001));
    }

    [Test]
    public void GivenFloatConstantCheckLog()
    {
        const string code = "log $a, 20.0855";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(3.0).Within(0.001));
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

        Assert.That(vm["a"].Float, Is.EqualTo(2.0).Within(0.001));
    }

    [Test]
    public void GivenVectorCheckLog()
    {
        const string code =
            """
            ld $b, 7.3891, 2.3
            log $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(2.0).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(0.833).Within(0.001));
    }

    [Test]
    public void GivenOutOfRangeValueCheckLogThrows()
    {
        const string code = "log $a, -0.23";
        var vm = new TetraVm(Assembler.Assemble(code));

        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
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

        Assert.That(vm["a"].Float, Is.EqualTo(1.5).Within(0.001));
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

        Assert.That(vm["a"].Float, Is.EqualTo(0.8).Within(0.001));
    }

    [Test]
    public void GivenVectorsCheckMod()
    {
        const string code =
            """
            ld $a, 8.3, 2.2
            ld $b, 2.5, 1.2
            mod $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.8).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(1.0).Within(0.001));
    }
    
    [Test]
    public void GivenFloatAndVectorCheckMod()
    {
        const string code =
            """
            ld $a, 8.3
            ld $b, 2.5, 1.2
            mod $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
    }
    
    [Test]
    public void GivenVectorAndFloatCheckMod()
    {
        const string code =
            """
            ld $a, 8.3, 2.3
            ld $b, 2.5
            mod $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
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

        Assert.That(vm["a"].Float, Is.EqualTo(3.0).Within(0.001));
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

        Assert.That(vm["a"].Float, Is.EqualTo(3.0).Within(0.001));
    }

    [Test]
    public void GivenVectorsCheckMin()
    {
        const string code =
            """
            ld $a, 8.0, 3.0
            ld $b, 6.0, 5.0
            min $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(6.0).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(3.0).Within(0.001));
    }
    
    [Test]
    public void GivenFloatAndVectorCheckMin()
    {
        const string code =
            """
            ld $a, 8.0
            ld $b, 6.0, 5.0
            min $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
    }
    
    [Test]
    public void GivenVectorAndFloatCheckMin()
    {
        const string code =
            """
            ld $a, 8.0, 3.0
            ld $b, 7.0
            min $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
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

        Assert.That(vm["a"].Float, Is.EqualTo(5.0).Within(0.001));
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

        Assert.That(vm["a"].Float, Is.EqualTo(9.0).Within(0.001));
    }

    [Test]
    public void GivenVectorsCheckMax()
    {
        const string code =
            """
            ld $a, 8.0, 3.0
            ld $b, 6.0, 5.0
            max $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(8.0).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(5.0).Within(0.001));
    }

    [Test]
    public void GivenFloatAndVectorCheckMax()
    {
        const string code =
            """
            ld $a, 8.0
            ld $b, 6.0, 5.0
            max $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
    }
    
    [Test]
    public void GivenVectorAndFloatCheckMax()
    {
        const string code =
            """
            ld $a, 8.0, 3.0
            ld $b, 7.0
            max $a, $b
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(2));
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

        Assert.That(vm["a1"].Float, Is.EqualTo(1.0).Within(0.001));
        Assert.That(vm["a2"].Float, Is.EqualTo(0.0).Within(0.001));
    }
    
    [Test]
    public void GivenVectorCheckSign()
    {
        const string code = "sign $a, 1.0, -0.0, -3.0";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Length, Is.EqualTo(3));
        Assert.That(vm["a"].Floats[0], Is.EqualTo(1.0).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.Zero.Within(0.001));
        Assert.That(vm["a"].Floats[2], Is.EqualTo(-1.0).Within(0.001));
    }

    [Test]
    public void GivenFloatCheckSqrt()
    {
        const string code = "sqrt $a, 2.0";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm["a"].Float, Is.EqualTo(1.414).Within(0.001));
    }

    [Test]
    public void GivenVectorCheckSqrt()
    {
        const string code = "sqrt $a, 2.0, 3.0";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm["a"].Floats[0], Is.EqualTo(1.414).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(1.732).Within(0.001));
    }

    [Test]
    public void GivenZeroCheckSqrt()
    {
        const string code = "sqrt $a, 0.0";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm["a"].Float, Is.EqualTo(0.0).Within(0.001));
    }

    [Test]
    public void GivenNegativeCheckSqrtThrows()
    {
        const string code = "sqrt $a, -1.0";
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void GivenFloatConstantCheckCeil()
    {
        const string code = "ceil $a, 1.3";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(2.0).Within(0.001));
    }

    [Test]
    public void GivenVectorCheckCeil()
    {
        const string code = "ceil $a, 1.3, 2.7";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(2.0).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(3.0).Within(0.001));
    }

    [Test]
    public void GivenFloatConstantCheckFract()
    {
        const string code = "fract $a, 1.3";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.3).Within(0.001));
    }

    [Test]
    public void GivenVectorCheckFract()
    {
        const string code = "fract $a, 1.3, 2.7";
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Floats[0], Is.EqualTo(0.3).Within(0.001));
        Assert.That(vm["a"].Floats[1], Is.EqualTo(0.7).Within(0.001));
    }
}