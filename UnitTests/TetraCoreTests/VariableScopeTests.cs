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

namespace UnitTests.TetraCoreTests;

[TestFixture]
public class VariableScopeTests
{
    [Test]
    public void CheckAlteringVariableInChildScopeUpdatesParentScope()
    {
        const string code =
            """
            ld $a, 1
            push_frame
            ld $a, 2
            pop_frame
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(2));
    }

    [Test]
    public void CheckDeclaringVariableInChildScopeDoesNotUpdateParentScope()
    {
        const string code =
            """
            ld $a, 1
            push_frame
            decl $a
            ld $a, 2
            pop_frame
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(1));
    }

    [Test]
    public void DeclOnExistingVariableIsNoOp()
    {
        const string code =
            """
            decl $a
            ld $a, 1
            decl $a       # no-op
            ld $a, 2
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(vm["a"].Int, Is.EqualTo(2));
    }

    [Test]
    public void DeclaredVariableInChildScopeIsDiscardedAfterPop()
    {
        const string code =
            """
            push_frame
            decl $b
            ld $b, 42
            pop_frame
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();

        Assert.That(() => vm["b"], Throws.Exception);
    }
}