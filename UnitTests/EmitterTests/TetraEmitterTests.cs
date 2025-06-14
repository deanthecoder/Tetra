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

using DTC.Core.Extensions;
using DTC.Core.UnitTesting;
using DTC.GLSLParser;
using TetraCore;

namespace UnitTests.EmitterTests;

[TestFixture]
public class TetraEmitterTests : TestsBase
{
    [Test]
    public void CheckValidConstruction()
    {
        Assert.That(() => new TetraEmitter(), Throws.Nothing);
    }

    [Test]
    public void EmitVoidFunctionDeclaration()
    {
        const string code = "void foo() { }";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("foo:"));
        Assert.That(tetraCode, Does.Contain("ret"));
    }
    
    [Test]
    public void EmitVoidFunctionDeclarationWithParams()
    {
        const string code = "void foo(int a, in int b) { }";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("foo:"));
        Assert.That(tetraCode, Does.Contain("ld $a, $arg0"));
        Assert.That(tetraCode, Does.Contain("ld $b, $arg1"));
        Assert.That(tetraCode, Does.Contain("ret"));
    }
    
    [Test]
    public void EmitFloatFunctionReturningLiteral()
    {
        const string code = "float foo() { return 1.0; }";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("foo:"));
        Assert.That(tetraCode, Does.Contain("ret 1.0"));
    }

    [Test]
    public void EmitFloatFunctionReturningVariable()
    {
        const string code = "float foo() { int a = 1; return a; }";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("foo:"));
        Assert.That(tetraCode, Does.Contain("ret $a"));
    }
    
    [Test]
    public void EmitFloatFunctionReturningExpression()
    {
        const string code = "float foo() { return 1 + 2; }";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("foo:"));
        Assert.That(tetraCode, Does.Contain("ld $tmp0, 1"));
        Assert.That(tetraCode, Does.Contain("add $tmp0, 2"));
        Assert.That(tetraCode, Does.Contain("ret $tmp0"));
    }

    [Test]
    public void EmitFunctionCallWithNoParams()
    {
        const string code =
            """
            void main() { foo(); }
            void foo() { } 
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);

        Assert.That(tetraCode, Does.Contain("call foo"));
    }
    
    [Test]
    public void EmitFunctionCallWithParams()
    {
        const string code =
            """
            void main() { foo(6, 9.2); }
            void foo(int a, float b) { } 
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);

        Assert.That(tetraCode, Does.Contain("ld $arg0, 6"));
        Assert.That(tetraCode, Does.Contain("ld $arg1, 9.2"));
        Assert.That(tetraCode, Does.Contain("call foo"));
        Assert.That(tetraCode, Does.Contain("ret"));
    }    
    
    [Test]
    public void EmitFunctionCallWithReturnValue()
    {
        const string code =
            """
            void main() { int a = foo(); }
            int foo() { return 23; } 
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);

        Assert.That(tetraCode, Does.Contain("call foo"));
        Assert.That(tetraCode, Does.Contain("ld $a, $retval"));
    }

    [Test]
    public void EmitMathOperations()
    {
        const string code =
            """
            int i = 0;
            int denominator = 2 * i + 1;
            float term = 1.0 / denominator;
            term *= -2;
            """;
        const string expected =
            """
            decl $i
            ld $i, 0
            decl $denominator
            ld $tmp1, 2
            mul $tmp1, $i
            ld $tmp0, $tmp1
            add $tmp0, 1
            ld $denominator, $tmp0
            decl $term
            ld $tmp2, 1.0
            div $tmp2, $denominator
            ld $term, $tmp2
            ld $tmp3, $term
            ld $tmp4, 2
            neg $tmp4
            mul $tmp3, $tmp4
            ld $term, $tmp3
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);

        Assert.That(tetraCode.TrimEnd(), Is.EqualTo(expected.TrimEnd()));
    }

    [Test]
    public void EmitPrefixOperator()
    {
        const string code = "int i = 1; int j = --i;";
        const string expected =
            """
            decl $i
            ld $i, 1
            decl $j
            dec $i
            ld $j, $i
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);

        Assert.That(tetraCode.TrimEnd(), Is.EqualTo(expected.TrimEnd()));
    }
    
    [Test]
    public void EmitPostfixOperator()
    {
        const string code = "int i = 1; int j = i--;";
        const string expected =
            """
            decl $i
            ld $i, 1
            decl $j
            ld $tmp0, $i
            dec $i
            ld $j, $tmp0
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);

        Assert.That(tetraCode.TrimEnd(), Is.EqualTo(expected.TrimEnd()));
    }

    [Test]
    public void CheckForLoop()
    {
        const string code =
            """
            int sum = 0;
            for (int i = 0; i < 5; ++i) {
                sum += i;
            }
            """;
        const string expected =
            """
            decl $sum
            ld $sum, 0
            push_frame
            decl $i
            ld $i, 0
            for_start_0:
            ld $tmp0, $i
            lt $tmp0, 5
            jmp_z $tmp0, for_end_0
            ld $tmp1, $sum
            add $tmp1, $i
            ld $sum, $tmp1
            inc $i
            jmp for_start_0
            for_end_0:
            pop_frame
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode.TrimEnd(), Is.EqualTo(expected.TrimEnd()));
    }

    [Test, Ignore( "Not implemented yet" )]
    public void CheckForLoopDefinesVariablesInScope()
    {
        throw new NotImplementedException();
    }
    
    [Test, Ignore( "Not implemented yet" )]
    public void CheckForLoopContainingReturn()
    {
        // Must end scope.
        throw new NotImplementedException();
    }
    
    [Test, Ignore( "Not implemented yet" )]
    public void CheckForLoopContainingBreak()
    {
        throw new NotImplementedException();
    }
    
    [Test, Ignore( "Not implemented yet" )]
    public void CheckForLoopContainingContinue()
    {
        throw new NotImplementedException();
    }

    [Test]
    public void EmitPiApproximationCode()
    {
        var code = ProjectDir.GetDir("Examples").GetFile("PiApproximation.c").ReadAllText();

        string tetraCode = null;
        Assert.That(() => tetraCode = Compiler.CompileToTetraSource(code, "main"), Throws.Nothing);

        Assert.That(tetraCode, Is.Not.Null);
        Assert.That(tetraCode, Is.Not.Empty);

        Program program = null;
        Assert.That(() => program = Assembler.Assemble(tetraCode), Throws.Nothing);
        var vm = new TetraVm(program);
        vm.Debug = true;
        vm.Run();
        
        Assert.That(vm["retval"].AsFloat(), Is.EqualTo(3.141).Within(0.01));
    }
}