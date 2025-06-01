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
using System.Text.RegularExpressions;
using DTC.Core;
using DTC.Core.Extensions;
using TetraCore.Exceptions;

namespace TetraCore;

/// <summary>
/// TetraVm is the core virtual machine that executes a list of Tetra instructions.
/// It maintains a stack of <see cref="ScopeFrame"/> objects for managing variable scopes,
/// and interprets instructions sequentially unless directed otherwise via control flow.
/// </summary>
public class TetraVm
{
    private readonly Program m_program;
    private readonly Stack<ScopeFrame> m_frames = [];
    private readonly Stack<(int functionLabel, int returnIp)> m_callStack = [];
    private readonly List<string> m_uniforms = ["retval"];
    private int m_ip;

    public ScopeFrame CurrentFrame => m_frames.Peek();

    public event EventHandler<string> OutputWritten;

    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool Debug { get; set; }

    public TetraVm(Program program)
    {
        m_program = program ?? throw new ArgumentNullException(nameof(program));

        OutputWritten += (_, message) => Console.WriteLine(message);
        
        Reset();
    }

    public Operand this[string variableName]
    {
        get
        {
            var slot = m_program.SymbolTable.GetSlotFromName(variableName);
            return CurrentFrame.GetVariable(slot.ToString());
        }
    }

    public void Run()
    {
        const int maxInstructionExecutions = 10_000;

        // Execute the instructions.
        var instructionExecutions = 0;
        var instructions = m_program.Instructions;
        var instructionCount = instructions.Length;
        while (m_ip < instructionCount)
        {
            var instr = instructions[m_ip];
            try
            {
                if (Debug)
                {
                    Console.WriteLine();
                    Console.WriteLine("Variables:");
                    var state = CurrentFrame.ToUiString(m_program.SymbolTable);
                    foreach (var s in state!.Split("\n").Where(o => !string.IsNullOrWhiteSpace(o)))
                        Console.WriteLine($"  {s}");
                    Console.WriteLine($"Next: {instr}");
                }
                
                var keepRunning = Execute(instr);
                if (!keepRunning)
                    break;
                instructionExecutions++;
            }
            catch (Exception e)
            {
                var sb = new StringBuilder();
                var message = Regex.Replace(
                    e.Message,
                    @"\$(\d+)",
                    match => m_program.SymbolTable[int.Parse(match.Groups[1].Value)]);
                sb.AppendLine(message);
                sb.AppendLine($"  └─ {instr}");

                var callstack = m_callStack
                    .Select(o => m_program.LabelTable.GetLabelFromInstructionPointer(o.functionLabel))
                    .Append("<Root>");
                sb.AppendLine("Callstack:");
                foreach(var funcName in callstack)
                    sb.AppendLine($"  → {funcName}");

                sb.AppendLine("Variables:");
                var state = CurrentFrame.ToUiString(m_program.SymbolTable);
                foreach(var s in state!.Split('\n').Where(o => !string.IsNullOrWhiteSpace(o)))
                    sb.AppendLine($"  {s}");

                Logger.Instance.Error(sb.ToString());
                throw;
            }
            
            if (instructionExecutions >= maxInstructionExecutions)
                throw new RuntimeException("Too many instruction executions.");
        }
    }

    private void Reset()
    {
        m_frames.Clear();
        m_frames.Push(new ScopeFrame()); // Global scope
        m_callStack.Clear();
        m_ip = 0;
    }

