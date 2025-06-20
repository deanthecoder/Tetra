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

using DTC.Core.UnitTesting;
using DTC.GLSLParser;
using TetraCore;
using TetraCore.Exceptions;

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
    public void CheckSingleVariableDeclaration()
    {
        const string code = "int a;";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("decl $a"));
    }

    [Test]
    public void CheckMultipleVariableDeclaration()
    {
        const string code = "int a, b;";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("decl $a"));
        Assert.That(tetraCode, Does.Contain("decl $a"));
    }
    
    [Test]
    public void CheckVariableDeclarationWithInitialValue()
    {
        const string code = "int a = 1;";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("decl $a"));
        Assert.That(tetraCode, Does.Contain("ld $a, 1"));
    }
    
    [Test]
    public void CheckMultipleVariableDeclarationsWithInitialValue()
    {
        const string code = "int a = 1.0, b = 2.0;";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("decl $a"));
        Assert.That(tetraCode, Does.Contain("ld $a, 1.0"));
        Assert.That(tetraCode, Does.Contain("decl $b"));
        Assert.That(tetraCode, Does.Contain("ld $b, 2.0"));
    }

    [Test]
    public void CheckMixedVariableDeclarations()
    {
        const string code = "int a = 1, b;";
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode, Does.Contain("decl $a"));
        Assert.That(tetraCode, Does.Contain("ld $a, 1"));
        Assert.That(tetraCode, Does.Contain("decl $b"));
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
            for0_start:
            ld $tmp0, $i
            lt $tmp0, 5
            jmp_z $tmp0, for0_end
            ld $tmp1, $sum
            add $tmp1, $i
            ld $sum, $tmp1
            for0_incr:
            inc $i
            jmp for0_start
            for0_end:
            pop_frame
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);
        
        Assert.That(tetraCode.TrimEnd(), Is.EqualTo(expected.TrimEnd()));
    }

    [Test]
    public void CheckForLoopDefinesVariablesInScope()
    {
        const string code =
            """
            int i = 23;
            for (int i = 0; i < 5; ++i) { }
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["i"].Int, Is.EqualTo(23));
    }
    
    [Test]
    public void CheckForLoopContainingReturn()
    {
        const string code =
            """
            void main() {
                for (int i = 0; i < 5; ++i) {
                    for (int j = 0; j < 5; ++j)
                        return;
                }
            }
            """;
        var tetraCode = Compiler.CompileToTetraSource(code, "main");
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm.CurrentFrame.IsRoot, Is.True);
    }

    [Test]
    public void CheckForLoopContainingBreak()
    {
        const string code =
            """
            int a;
            void main() {
                for (int i = 0; i < 1; ++i) {
                    a = 23;
                    break;
                    a = 32;
                }
            }
            """;
        var tetraCode = Compiler.CompileToTetraSource(code, "main");
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(23));
    }

    [Test]
    public void CheckForLoopContainingContinue()
    {
        const string code =
            """
            int a = 0;
            void main() {
                for (int i = 0; i < 5; ++i) {
                    a += i;
                    continue;
                    a = -1000;
                }
            }
            """;
        var tetraCode = Compiler.CompileToTetraSource(code, "main");
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(10));
    }

    [Test]
    public void CheckSecondTermInAndExpression()
    {
        const string code =
            """
            int a, b, d1, d2;
            void main() {
                d1 = 13 && setA();
                d2 = 0 && setB();
            }
            int setA() { a = 1; return 12; }
            int setB() { b = 1; return 15; }
            """;
        var tetraCode = Compiler.CompileToTetraSource(code, "main");
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
        Assert.That(vm["d1"].Int, Is.EqualTo(1));
        Assert.That(vm["b"].Int, Is.Zero);
        Assert.That(vm["d2"].Int, Is.Zero);
    }
    
    [Test]
    public void CheckSecondTermInOrExpression()
    {
        const string code =
            """
            int a, b, d1, d2;
            void main() {
                d1 = 23 || setA();
                d2 = 0 || setB();
            }
            int setA() { a = 1; return 11; }
            int setB() { b = 1; return 12; }
            """;
        var tetraCode = Compiler.CompileToTetraSource(code, "main");
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();

        Assert.That(vm["a"].Int, Is.Zero);
        Assert.That(vm["d1"].Int, Is.EqualTo(1));
        Assert.That(vm["b"].Int, Is.EqualTo(1));
        Assert.That(vm["d2"].Int, Is.EqualTo(1));
    }

    [Test]
    public void CheckVectorCreation()
    {
        const string code = "vec2 v = vec2(1.0, 2.0);";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["v"].Floats, Is.EqualTo(new[] { 1.0f, 2.0f }));
    }
    
    [Test]
    public void CheckVectorArrayAccess()
    {
        const string code =
            """
            vec2 v = vec2(1.0, 2.0);
            float y = v[1];
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["y"].Float, Is.EqualTo(2.0));
    }
    
    [Test, Sequential]
    public void CheckVectorArrayAccessWithOutOfBoundsIndex([Values(-1, 23)] int index)
    {
        var code =
            $"""
            vec2 v = vec2(1.0, 2.0);
            float y = v[{index}];
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        
        Assert.That(() => vm.Run(), Throws.InstanceOf<RuntimeException>());
    }
    
    [Test]
    public void CheckVectorElementAccess()
    {
        const string code =
            """
            vec2 v = vec2(1.0, 2.0);
            float y = v.y;
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["y"].Float, Is.EqualTo(2.0));
    }
    
    [Test]
    public void CheckVectorConstructionFromSwizzle()
    {
        const string code =
            """
            vec2 v1 = vec2(1.0, 2.0);
            vec3 v3 = v1.xyx;
            """;
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["v3"].Floats, Is.EqualTo(new[] { 1.0f, 2.0f, 1.0f }));
    }
    
    [Test]
    public void CheckShiftRightOperator()
    {
        const string code = "int a = 10 >> 1;";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["a"].Int, Is.EqualTo(5));
    }
    
    [Test]
    public void CheckShiftLeftOperator()
    {
        const string code = "int a = 10 << 1;";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["a"].Int, Is.EqualTo(20));
    }
    
    [Test]
    public void CheckBitwiseAnd()
    {
        const string code = "int a = 11 & 7;";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["a"].Int, Is.EqualTo(3));
    }
    
    [Test]
    public void CheckBitwiseOr()
    {
        const string code = "int a = 9 | 2;";
        var tetraCode = Compiler.CompileToTetraSource(code);
        var vm = new TetraVm(Assembler.Assemble(tetraCode));
        vm.Run();
        
        Assert.That(vm["a"].Int, Is.EqualTo(11));
    }
}