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
using DTC.GLSLParser;
using TetraCore;

namespace UnitTests.EmitterTests;

public class EmitIntrinsicCallTests
{
    [Test]
    public void CheckSin()
    {
        const string code = "float a = sin(1.23);";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.942).Within(0.001));
    }
    
    [Test]
    public void CheckCos()
    {
        const string code = "float a = cos(1.23);";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(0.334).Within(0.001));
    }

    [Test]
    public void CheckTan()
    {
        const string code = "float a = tan(1.23);";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(2.819).Within(0.001));
    }

    [Test]
    public void CheckMin()
    {
        const string code = "float a = min(1.23, 4.56);";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(1.23).Within(0.001));
    }

    [Test]
    public void CheckMax()
    {
        const string code = "float a = max(1.23, 4.56);";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(4.56).Within(0.001));
    }
    
    [Test]
    public void CheckClamp()
    {
        const string code =
            """
            float a = clamp(8.0, 1.23, 4.56);
            float b = clamp(-3.0, 1.23, 4.56);
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(4.56).Within(0.001));
        Assert.That(vm["b"].Float, Is.EqualTo(1.23).Within(0.001));
    }
    
    [Test]
    public void CheckFloor()
    {
        const string code =
            "float a = floor(8.3);";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["a"].Float, Is.EqualTo(8.0).Within(0.001));
    }

    [Test]
    public void CheckMix()
    {
        const string code =
            """
            float a = mix(0.3, 1.1, 7.3);
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Float, Is.EqualTo(2.96).Within(0.001));
    }
}