    private bool Execute(Instruction instr)
    {
        switch (instr.OpCode)
        {
            case OpCode.Nop: m_ip++; break;
            case OpCode.Ld: ExecuteLd(instr); break;
            case OpCode.Add: ExecuteAdd(instr); break;
            case OpCode.Sub: ExecuteSub(instr); break;
            case OpCode.Inc: ExecuteInc(instr); break;
            case OpCode.Dec: ExecuteDec(instr); break;
            case OpCode.Neg: ExecuteNeg(instr); break;
            case OpCode.Mul: ExecuteMul(instr); break;
            case OpCode.Div: ExecuteDiv(instr); break;
            case OpCode.Halt: return false;
            case OpCode.Jmp: ExecuteJmp(instr); break;
            case OpCode.JmpEq: ExecuteJmpEq(instr); break;
            case OpCode.JmpNe: ExecuteJmpNe(instr); break;
            case OpCode.JmpLt: ExecuteJmpLt(instr); break;
            case OpCode.JmpGt: ExecuteJmpGt(instr); break;
            case OpCode.JmpLe: ExecuteJmpLe(instr); break;
            case OpCode.JmpGe: ExecuteJmpGe(instr); break;
            case OpCode.Print: ExecutePrint(instr); break;
            case OpCode.PushFrame: ExecutePushFrame(); break;
            case OpCode.PopFrame: ExecutePopFrame(); break;
            case OpCode.Call: ExecuteCall(instr); break;
            case OpCode.Ret: ExecuteRet(instr); break;
            case OpCode.Sin: ExecuteSin(instr); break;
            case OpCode.Sinh: ExecuteSinh(instr); break;
            case OpCode.Asin: ExecuteAsin(instr); break;
            case OpCode.Cos: ExecuteCos(instr); break;
            case OpCode.Cosh: ExecuteCosh(instr); break;
            case OpCode.Acos: ExecuteAcos(instr); break;
            case OpCode.Tan: ExecuteTan(instr); break;
            case OpCode.Tanh: ExecuteTanh(instr); break;
            case OpCode.Atan: ExecuteAtan(instr); break;
            case OpCode.Pow: ExecutePow(instr); break;
            case OpCode.Exp: ExecuteExp(instr); break;
            case OpCode.Log: ExecuteLog(instr); break;
            case OpCode.Sqrt: ExecuteSqrt(instr); break;
            case OpCode.Abs: ExecuteAbs(instr); break;
            case OpCode.Sign: ExecuteSign(instr); break;
            case OpCode.Mod: ExecuteMod(instr); break;
            case OpCode.Min: ExecuteMin(instr); break;
            case OpCode.Max: ExecuteMax(instr); break;
            case OpCode.Ceil: ExecuteCeil(instr); break;
            case OpCode.Fract: ExecuteFract(instr); break;
            default:
                throw new InvalidOperationException($"Instruction defined, but not implemented: '{instr}'");
        }
        return true;
    }

    /// <summary>
    /// Gets the actual value of an operand, resolving variables to their stored values.
    /// </summary>
    /// <remarks>
    /// If the operand is a variable reference, retrieves its value from the current scope frame.
    /// If the operand is a constant (int/float), returns the operand unchanged.
    /// </remarks>
    private Operand GetOperandValue(Operand operand)
    {
        // If the operand is a variable, get its value.
        return operand.Type == OperandType.Variable ? CurrentFrame.GetVariable(operand.Name) : operand;
    }

    /// <summary>
    /// E.g. ld $a, 3.141
    /// E.g. ld $a, $b              (a = b)
    /// E.g. ld $a, 1.1, 2.2, 3.3   (a = [1.1, 2.2, 3.3])
    /// </summary>
    private void ExecuteLd(Instruction instr)
    {
        var a = instr.Operands[0];

        Operand b;
        if (instr.Operands.Length == 2)
        {
            // Just one 'b' operand.
            b = instr.Operands[1];
        }
        else
        {
            // Get 2nd+ operands.
            var operands = new Operand[instr.Operands.Length - 1];
            for (var i = 1; i < instr.Operands.Length; i++)
                operands[i - 1] = GetOperandValue(instr.Operands[i]);
            b = Operand.FromOperands(operands);
        }

        CurrentFrame.DefineVariable(a.Name, b);
        m_ip++;
    }

    /// <summary>
    /// E.g. add $a, 3.141
    /// E.g. add $a, $b     (a += b)
    /// </summary>
    private void ExecuteAdd(Instruction instr) =>
        DoMathOp(instr, (a , b) => a + b);

