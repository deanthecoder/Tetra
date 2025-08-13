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

namespace TetraCore;

public enum OpCode
{
    Nop,        // nop                       (no operation)
    Decl,       // decl $a                   (declare variable a)
    Ld,         // ld $a, $b                 (a = b)
    Ldc,        // ldc $a, $b                (a = b)
    Halt,       // halt                      (stop execution)
    Add,        // add $a, $b                (a += b)
    Sub,        // sub $a, $b                (a -= b)
    Inc,        // inc $a                    (a += 1)
    Dec,        // dec $a                    (a -= 1)
    Neg,        // neg $a                    (a = -a)
    Mul,        // mul $a, $b                (a *= b)
    Div,        // div $a, $b                (a /= b)
    Dim,        // dim $a, size              (resize 'a' to [size] elements)
    Shiftl,     // shiftl $a, $b             (a <<= b)
    Shiftr,     // shiftr $a, $b             (a >>= b)
    BitAnd,     // and $a, $b                (a &= b)
    BitOr,      // or $a, $b                 (a |= b)
    Lt,         // lt $a, $b                 (a = a < b)
    Le,         // le $a, $b                 (a = a <= b)
    Gt,         // gt $a, $b                 (a = a > b)
    Ge,         // ge $a, $b                 (a = a >= b)
    Eq,         // eq $a, $b                 (a = a == b)
    Ne,         // ne $a, $b                 (a = a != b)
    And,        // and $a, $b (logical)      (a = a && b)
    Or,         // or $a, $b (logical)       (a = a || b)
    Not,        // not $a                    (a = !a)
    Test,       // test $a                   (a = a != 0)
    Jmp,        // jmp label                 (jump to label)
    Jmpz,       // jmpz $a, label            (if a == 0, jump to label)
    Jmpnz,      // jmpnz $a, label           (if a != 0, jump to label)
    PushFrame,  // push_frame                (push new call frame)
    PopFrame,   // pop_frame                 (pop call frame)
    Call,       // call label                (call function at label)
    Ret,        // ret                       (return from function)
    
    // Intrinsics.
    Intrinsic,  // intrinsic id
    Print,      // print $a                  (print a)
    Debug,      // debug $a                  (debug a)
    Sin,        // sin $a, $b                (a = sin(b))
    Sinh,       // sinh $a, $b               (a = sinh(b))
    Asin,       // asin $a, $b               (a = asin(b))
    Cos,        // cos $a, $b                (a = cos(b))
    Cosh,       // cosh $a, $b               (a = cosh(b))
    Acos,       // acos $a, $b               (a = acos(b))
    Tan,        // tan $a, $b                (a = tan(b))
    Tanh,       // tanh $a, $b               (a = tanh(b))
    Atan,       // atan $a, $b               (a = atan(b))
    Pow,        // pow $a, $b                (a = pow(a, b))
    Sqrt,       // sqrt $a, $b               (a = sqrt(b))
    Exp,        // exp $a, $b                (a = exp(b))
    Log,        // log $a, $b                (a = log(b))
    Abs,        // abs $a, $b                (a = abs(b))
    Sign,       // sign $a, $b               (a = sign(b))
    Mod,        // mod $a, $b                (a = a % b)
    Min,        // min $a, $b                (a = min(a, b))
    Max,        // max $a, $b                (a = max(a, b))
    Clamp,      // clamp $a, $b, $c          (a = clamp(b, c))
    Mix,        // mix $a, $b, $c            (a = mix(b, c))
    Smoothstep, // smoothstep $a, $e1, $e2   (a = smoothstep(a, e1, e2))
    Step,       // step $a, $edge            (a = step(edge, a))
    Reflect,    // reflect $a, $n            (a = reflect(a, n))
    Refract,    // refract $a, $n, $eta      (a = refract(a, n, eta))
    Normalize,  // normalize $a, $b          (a = normalize(b))
    Length,     // length $a, $b             (a = length(b))
    Dot,        // dot $a, $b                (a = dot(a, b))
    Ceil,       // ceil $a, $b               (a = ceil(b))
    Floor,      // floor $a, $b              (a = floor(b))
    Fract,      // fract $a, $b              (a = fract(b))
    Cross       // cross $a, $b              (a = cross(a, b))
}