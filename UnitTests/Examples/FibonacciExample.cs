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

namespace UnitTests.Examples;

[TestFixture]
public class FibonacciExample
{
    [Test]
    public void CalculateFibonacciSequence()
    {
        const string code =
            """
                #
                # Print the first 10 Fibonacci numbers.
                #
                ld $i, 0
                ld $count, 10

            loop:
                ld $c, $i
                ge $c, $count
                jmp_nz $c, done

                ld $arg0, $i
                call fib
                print $retval
                inc $i
                jmp loop

            done:
                halt

            #
            # fib(n):
            # if n <= 1 return n
            # else return fib(n - 1) + fib(n - 2)
            #
            fib:
                ld $n, $arg0
                ld $c, $n
                le $c, 1
                jmp_nz $c, base_case

                # fib(n - 1)
                ld $arg0, $n
                dec $arg0
                call fib
                ld $a, $retval

                # fib(n - 2)
                ld $arg0, $n
                dec $arg0
                dec $arg0
                call fib
                ld $b, $retval

                add $a, $b
                ret $a

            base_case:
                ret $n
            """;

        var vm = new TetraVm(Assembler.Assemble(code));
        var output = string.Empty;
        vm.OutputWritten += (_, s) => output = s + "\n";
        vm.Run();

        // This will print the sequence 0, 1, 1, 2, 3, 5, 8, 13, 21, 34
        Assert.That(vm["retval"].Int, Is.EqualTo(34));
        Assert.That(output, Is.Not.Empty);
    }
}