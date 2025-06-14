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
public class PiApproximation
{
    [Test]
    public void ApproximatePi()
    {
        const string code =
            """
                #
                # Leibniz formula for π
                #
                ld $sum, 0               # Running total
                ld $sign, 1              # Used to alternate +/-
                ld $i, 0                 # Loop counter
                ld $limit, 800           # Number of terms to compute

            loop:
                ld $c, $i
                ge $c, $limit
                jmp_nz $c, done          # Exit if i >= limit

                ld $denominator, $i
                mul $denominator, 2      # denominator = 2*i
                add $denominator, 1      # denominator = 2*i + 1
                ld $term, 1.0            # numerator is always 1.0
                div $term, $denominator  # term = 1 / (2*i + 1)
                mul $term, $sign         # apply sign
                add $sum, $term          # accumulate into sum
                neg $sign                # flip sign for next term
                inc $i                   # i++
                jmp loop

            done:
                mul $sum, 4              # Multiply sum by 4 to approximate PI
                print $sum               # Output result
                halt
            """;

        var vm = new TetraVm(Assembler.Assemble(code));
        var output = string.Empty;
        vm.OutputWritten += (_, s) => output = s + "\n";
        vm.Run();

        var result = vm["sum"].Float;

        Assert.That(result, Is.EqualTo(3.14f).Within(0.01f));
        Assert.That(output, Is.Not.Empty);
    }
}