    /// <summary>
    /// E.g. sub $a, 3.141
    /// E.g. sub $a, $b     (a -= b)
    /// </summary>
    private void ExecuteSub(Instruction instr) =>
        DoMathOp(instr, (a, b) => a - b);

    /// <summary>
    /// E.g. inc $a
    /// </summary>
    private void ExecuteInc(Instruction instr) =>
        DoMathOp(instr, (a, _) => ++a);

    /// <summary>
    /// E.g. dec $a
    /// </summary>
    private void ExecuteDec(Instruction instr) =>
        DoMathOp(instr, (a, _) => --a);

    /// <summary>
    /// E.g. neg $a  (a = -a)
    /// </summary>
    private void ExecuteNeg(Instruction instr) =>
        DoMathOp(instr, (a, _) => -a);

    /// <summary>
    /// Unconditional jump.
    /// E.g. jmp NN
    /// </summary>
    private void ExecuteJmp(Instruction instr)
    {
        var label = instr.Operands[0];
        if (label.Type != OperandType.Int)
            throw new RuntimeException("Integer operand expected.");
        m_ip = label.Int;
    }

    /// <summary>
    /// E.g. jmp_eq $a, $b, label
    /// E.g. jmp_eq $a, 2, label
    /// E.g. jmp_eq $a, 2.3, label
    /// </summary>
    private void ExecuteJmpEq(Instruction instr)
    {
        var a = instr.Operands[0];
        var aValue = CurrentFrame.GetVariable(a.Name);
        var b = GetOperandValue(instr.Operands[1]);
        var label = instr.Operands[2];

        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat().IsApproximately(b.AsFloat());
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.Int == b.Int;
        else
            throw new RuntimeException($"Cannot compare {a.Type} and {b.Type}.");

        if (jump)
            m_ip = label.Int;
        else
            m_ip++;
    }

    /// <summary>
    /// E.g. jmp_ne $a, $b, label
    /// E.g. jmp_ne $a, 2, label
    /// E.g. jmp_ne $a, 2.3, label
    /// </summary>
    private void ExecuteJmpNe(Instruction instr)
    {
        var a = instr.Operands[0];
        var aValue = CurrentFrame.GetVariable(a.Name);
        var b = GetOperandValue(instr.Operands[1]);
        var label = instr.Operands[2];

        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = !aValue.AsFloat().IsApproximately(b.AsFloat());
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.Int != b.Int;
        else
            throw new RuntimeException($"Cannot compare {a.Type} and {b.Type}.");

        if (jump)
            m_ip = label.Int;
        else
            m_ip++;
    }

    /// <summary>
    /// Jump if a is less than b.
    /// E.g. jmp_lt $a, $b, label
    /// E.g. jmp_lt $a, 2, label
    /// E.g. jmp_lt $a, 2.3, label
    /// </summary>
    private void ExecuteJmpLt(Instruction instr)
    {
        var a = instr.Operands[0];
        var aValue = CurrentFrame.GetVariable(a.Name);
        var b = GetOperandValue(instr.Operands[1]);
        var label = instr.Operands[2];

        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat() < b.AsFloat();
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.Int < b.Int;
        else
            throw new RuntimeException($"Cannot compare {a.Type} and {b.Type}.");

        if (jump)
            m_ip = label.Int;
        else
            m_ip++;
    }

    /// <summary>
    /// Jump if a is less than or equal to b.
    /// E.g. jmp_le $a, $b, label
    /// E.g. jmp_le $a, 2, label
    /// E.g. jmp_le $a, 2.3, label
    /// </summary>
    private void ExecuteJmpLe(Instruction instr)
    {
        var a = instr.Operands[0];
        var aValue = CurrentFrame.GetVariable(a.Name);
        var b = GetOperandValue(instr.Operands[1]);
        var label = instr.Operands[2];

        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat() <= b.AsFloat();
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.Int <= b.Int;
        else
            throw new RuntimeException($"Cannot compare {a.Type} and {b.Type}.");

        if (jump)
            m_ip = label.Int;
        else
            m_ip++;
    }

