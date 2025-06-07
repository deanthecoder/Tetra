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
using System.Text;
using DTC.Core;
using TetraCore;
using TetraCore.Exceptions;

namespace UnitTests.TetraCoreTests;

[TestFixture]
public class RuntimeTests
{
    [Test]
    public void CheckRuntimeExceptionReportsStackTrace()
    {
        const string code =
            """
                call func1
                halt
                
            func1:
                nop
                nop
                call func2
                nop
                ret
                
            func2:
                print $unknown
                ret
            """;

        var program = Assembler.Assemble(code);

        var output = new StringBuilder();
        Logger.Instance.Logged += OnLogged;
        try
        {
            var vm = new TetraVm(program);

            Assert.That(() => vm.Run(), Throws.TypeOf<RuntimeException>());
            Assert.That(output.ToString(), Does.Contain("func1"));
            Assert.That(output.ToString(), Does.Contain("func2"));
        }
        finally
        {
            Logger.Instance.Logged -= OnLogged;
        }
        return;

        void OnLogged(object sender, (Logger.Severity, string Message) info) =>
            output.AppendLine(info.Message);
    }
}