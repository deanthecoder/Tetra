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
public class CallTests
{
    [Test]
    public void CheckCallVoidFunctionWithNoArgs()
    {
        const string code =
            """
                call output
                halt
            output:
                print 123
                ret
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        var output = new List<string>();
        vm.OutputWritten += (_, message) => output.Add(message);
        vm.Run();
        
        Assert.That(output, Is.EqualTo((string[]) ["123"]));
    }

    [Test]
    public void CallVoidFunctionWithArgs()
    {
        const string code =
            """
                ld $arg0, 42
                call print_arg
                halt
            print_arg:
                print $arg0
                ret
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        var output = new List<string>();
        vm.OutputWritten += (_, message) => output.Add(message);
        vm.Run();
        
        Assert.That(output, Is.EqualTo((string[]) ["arg0 = 42"]));
    }

    [Test]
    public void CallFunctionWithNoArgsAndReturnValue()
    {
        const string code =
            """
                ld $a, 1
                call get_value
                print $retval
                print $a
                halt
            get_value:
                decl $a
                ld $a, 123
                ret $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        var output = new List<string>();
        vm.OutputWritten += (_, message) => output.Add(message);
        vm.Run();
        
        Assert.That(output, Is.EqualTo((string[]) ["retval = 123", "a = 1"]));
    }

    [Test]
    public void CallFunctionWithArgsAndReturnValue()
    {
        const string code =
            """
                ld $arg0, 5
                ld $arg1, 7
                call add
                print $retval
                halt
            add:
                ld $a, $arg0
                add $a, $arg1
                ret $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        var output = string.Empty;
        vm.OutputWritten += (_, message) => output += message;
        vm.Run();

        Assert.That(output, Is.EqualTo("retval = 12"));
    }

    [Test]
    public void CheckReturningVoidOutsideProcedureThrows()
    {
        const string code = "ret";
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CheckReturningValueOutsideProcedureThrows()
    {
        const string code = "ret 23";
        var vm = new TetraVm(Assembler.Assemble(code));
        
        Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
    }

    [Test]
    public void CallNestedFunctionWithReturnValue()
    {
        const string code =
            """
                ld $arg0, 4
                call double_and_add
                print $retval
                halt

            double_and_add:
                call double_value
                ld $result, $retval
                add $result, 3
                ret $result

            double_value:
                ld $x, $arg0
                add $x, $x
                ret $x
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        var output = new List<string>();
        vm.OutputWritten += (_, message) => output.Add(message);
        vm.Run();

        Assert.That(output, Is.EqualTo((string[]) ["retval = 11"]));
    }
}