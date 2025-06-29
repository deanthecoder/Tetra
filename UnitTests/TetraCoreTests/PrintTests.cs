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
public class PrintTests
{
    [Test]
    public void CheckPrintingInt()
    {
        const string code = "print 123";
        var vm = new TetraVm(Assembler.Assemble(code));
        var s = string.Empty;
        vm.OutputWritten += (_, output) => s += output; 
        
        Assert.That(() => vm.Run(), Throws.Nothing);
        Assert.That(s, Is.EqualTo("123"));
    }
    
    [Test]
    public void CheckPrintingFloat()
    {
        const string code = "print 123.456";
        var vm = new TetraVm(Assembler.Assemble(code));
        var s = string.Empty;
        vm.OutputWritten += (_, output) => s += output; 
        
        Assert.That(() => vm.Run(), Throws.Nothing);
        Assert.That(s, Is.EqualTo("123.456"));
    }
    
    [Test]
    public void CheckPrintingVariable()
    {
        const string code =
            """
            ld $a, 69.23
            print $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        var s = string.Empty;
        vm.OutputWritten += (_, output) => s += output; 
        
        Assert.That(() => vm.Run(), Throws.Nothing);
        Assert.That(s, Is.EqualTo("a = 69.23"));
    }

    [Test]
    public void CheckPrintingVector()
    {
        const string code = "print 69.0, 0.23";
        var vm = new TetraVm(Assembler.Assemble(code));
        var s = string.Empty;
        vm.OutputWritten += (_, output) => s += output; 

        Assert.That(() => vm.Run(), Throws.Nothing);
        Assert.That(s, Is.EqualTo("[69.0,0.23]"));
    }
    
    [Test]
    public void CheckPrintingVectorVariable()
    {
        const string code =
            """
            ld $a, 69.0, -0.23
            print $a
            """;
        var vm = new TetraVm(Assembler.Assemble(code));
        var s = string.Empty;
        vm.OutputWritten += (_, output) => s += output; 

        Assert.That(() => vm.Run(), Throws.Nothing);
        Assert.That(s, Is.EqualTo("a = [69.0,-0.23]"));
    }

    [Test]
    public void CheckPrintingLabelThrows()
    {
        const string code =
            """
            main:
                print main
            """;
        Assert.That(() => Assembler.Assemble(code), Throws.TypeOf<SyntaxErrorException>());
    }
}