    /// <summary>
    /// Jump if a is greater than b.
    /// E.g. jmp_gt $a, $b, label
    /// E.g. jmp_gt $a, 2, label
    /// E.g. jmp_gt $a, 2.3, label
    /// </summary>
    private void ExecuteJmpGt(Instruction instr)
    {
        var a = instr.Operands[0];
        var aValue = CurrentFrame.GetVariable(a.Name);
        var b = GetOperandValue(instr.Operands[1]);
        var label = instr.Operands[2];

        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat() > b.AsFloat();
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.Int > b.Int;
        else
            throw new RuntimeException($"Cannot compare {a.Type} and {b.Type}.");

        if (jump)
            m_ip = label.Int;
        else
            m_ip++;
    }

    /// <summary>
    /// Jump if a is greater than or equal to b.
    /// E.g. jmp_ge $a, $b, label
    /// E.g. jmp_ge $a, 2, label
    /// E.g. jmp_ge $a, 2.3, label
    /// </summary>
    private void ExecuteJmpGe(Instruction instr)
    {
        var a = instr.Operands[0];
        var aValue = CurrentFrame.GetVariable(a.Name);
        var b = GetOperandValue(instr.Operands[1]);
        var label = instr.Operands[2];

        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat() >= b.AsFloat();
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.Int >= b.Int;
        else
            throw new RuntimeException($"Cannot compare {a.Type} and {b.Type}.");

        if (jump)
            m_ip = label.Int;
        else
            m_ip++;
    }

    /// <summary>
    /// E.g. mul $a, 3.141
    /// E.g. mul $a, $b     (a *= b)
    /// </summary>
    private void ExecuteMul(Instruction instr) =>
        DoMathOp(instr, (a, b) => a * b);

    /// <summary>
    /// E.g. div $a, 3.141
    /// E.g. div $a, $b     (a /= b)
    /// </summary>
    private void ExecuteDiv(Instruction instr) =>
        DoMathOp(instr, (a, b) => b == 0.0f ? throw new RuntimeException("Division by zero.") : a / b);

    /// <summary>
    /// E.g. print 3.141
    /// E.g. print $a
    /// </summary>
    private void ExecutePrint(Instruction instr)
    {
        var a = instr.Operands[0];
        var toPrint = Operand.FromOperands(instr.Operands.Select(GetOperandValue).ToArray());
        OutputWritten?.Invoke(this, a.Type == OperandType.Variable ? $"{a.ToUiString(m_program.SymbolTable).TrimStart('$')} = {toPrint}" : toPrint.ToString());
        m_ip++;
    }

    /// <summary>
    /// Pops the top-scoped variable frame from the stack.
    /// E.g. pop_frame
    /// </summary>
    private void ExecutePopFrame()
    {
        if (m_frames.Count == 1)
            throw new RuntimeException("Cannot pop the last remaining frame.");
        m_frames.Pop();
        m_ip++;
    }

    /// <summary>
    /// Pushes a new scoped variable frame onto the stack.
    /// E.g. push_frame
    /// </summary>
    private void ExecutePushFrame()
    {
        m_frames.Push(new ScopeFrame(CurrentFrame));
        m_ip++;
    }

    /// <summary>
    /// Call a procedure (implicitly pushing a new scoped variable frame).
    /// E.g. call label
    /// </summary>
    private void ExecuteCall(Instruction instr)
    {
        var label = instr.Operands[0];
        if (label.Type != OperandType.Int)
            throw new RuntimeException("Integer operand expected.");
        if (instr.Operands.Length > 1)
            throw new RuntimeException("Too many operands.");
        m_frames.Push(new ScopeFrame(CurrentFrame));
        m_callStack.Push((label.Int, m_ip + 1));
        m_ip = label.Int;
    }

