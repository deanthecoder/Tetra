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
public class FramePushPopTests
{
    [Test]
    public void CheckPoppingBaseFrameThrows()
    {
        const string code = "pop_frame";
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckPushingFrameCreatesNewVariableScope()
    {
        const string code =
            """
            ld $a, 23
            ld $result, 0
            push_frame
            decl $a
            ld $a, 42
            add $result, $a
            pop_frame
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        vm.Run();
        
        Assert.That(vm["result"].Int, Is.EqualTo(42));
        Assert.That(vm["a"].Int, Is.EqualTo(23));
    }
}