    /// <summary>
    /// Return from a procedure (implicitly popping the top-scoped variable frame).
    /// E.g. ret
    /// E.g. ret $a
    /// E.g. ret 123
    /// </summary>
    private void ExecuteRet(Instruction instr)
    {
        if (m_callStack.Count == 0)
            throw new RuntimeException("No procedure to return to.");

        var a = instr.Operands.Length > 0 ? instr.Operands[0] : null;
        if (a != null)
            a = GetOperandValue(a);

        // Pop the scoped variable frame.
        m_frames.Pop();

        // Make the return value available to the caller (may be null).
        CurrentFrame.Retval = a;

        // Restore the IP.
        m_ip = m_callStack.Pop().returnIp;
    }

    /// <summary>
    /// E.g. sin $a, $theta
    /// E.g. sin $a, 1.2
    /// </summary>
    private void ExecuteSin(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Sin(b));

    /// <summary>
    /// E.g. sinh $a, $theta
    /// E.g. sinh $a, 1.2
    /// </summary>
    private void ExecuteSinh(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Sinh(b));

    /// <summary>
    /// E.g. asin $a, $theta
    /// </summary>
    private void ExecuteAsin(Instruction instr)
    {
        DoMathOp(instr, (_, b) =>
        {
            if (b < -1f || b > 1f)
                throw new RuntimeException($"Input value '{b:0.0###}' must be in the range [-1, 1].");
            return MathF.Asin(b);
        });
    }

    /// <summary>
    /// E.g. cos $a, $theta
    /// E.g. cos $a, 1.2
    /// </summary>
    private void ExecuteCos(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Cos(b));

    /// <summary>
    /// E.g. cosh $a, $theta
    /// E.g. cosh $a, 1.2
    /// </summary>
    private void ExecuteCosh(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Cosh(b));

    /// <summary>
    /// E.g. acos $a, $theta
    /// </summary>
    private void ExecuteAcos(Instruction instr)
    {
        DoMathOp(instr, (_, b) =>
        {
            if (b < -1f || b > 1f)
                throw new RuntimeException($"Input value '{b:0.0###}' must be in the range [-1, 1].");
            return MathF.Acos(b);
        });
    }

    /// <summary>
    /// E.g. tan $a, $theta
    /// E.g. tan $a, 1.2
    /// </summary>
    private void ExecuteTan(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Tan(b));

    /// <summary>
    /// E.g. tanh $a, $theta
    /// E.g. tanh $a, 1.2
    /// </summary>
    private void ExecuteTanh(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Tanh(b));

    /// <summary>
    /// E.g. atan $a, $theta
    /// E.g. atan $a, 1.2
    /// </summary>
    private void ExecuteAtan(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Atan(b));

    /// <summary>
    /// E.g. pow $a, $b
    /// </summary>
    private void ExecutePow(Instruction instr)
    {
        DoMathOp(instr, (a, b) =>
        {
            if (a == 0f && b < 0f)
                throw new RuntimeException($"Zero cannot be raised to a negative power (base: {a:0.0###}, exponent: {b:0.0###}).");
            return MathF.Pow(a, b);
        });
    }
    
    /// <summary>
    /// E.g. sqrt $a, $b
    /// </summary>
    private void ExecuteSqrt(Instruction instr)
    {
        DoMathOp(instr, (_, b) =>
        {
            if (b < 0f)
                throw new RuntimeException($"Input value '{b:0.0###}' must be greater than or equal to zero.");
            return MathF.Sqrt(b);
        });
    }

    /// <summary>
    /// E.g. exp $a, $b
    /// E.g. exp $a, 1.2
    /// </summary>
    private void ExecuteExp(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Exp(b));

    /// <summary>
    /// E.g. log $a, $b
    /// </summary>
    private void ExecuteLog(Instruction instr)
    {
        DoMathOp(instr, (_, b) =>
        {
            if (b <= 0f)
                throw new RuntimeException($"Input value '{b:0.0###}' must be greater than 0.");
            return MathF.Log(b);
        });
    }

    /// <summary>
    /// E.g. abs $a, $b
    /// E.g. abs $a, 1.2
    /// </summary>
    private void ExecuteAbs(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Abs(b));

    /// <summary>
    /// E.g. sign $a, $b 
    /// E.g. sign $a, 1.2
    /// </summary>
    private void ExecuteSign(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Sign(b));

    /// <summary>
    /// E.g. mod $a, $b
    /// E.g. mod $a, 1.2
    /// </summary>
    private void ExecuteMod(Instruction instr) =>
        DoMathOp(instr, (a, b) =>
        {
            if (b == 0f)
                throw new RuntimeException("Modulus by zero is undefined.");
            return a % b;
        });

    /// <summary>
    /// E.g. min $a, $b
    /// E.g. min $a, 1.2
    /// </summary>
    private void ExecuteMin(Instruction instr) =>
        DoMathOp(instr, (a, b) => a < b ? a : b);

    /// <summary>
    /// E.g. max $a, $b
    /// E.g. max $a, 1.2
    /// </summary>
    private void ExecuteMax(Instruction instr) =>
        DoMathOp(instr, (a, b) => a > b ? a : b);

    /// <summary>
    /// E.g. ceil $a, $b
    /// E.g. ceil $a, 1.2
    /// </summary>
    private void ExecuteCeil(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Ceiling(b));
    
    /// <summary>
    /// E.g. fract $a, $b
    /// E.g. fract $a, 1.2
    /// </summary>
    private void ExecuteFract(Instruction instr) =>
        DoMathOp(instr, (_, b) => b - MathF.Floor(b));

    /// <summary>
    /// Perform a float->float operation on the elements of a numeric operand.
    /// </summary>
    private void DoMathOp(Instruction instr, Func<float, float, float> op)
    {
        // Get target variable.
        var a = instr.Operands[0];
        var aName = a.Name;
        if (CurrentFrame.IsDefined(aName))
        {
            a = CurrentFrame.GetVariable(aName);
            if (a.Type is OperandType.Label or OperandType.Variable)
                throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {a.Type}.");
        }

        // Single operand? We can do that...
        Operand result;
        if (instr.Operands.Length == 1)
        {
            result = new Operand(new float[a.Length]);
            for (var i = 0; i < a.Length; i++)
                result.Floats[i] = op(a.Floats[i], float.NaN);

            if (a.Type == OperandType.Int)
                result = result.WithType(OperandType.Int);
        }
        else
        {
            // Get 2nd+ operands.
            var operands = new Operand[instr.Operands.Length - 1];
            for (var i = 1; i < instr.Operands.Length; i++)
                operands[i - 1] = GetOperandValue(instr.Operands[i]);
            var b = Operand.FromOperands(operands);
            if (b.Type is OperandType.Label or OperandType.Variable)
                throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {b.Type}.");

            if (CurrentFrame.IsDefined(aName))
            {
                // Ensure that the operands have the same dimension.
                if (a.Length == 1 && b.Length > 1)
                    a = a.GrowFromOneToN(b.Length);
                else if (b.Length == 1 && a.Length > 1)
                    b = b.GrowFromOneToN(a.Length);
                
                if (a.Length != b.Length)
                    throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on operands of different length ({a.Length} vs {b.Length}).");

                result = new Operand(new float[a.Length]);
                for (var i = 0; i < a.Length; i++)
                    result.Floats[i] = op(a.Floats[i], b.Floats[i]);
            }
            else
            {
                result = new Operand(new float[b.Length]);
                for (var i = 0; i < b.Length; i++)
                    result.Floats[i] = op(float.NaN, b.Floats[i]);
            }

            if (a.Type == OperandType.Int && b.Type == OperandType.Int)
                result = result.WithType(OperandType.Int);
        }

        // Store the result.
        CurrentFrame.SetVariable(aName, result, true);
        m_ip++;
    }

    public void AddUniform(string name, Operand value)
    {
        if (m_uniforms.Contains(name))
            throw new RuntimeException($"Uniform '{name}' already defined.");
        CurrentFrame.DefineVariable($"{m_uniforms.Count}", value);
        m_uniforms.Add(name);
    }
}