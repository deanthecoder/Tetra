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
// ReSharper disable InvalidXmlDocComment

namespace TetraCore;

/// <summary>
/// TetraVm is the core virtual machine that executes a list of Tetra instructions.
/// It maintains a stack of <see cref="ScopeFrame"/> objects for managing variable scopes,
/// and interprets instructions sequentially unless directed otherwise via control flow.
/// </summary>
public class TetraVm
{
    private readonly Program m_program;
    private readonly Stack<ScopeFrame> m_frames = new Stack<ScopeFrame>(4);
    private readonly Stack<(int functionLabel, int returnIp, int scopeFrameDepth)> m_callStack = new Stack<(int, int, int)>(4);
    private List<string> m_uniforms;
    private int m_ip;
    private bool m_isDebugging;

    public ScopeFrame CurrentFrame => m_frames.Peek();

    /// <summary>
    /// Raised when a 'print' instruction is executed.
    /// </summary>
    public event EventHandler<string> OutputWritten;
    
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
            return CurrentFrame.GetVariable(slot.ToString(), allowUndefined: true);
        }
    }

    public void Debug()
    {
        try
        {
            m_isDebugging = true;
            Run();
        }
        finally
        {
            m_isDebugging = false;
        }
    }

    public void Run()
    {
        const int maxInstructionExecutions = 150_000;

        DebugSnap debugSnap = null;

        // Execute the instructions.
        var instructionExecutions = 0;
        var instructions = m_program.Instructions;
        var instructionCount = instructions.Length;
        while (m_ip < instructionCount)
        {
            var instr = instructions[m_ip];
            try
            {
                if (m_isDebugging)
                    debugSnap = new DebugSnap(m_program, m_ip, m_callStack, CurrentFrame);
                
                var keepRunning = Execute(instr);
                if (m_isDebugging)
                {
                    var newDebugSnap = new DebugSnap(m_program, m_ip, m_callStack, CurrentFrame);
                    Console.Write(debugSnap!.GetDiff(newDebugSnap));
                    debugSnap = newDebugSnap;
                }
                
                if (!keepRunning)
                    break;
                
                instructionExecutions++;
            }
            catch (Exception e)
            {
                var sb = new StringBuilder();
                sb.Append('-', 50);
                sb.AppendLine();
                
                var message = Regex.Replace(
                    e.Message,
                    @"\$(\d+)",
                    match => m_program.SymbolTable[int.Parse(match.Groups[1].Value)]);
                sb.AppendLine(message);
                sb.AppendLine($"  └─ {m_ip}: {instr}");

                var callstack = m_callStack
                    .Select(o => m_program.LabelTable.GetLabelFromInstructionPointer(o.functionLabel))
                    .Append("<Root>");
                sb.AppendLine();
                sb.AppendLine("Callstack:");
                foreach(var funcName in callstack)
                    sb.AppendLine($"  → {funcName}");

                sb.AppendLine();
                var state = CurrentFrame.ToUiString(m_program.SymbolTable);
                sb.Append(state);

                Logger.Instance.Error(sb.ToString());
                
                m_program.Dump();
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
        m_uniforms = new List<string>(4)
        {
            "retval"
        };
    }

    private bool Execute(Instruction instr)
    {
        switch (instr.OpCode)
        {
            case OpCode.Nop: m_ip++; break;
            case OpCode.Decl: ExecuteDecl(instr); break;
            case OpCode.Ld: ExecuteLd(instr); break;
            case OpCode.Ldc: ExecuteLdc(instr); break;
            case OpCode.Add: ExecuteAdd(instr); break;
            case OpCode.Sub: ExecuteSub(instr); break;
            case OpCode.Inc: ExecuteInc(instr); break;
            case OpCode.Dec: ExecuteDec(instr); break;
            case OpCode.Neg: ExecuteNeg(instr); break;
            case OpCode.Mul: ExecuteMul(instr); break;
            case OpCode.Div: ExecuteDiv(instr); break;
            case OpCode.Dim: ExecuteDim(instr); break;
            case OpCode.Shiftl: ExecuteShiftL(instr); break;
            case OpCode.Shiftr: ExecuteShiftR(instr); break;
            case OpCode.BitAnd: ExecuteBitAnd(instr); break;
            case OpCode.BitOr: ExecuteBitOr(instr); break;
            case OpCode.Halt: return false;
            case OpCode.Lt: ExecuteLt(instr); break;
            case OpCode.Le: ExecuteLe(instr); break;
            case OpCode.Gt: ExecuteGt(instr); break;
            case OpCode.Ge: ExecuteGe(instr); break;
            case OpCode.Eq: ExecuteEq(instr); break;
            case OpCode.Ne: ExecuteNe(instr); break;
            case OpCode.And: ExecuteAnd(instr); break;
            case OpCode.Or: ExecuteOr(instr); break;
            case OpCode.Not: ExecuteNot(instr); break;
            case OpCode.Test: ExecuteTest(instr); break;
            case OpCode.Jmp: ExecuteJmp(instr); break;
            case OpCode.Jmpz: ExecuteJmpZ(instr); break;
            case OpCode.Jmpnz: ExecuteJmpNz(instr); break;
            case OpCode.Print: ExecutePrint(instr, debug: false); break;
            case OpCode.Debug: ExecutePrint(instr, debug: true); break;
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
            case OpCode.Floor: ExecuteFloor(instr); break;
            case OpCode.Fract: ExecuteFract(instr); break;
            case OpCode.Length: ExecuteLength(instr); break;
            case OpCode.Normalize: ExecuteNormalize(instr); break;
            case OpCode.Clamp: ExecuteClamp(instr); break;
            case OpCode.Mix: ExecuteMix(instr); break;
            case OpCode.Smoothstep: ExecuteSmoothstep(instr); break;
            case OpCode.Dot: ExecuteDot(instr); break;
            case OpCode.Reflect: ExecuteReflect(instr); break;
            case OpCode.Refract: ExecuteRefract(instr); break;
            case OpCode.Cross: ExecuteCross(instr); break;
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
        return operand.Type == OperandType.Variable ? CurrentFrame.GetVariable(operand.Name, m_program.SymbolTable) : operand;
    }

    /// <summary>
    /// E.g. decl $a
    /// E.g. decl $a, $b, ...
    /// </summary>
    private void ExecuteDecl(Instruction instr)
    {
        instr.Operands.ForEach(o =>
        {
            if (o.Type != OperandType.Variable)
                throw new RuntimeException("Variable operand expected.");
            if (!CurrentFrame.IsDefinedLocally(o.Name))
               CurrentFrame.DefineVariable(o.Name, Operand.Unassigned);
        });
        m_ip++;
    }
    
    /// <summary>
    /// E.g. ld $a, 3.141           (a = 3.141)
    /// E.g. ld $a, $b              (a = b)
    /// E.g. ld $a, 1.1, 2.2, 3.3   (a = [1.1, 2.2, 3.3])
    /// </summary>
    private void ExecuteLd(Instruction instr)
    {
        var a = instr.Operands[0];
        var b = UnpackBPlusOperands(instr.Operands);

        CurrentFrame.SetVariable(a.Name, b.Clone(), true);
        m_ip++;
    }
    
    /// <summary>
    /// As with 'ld', but sets the value in the scope of the function's caller.
    /// </summary>
    private void ExecuteLdc(Instruction instr)
    {
        var a = instr.Operands[0];
        var b = UnpackBPlusOperands(instr.Operands);
        
        var callerFrame = CurrentFrame.CallerFrame;
        if (callerFrame == null)
            throw new RuntimeException("Cannot set value - No caller function found.");
        callerFrame.SetVariable(a.Name, b, true);
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
    /// E.g. inc $a    (a = a + 1)
    /// </summary>
    private void ExecuteInc(Instruction instr) =>
        DoMathOp(instr, (a, _) => ++a);

    /// <summary>
    /// E.g. dec $a    (a = a - 1)
    /// </summary>
    private void ExecuteDec(Instruction instr) =>
        DoMathOp(instr, (a, _) => --a);

    /// <summary>
    /// E.g. neg $a    (a = -a)
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
    /// Jump if zero.
    /// E.g. jmpz $a, label
    /// </summary>
    private void ExecuteJmpZ(Instruction instr)
    {
        var a = instr.Operands[0];
        var aValue = CurrentFrame.GetVariable(a.Name);

        var jump = aValue.AsFloat().IsApproximately(0.0f);
        if (!jump)
        {
            m_ip++;
            return;
        }
        
        var label = instr.Operands[1];
        m_ip = label.Int;
    }

    /// <summary>
    /// Jump if not zero.
    /// E.g. jmpnz $a, label
    /// </summary>
    private void ExecuteJmpNz(Instruction instr)
    {
        var a = instr.Operands[0];
        var aValue = CurrentFrame.GetVariable(a.Name);

        var jump = !aValue.AsFloat().IsApproximately(0.0f);
        if (!jump)
        {
            m_ip++;
            return;
        }
        
        var label = instr.Operands[1];
        m_ip = label.Int;
    }

    /// <summary>
    /// E.g. mul $a, 3.141
    /// E.g. mul $a, $b     (a *= b)
    /// </summary>
    /// <remarks>
    /// This has a different implementation to ExecuteDiv, as we need to
    /// support matrix/vector multiplication.
    /// </remarks>
    private void ExecuteMul(Instruction instr)
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
        
        // Get 2nd+ operands.
        var operands = new Operand[instr.Operands.Length - 1];
        for (var i = 1; i < instr.Operands.Length; i++)
            operands[i - 1] = GetOperandValue(instr.Operands[i]);
        var b = Operand.FromOperands(operands);
        if (b.Type is OperandType.Label or OperandType.Variable)
            throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {b.Type}.");

        if (!CurrentFrame.IsDefined(aName))
            throw new RuntimeException($"Variable '{aName}' is not defined.");

        float[] floats;
        if (a.Length == 1 && b.Length == 1)
        {
            // Simple a * b
            floats = [ a.Floats[0] * b.Floats[0] ];
        }
        else if (b.Length == a.Length * a.Length)
        {
            // Vector × Matrix (column-major)
            floats = new float[a.Length];
            for (var col = 0; col < a.Length; col++)
            {
                var sum = 0f;
                for (var row = 0; row < a.Length; row++)
                    sum += a.Floats[row] * b.Floats[col * a.Length + row];
                floats[col] = sum;
            }
        }
        else
        {
            EnsureArrayDimensionsMatch(instr, ref a, ref b);

            floats = new float[a.Length];
            for (var i = 0; i < a.Length; i++)
                floats[i] = a.Floats[i] * b.Floats[i];
        }

        OperandType resultType;
        if (a.Type == OperandType.Int && b.Type == OperandType.Int)
            resultType = OperandType.Int;
        else
            resultType = floats.Length > 1 ? OperandType.Vector : OperandType.Float;
        var result = new Operand(floats) { Type = resultType };

        // Store the result.
        CurrentFrame.SetVariable(aName, result, true);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. div $a, 3.141
    /// E.g. div $a, $b     (a /= b)
    /// </summary>
    private void ExecuteDiv(Instruction instr) =>
        DoMathOp(instr, (a, b) => b == 0.0f ? throw new RuntimeException("Division by zero.") : a / b);

    /// <summary>
    /// E.g. dim $a, $b    (Sets length of $a vector to 'b')
    /// </summary>
    private void ExecuteDim(Instruction instr)
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

        // Get length operand.
        var b = GetOperandValue(instr.Operands[1]);
        if (b.Type is not OperandType.Int)
            throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' with non-integer operand {b.Type}.");
        if (b.Int == 0)
            throw new RuntimeException("Cannot set vector length to zero.");
        
        if (a.Length != b.Int)
        {
            // Store the result.
            if (b.Int == 1)
            {
                CurrentFrame.SetVariable(aName, new Operand(a.Float), true);
            }
            else
            {
                float[] floats;
                if (a.Length == 1)
                {
                    // If $a is single float, duplicate 'b' times.
                    floats = Enumerable.Repeat(a.Float, b.Int).ToArray();
                }
                else
                {
                    if (a.Length < b.Int)
                        throw new RuntimeException($"Cannot increase vector length from {a.Length} to {b.Int}.");

                    // Truncate vector to a length of 'b'.
                    floats = new float[b.Int];
                    for (var i = 0; i < b.Int; i++)
                        floats[i] = a.Floats[i];
                }
            
                CurrentFrame.SetVariable(aName, new Operand(floats), true);
            }
        }

        m_ip++;
    }

    /// <summary>
    /// E.g. lt $a, $b    (a = a < b)
    /// </summary>
    private void ExecuteLt(Instruction instr) =>
        DoMathOp(instr, (a, b) => a < b ? 1.0f : 0.0f);

    /// <summary>
    /// E.g. le $a, $b    (a = a <= b)
    /// </summary>
    private void ExecuteLe(Instruction instr) =>
        DoMathOp(instr, (a, b) => a <= b ? 1.0f : 0.0f);

    /// <summary>
    /// E.g. gt $a, $b    (a = a > b)
    /// </summary>
    private void ExecuteGt(Instruction instr) =>
        DoMathOp(instr, (a, b) => a > b ? 1.0f : 0.0f);

    /// <summary>
    /// E.g. ge $a, $b    (a = a >= b)
    /// </summary>
    private void ExecuteGe(Instruction instr) =>
        DoMathOp(instr, (a, b) => a >= b ? 1.0f : 0.0f);

    /// <summary>
    /// E.g. eq $a, $b    (a = a == b)
    /// </summary>
    private void ExecuteEq(Instruction instr) =>
        DoMathOp(instr, (a, b) => a.IsApproximately(b) ? 1.0f : 0.0f, resultAsBool: true);

    /// <summary>
    /// E.g. ne $a, $b    (a = a != b)
    /// </summary>
    private void ExecuteNe(Instruction instr) =>
        DoMathOp(instr, (a, b) => !a.IsApproximately(b) ? 1.0f : 0.0f, resultAsBool: true);

    /// <summary>
    /// E.g. and $a, $b    (a = a && b)
    /// </summary>
    private void ExecuteAnd(Instruction instr) =>
        DoMathOp(instr, (a, b) => a != 0.0f && b != 0.0f ? 1.0f : 0.0f);

    /// <summary>
    /// E.g. or $a, $b    (a = a || b)
    /// </summary>
    private void ExecuteOr(Instruction instr) =>
        DoMathOp(instr, (a, b) => a != 0.0f || b != 0.0f ? 1.0f : 0.0f);

    /// <summary>
    /// E.g. not $a    (a = !a)
    /// </summary>
    private void ExecuteNot(Instruction instr) =>
        DoMathOp(instr, (a, _) => a == 0.0f ? 1.0f : 0.0f);
    
    /// <summary>
    /// E.g. test $a, $b    (a = 1 if 'b' is non-zero)
    /// </summary>
    private void ExecuteTest(Instruction instr)
    {
        // Get target variable.
        var a = instr.Operands[0];
        var aName = a.Name;
        
        if (CurrentFrame.IsDefined(aName))
        {
            a = CurrentFrame.GetVariable(aName, allowUndefined: true);
            if (a.Type is OperandType.Label or OperandType.Variable)
                throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {a.Type}.");
        }

        // Get test variable.
        var b = UnpackBPlusOperands(instr.Operands);

        // Store the result.
        var result = new Operand(b.Floats.Any(o => o != 0.0) ? 1 : 0);
        CurrentFrame.SetVariable(aName, result, true);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. shiftr $a, 2    (a = a >> 2)
    /// </summary>
    private void ExecuteShiftR(Instruction instr)
    {
        // Get target variable.
        var a = instr.Operands[0];
        var aName = a.Name;
        if (CurrentFrame.IsDefined(aName))
        {
            a = CurrentFrame.GetVariable(aName);
            if (a.Type is not OperandType.Int)
                throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {a.Type}.");
        }

        var b = GetOperandValue(instr.Operands[1]);
        var result = new Operand(a.Int >> b.Int);
            
        // Store the result.
        CurrentFrame.SetVariable(aName, result, true);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. shiftl $a, 2    (a = a << 2)
    /// </summary>
    private void ExecuteShiftL(Instruction instr)
    {
        // Get target variable.
        var a = instr.Operands[0];
        var aName = a.Name;
        if (CurrentFrame.IsDefined(aName))
        {
            a = CurrentFrame.GetVariable(aName);
            if (a.Type is not OperandType.Int)
                throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {a.Type}.");
        }

        var b = GetOperandValue(instr.Operands[1]);
        var result = new Operand(a.Int << b.Int);
            
        // Store the result.
        CurrentFrame.SetVariable(aName, result, true);
        m_ip++;
    }

    /// <summary>
    /// E.g. bit_and $a, 2    (a = a & 2)
    /// </summary>
    private void ExecuteBitAnd(Instruction instr)
    {
        // Get target variable.
        var a = instr.Operands[0];
        var aName = a.Name;
        if (CurrentFrame.IsDefined(aName))
        {
            a = CurrentFrame.GetVariable(aName);
            if (a.Type is not OperandType.Int)
                throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {a.Type}.");
        }

        var b = GetOperandValue(instr.Operands[1]);
        var result = new Operand(a.Int & b.Int);
            
        // Store the result.
        CurrentFrame.SetVariable(aName, result, true);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. bit_or $a, 2    (a = a & 2)
    /// </summary>
    private void ExecuteBitOr(Instruction instr)
    {
        // Get target variable.
        var a = instr.Operands[0];
        var aName = a.Name;
        if (CurrentFrame.IsDefined(aName))
        {
            a = CurrentFrame.GetVariable(aName);
            if (a.Type is not OperandType.Int)
                throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {a.Type}.");
        }

        var b = GetOperandValue(instr.Operands[1]);
        var result = new Operand(a.Int | b.Int);
            
        // Store the result.
        CurrentFrame.SetVariable(aName, result, true);
        m_ip++;
    }

    /// <summary>
    /// E.g. print 3.141
    /// E.g. print $a
    /// </summary>
    private void ExecutePrint(Instruction instr, bool debug)
    {
        var a = instr.Operands[0];
        var value = Operand.FromOperands(instr.Operands.Select(GetOperandValue).ToArray());

        var toPrint = value.ToString();
        if (a.Type == OperandType.Variable)
            toPrint = $"{a.ToUiString(m_program.SymbolTable).TrimStart('$')} = {toPrint}";

        if (debug)
        {
            var functionName = m_callStack.Count > 0 ? $"{m_program.LabelTable.GetLabelFromInstructionPointer(m_callStack.Peek().functionLabel)}()" : "<Root>";
            toPrint = $"{toPrint,-30} : {functionName}";
        }
        
        OutputWritten?.Invoke(this, toPrint);
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
        m_frames.Push(new ScopeFrame(ScopeType.Block, CurrentFrame));
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
        m_callStack.Push((label.Int, m_ip + 1, m_frames.Count));
        m_frames.Push(new ScopeFrame(ScopeType.Function, CurrentFrame));
        m_ip = label.Int;
    }

    /// <summary>
    /// Return from a procedure (implicitly popping the top-scoped variable frame).
    /// E.g. ret
    /// E.g. ret $a
    /// E.g. ret $r, $g, $b
    /// E.g. ret 123
    /// </summary>
    private void ExecuteRet(Instruction instr)
    {
        if (m_callStack.Count == 0)
            throw new RuntimeException("No procedure to return to.");

        Operand a;
        if (instr.Operands.Length > 0)
        {
            // Returning a value.
            var operands = new Operand[instr.Operands.Length];
            for (var i = 0; i < operands.Length; i++)
                operands[i] = GetOperandValue(instr.Operands[i]);
            a = Operand.FromOperands(operands);
        }
        else
        {
            // Void function.
            a = null;
        }

        var preCallStackInfo = m_callStack.Pop();

        // Pop the scoped variable frame(s).
        while (m_frames.Count > preCallStackInfo.scopeFrameDepth)
            m_frames.Pop();

        // Make the return value available to the caller (may be null).
        CurrentFrame.Retval = a;
        
        // Restore the IP.
        m_ip = preCallStackInfo.returnIp;
    }

    /// <summary>
    /// E.g. sin $a, $b    (a = sin(b))
    /// </summary>
    private void ExecuteSin(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Sin(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. sinh $a, $b    (a = sinh(b))
    /// </summary>
    private void ExecuteSinh(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Sinh(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. asin $a, $b    (a = asin(b))
    /// </summary>
    private void ExecuteAsin(Instruction instr)
    {
        DoMathOp(instr, (_, b) =>
        {
            if (b < -1f || b > 1f)
                throw new RuntimeException($"Input value '{b:0.0###}' must be in the range [-1, 1].");
            return MathF.Asin(b);
        }, requiresSingleInput: true);
    }

    /// <summary>
    /// E.g. cos $a, $b    (a = cos(b))
    /// </summary>
    private void ExecuteCos(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Cos(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. cosh $a, $b    (a = cosh(b))
    /// </summary>
    private void ExecuteCosh(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Cosh(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. acos $a, $b    (a = acos(b))
    /// </summary>
    private void ExecuteAcos(Instruction instr)
    {
        DoMathOp(instr, (_, b) =>
        {
            if (b < -1f || b > 1f)
                throw new RuntimeException($"Input value '{b:0.0###}' must be in the range [-1, 1].");
            return MathF.Acos(b);
        }, requiresSingleInput: true);
    }

    /// <summary>
    /// E.g. tan $a, $b    (a = tan(b))
    /// </summary>
    private void ExecuteTan(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Tan(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. tanh $a, $b    (a = tanh(b))
    /// </summary>
    private void ExecuteTanh(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Tanh(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. atan $a, $b    (a = atan(b))
    /// </summary>
    private void ExecuteAtan(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Atan(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. pow $a, $b    (a = pow(a, b))
    /// </summary>
    private void ExecutePow(Instruction instr)
    {
        DoMathOp(instr, (a, b) =>
        {
            if (a < 0.0f)
                throw new RuntimeException($"Negative input value '{a:0.0###}' cannot be raised to any power.");
            if (a == 0.0f && b < 0.0f)
                throw new RuntimeException($"Zero cannot be raised to a negative power (base: {a:0.0###}, exponent: {b:0.0###}).");
            return MathF.Pow(a, b);
        });
    }
    
    /// <summary>
    /// E.g. sqrt $a, $b    (a = sqrt(b))
    /// </summary>
    private void ExecuteSqrt(Instruction instr)
    {
        DoMathOp(instr, (_, b) =>
        {
            if (b < 0f)
                throw new RuntimeException($"Input value '{b:0.0###}' must be greater than or equal to zero.");
            return MathF.Sqrt(b);
        }, requiresSingleInput: true);
    }

    /// <summary>
    /// E.g. exp $a, $b    (a = exp(b))
    /// </summary>
    private void ExecuteExp(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Exp(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. log $a, $b    (a = log(b))
    /// </summary>
    private void ExecuteLog(Instruction instr)
    {
        DoMathOp(instr, (_, b) =>
        {
            if (b <= 0f)
                throw new RuntimeException($"Input value '{b:0.0###}' must be greater than 0.");
            return MathF.Log(b);
        }, requiresSingleInput: true);
    }

    /// <summary>
    /// E.g. abs $a, $b    (a = abs(b))
    /// </summary>
    private void ExecuteAbs(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Abs(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. sign $a, $b    (a = sign(b))
    /// </summary>
    private void ExecuteSign(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Sign(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. mod $a, $b    (a = a % b)
    /// </summary>
    private void ExecuteMod(Instruction instr) =>
        DoMathOp(instr, (a, b) =>
        {
            if (b == 0f)
                throw new RuntimeException("Modulus by zero is undefined.");
            return a - b * MathF.Floor(a / b);
        });

    /// <summary>
    /// E.g. min $a, $b    (a = min(a, b))
    /// </summary>
    private void ExecuteMin(Instruction instr) =>
        DoMathOp(instr, (a, b) => a < b ? a : b);

    /// <summary>
    /// E.g. max $a, $b    (a = max(a, b))
    /// </summary>
    private void ExecuteMax(Instruction instr) =>
        DoMathOp(instr, (a, b) => a > b ? a : b);

    /// <summary>
    /// E.g. ceil $a, $b    (a = ceil(b))
    /// </summary>
    private void ExecuteCeil(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Ceiling(b), requiresSingleInput: true);
    
    /// <summary>
    /// E.g. floor $a, $b    (a = floor(b))
    /// </summary>
    private void ExecuteFloor(Instruction instr) =>
        DoMathOp(instr, (_, b) => MathF.Floor(b), requiresSingleInput: true);
    
    /// <summary>
    /// E.g. fract $a, $b    (a = fract(b))
    /// </summary>
    private void ExecuteFract(Instruction instr) =>
        DoMathOp(instr, (_, b) => b - MathF.Floor(b), requiresSingleInput: true);

    /// <summary>
    /// E.g. length $a, $b    (a = length(b))
    /// </summary>
    private void ExecuteLength(Instruction instr)
    {
        var a = instr.Operands[0];
        var b = UnpackBPlusOperands(instr.Operands);
        
        // Calculate length.
        var sum2 = 0.0f;
        for (var i = 0; i < b.Floats.Length; i++)
        {
            var f = b.Floats[i];
            sum2 += f * f;
        }
        var l = new Operand(sum2 == 0.0f ? 0.0f : MathF.Sqrt(sum2));

        CurrentFrame.DefineVariable(a.Name, l);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. normalize $a, $b    (a = normalize(b))
    /// </summary>
    private void ExecuteNormalize(Instruction instr)
    {
        var a = instr.Operands[0];
        var b = UnpackBPlusOperands(instr.Operands);

        if (b.Length == 1)
            throw new RuntimeException($"Cannot normalize a single value ({b.ToUiString()}).");

        // Calculate length.
        var sum2 = 0.0f;
        foreach (var f in b.Floats)
            sum2 += f * f;
        var result = new Operand(new float[b.Length]);
        if (sum2 > 0.0f)
        {
            var l = MathF.Sqrt(sum2);
            for (var i = 0; i < result.Floats.Length; i++)
                result.Floats[i] = b.Floats[i] / l;
        }

        CurrentFrame.DefineVariable(a.Name, result);
        m_ip++;
    }

    /// <summary>
    /// E.g. clamp $a, $from, $to    (a = clamp(a, from, to))
    /// </summary>
    private void ExecuteClamp(Instruction instr)
    {
        if (instr.Operands.Length < 3)
            throw new RuntimeException("Expected: clamp $a, $from, $to");

        DoMathOp(instr, (a, b, c) => MathF.Max(b, MathF.Min(c, a)));
    }
    
    /// <summary>
    /// E.g. mix $a, $from, $to    (a = mix(a, from, to))
    /// </summary>
    private void ExecuteMix(Instruction instr)
    {
        if (instr.Operands.Length < 3)
            throw new RuntimeException("Expected: mix $a, $from, $to");

        DoMathOp(instr, (a, b, c) => c.Lerp(a, b));
    }

    /// <summary>
    /// E.g. smoothstep $a, $edge0, $edge1    (a = smoothstep(a, edge0, edge1))
    /// </summary>
    private void ExecuteSmoothstep(Instruction instr)
    {
        if (instr.Operands.Length != 3)
            throw new RuntimeException("Expected: smoothstep $a, $edge0, $edge1");

        DoMathOp(instr, (x, edge0, edge1) =>
        {
            if (edge0.Equals(edge1))
                throw new RuntimeException("edge0 and edge1 must not be equal.");

            var t = ((x - edge0) / (edge1 - edge0)).Clamp(0.0f, 1.0f);
            return t * t * (3.0f - 2.0f * t);
        });
    }

    /// <summary>
    /// E.g. dot $a, $b    (a = dot(a, b))
    /// </summary>
    private void ExecuteDot(Instruction instr)
    {
        var a = instr.Operands[0];
        var aName = a.Name;
        a = GetOperandValue(a);
        var b = UnpackBPlusOperands(instr.Operands);

        EnsureArrayDimensionsMatch(instr, ref a, ref b);

        // Calculate length.
        var result = 0.0f;
        for (var i = 0; i < a.Floats.Length; i++)
            result += a.Floats[i] * b.Floats[i];

        CurrentFrame.DefineVariable(aName, new Operand(result));
        m_ip++;
    }

    /// <summary>
    /// E.g. reflect $a, $n    (a = reflect(a, n))
    /// </summary>
    private void ExecuteReflect(Instruction instr)
    {
        var a = instr.Operands[0];
        var aName = a.Name;
        a = GetOperandValue(a);
        var b = UnpackBPlusOperands(instr.Operands);

        EnsureArrayDimensionsMatch(instr, ref a, ref b);

        // Compute reflection vector: R = I - 2 * dot(N, I) * N
        var dot = 0.0f;
        for (var i = 0; i < a.Length; i++)
            dot += a.Floats[i] * b.Floats[i];

        var result = new float[a.Length];
        for (var i = 0; i < a.Length; i++)
            result[i] = a.Floats[i] - 2.0f * dot * b.Floats[i];

        CurrentFrame.DefineVariable(aName, new Operand(result));
        m_ip++;
    }
    
    /// <summary>
    /// E.g. refract $a, $n, $eta    (a = refract(a, n, eta))
    /// </summary>
    private void ExecuteRefract(Instruction instr)
    {
        var a = instr.Operands[0];
        var aName = a.Name;
        var bName = instr.Operands[1].Name;
        a = GetOperandValue(instr.Operands[0]);
        var b = GetOperandValue(instr.Operands[1]);

        // Both operands must be a 3D vector.
        if (a.Length != 3 || b.Length != 3)
            throw new RuntimeException($"Operation is only valid for 3D vectors ({aName}: {a.ToUiString()}, {bName}: {b.ToUiString()}).");

        // Get eta value from last operand
        var eta = GetOperandValue(instr.Operands[2]).Float;
    
        // Compute dot product of incident vector and normal
        var dot = 0.0f;
        for (var i = 0; i < a.Length; i++)
            dot += a.Floats[i] * b.Floats[i];
    
        // Compute refraction vector
        var k = 1.0f - eta * eta * (1.0f - dot * dot);
        
        var result = new float[a.Length];
        if (k >= 0.0f) 
        {
            var f = eta * dot + MathF.Sqrt(k);
            for (var i = 0; i < a.Length; i++)
                result[i] = eta * a.Floats[i] - f * b.Floats[i];
        }
    
        CurrentFrame.SetVariable(aName, new Operand(result));
        m_ip++;
    }
    
    /// <summary>
    /// E.g. cross $a, $b    (a = cross(a, b))
    /// </summary>
    private void ExecuteCross(Instruction instr)
    {
        var a = instr.Operands[0];
        var aName = a.Name;
        var bName = instr.Operands[1].Name;
        a = GetOperandValue(a);
        var b = UnpackBPlusOperands(instr.Operands);

        // Both operands must be a 3D vector.
        if (a.Length != 3 || b.Length != 3)
            throw new RuntimeException($"Operation is only valid for 3D vectors ({aName}: {a.ToUiString()}, {bName}: {b.ToUiString()}).");

        // Compute cross product.
        var result = new float[a.Length];
        result[0] = a.Floats[1] * b.Floats[2] - a.Floats[2] * b.Floats[1];
        result[1] = a.Floats[2] * b.Floats[0] - a.Floats[0] * b.Floats[2];
        result[2] = a.Floats[0] * b.Floats[1] - a.Floats[1] * b.Floats[0];

        CurrentFrame.DefineVariable(aName, new Operand(result));
        m_ip++;
    }    
    
    /// <summary>
    /// Applies a binary float operation element-wise between the target variable <c>$a</c>
    /// and the second operand (e.g., <c>$b</c>), storing the result back in <c>$a</c>.
    /// 
    /// This supports scalar or vector operands.
    /// </summary>
    private void DoMathOp(Instruction instr, Func<float, float, float> op, bool resultAsBool = false, bool requiresSingleInput = false)
    {
        // Get target variable.
        var a = instr.Operands[0];
        var aName = a.Name;
        if (CurrentFrame.IsDefined(aName))
        {
            a = CurrentFrame.GetVariable(aName, allowUndefined: requiresSingleInput);
            if (a.Type is OperandType.Label or OperandType.Variable)
                throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {a.Type}.");
        }

        // Single operand? We can do that...
        Operand result;
        if (instr.Operands.Length == 1)
        {
            var floats = new float[a.Length];
            for (var i = 0; i < a.Length; i++)
                floats[i] = op(a.Floats[i], float.NaN);

            result = a.Type == OperandType.Int ? new Operand(floats) { Type = OperandType.Int } : new Operand(floats);
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

            float[] floats;
            if (CurrentFrame.IsDefined(aName))
            {
                if (requiresSingleInput)
                {
                    floats = new float[b.Length];
                    for (var i = 0; i < b.Length; i++)
                        floats[i] = op(float.NaN, b.Floats[i]);
                }
                else
                {
                    EnsureArrayDimensionsMatch(instr, ref a, ref b);

                    floats = new float[a.Length];
                    for (var i = 0; i < a.Length; i++)
                        floats[i] = op(a.Floats[i], b.Floats[i]);
                }
            }
            else
            {
                floats = new float[b.Length];
                for (var i = 0; i < b.Length; i++)
                    floats[i] = op(float.NaN, b.Floats[i]);
            }

            if (a.Type == OperandType.Int && b.Type == OperandType.Int)
                result = new Operand(floats) { Type = OperandType.Int };
            else
                result = resultAsBool ? new Operand(floats.FastAll(0.0f) ? 0.0f : 1.0f) : new Operand(floats);
        }

        // Store the result.
        CurrentFrame.SetVariable(aName, result, true);
        m_ip++;
    }

    /// <summary>
    /// Applies a ternary float operation element-wise across three operands: the target <c>$a</c>,
    /// and inputs <c>$b</c> and <c>$c</c>, storing the result back in <c>$a</c>.
    ///
    /// The lambda receives each element as <c>(a, b, c)</c>.
    /// </summary>
    private void DoMathOp(Instruction instr, Func<float, float, float, float> op)
    {
        // Get target variable.
        var a = instr.Operands[0];
        var aName = a.Name;
        Operand aVal;
        if (CurrentFrame.IsDefined(aName))
        {
            aVal = CurrentFrame.GetVariable(aName);
            if (aVal.Type is OperandType.Label or OperandType.Variable)
                throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {aVal.Type}.");
        }
        else
        {
            aVal = new Operand(new float[1]);
        }

        if (instr.Operands.Length < 3)
            throw new RuntimeException("Expected three operands for this operation.");

        var b = GetOperandValue(instr.Operands[1]);
        var c = GetOperandValue(instr.Operands[2]);

        if (b.Type is OperandType.Label or OperandType.Variable)
            throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {b.Type}.");
        if (c.Type is OperandType.Label or OperandType.Variable)
            throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on {c.Type}.");

        // Make sure all arrays are compatible
        EnsureArrayDimensionsMatch(instr, ref aVal, ref b);
        EnsureArrayDimensionsMatch(instr, ref aVal, ref c);

        var result = new Operand(new float[aVal.Length]);
        for (var i = 0; i < aVal.Length; i++)
            result.Floats[i] = op(aVal.Floats[i], b.Floats[i], c.Floats[i]);

        if (aVal.Type == OperandType.Int && b.Type == OperandType.Int && c.Type == OperandType.Int)
            result = result.WithType(OperandType.Int);

        // Store the result.
        CurrentFrame.SetVariable(aName, result, true);
        m_ip++;
    }

    private static void EnsureArrayDimensionsMatch(Instruction instr, ref Operand a, ref Operand b)
    {
        // Ensure that the operands have the same dimension.
        if (a.Length == 1 && b.Length > 1)
            a = a.GrowFromOneToN(b.Length);
        else if (b.Length == 1 && a.Length > 1)
            b = b.GrowFromOneToN(a.Length);

        if (a.Length != b.Length)
            throw new RuntimeException($"Cannot perform '{OpCodeToStringMap.GetString(instr.OpCode)}' on operands of different length ({a.Length} vs {b.Length}).");
    }

    /// <summary>
    /// Find the '$b' operand, and any that follow, and unpack into a single array operand.
    /// </summary>
    private Operand UnpackBPlusOperands(Operand[] operands)
    {
        if (operands.Length == 2)
        {
            // Just one 'b' operand.
            return GetOperandValue(operands[1]);
        }
        
        // Get 2nd+ operands.
        var moreOperands = new Operand[operands.Length - 1];
        for (var i = 1; i < operands.Length; i++)
            moreOperands[i - 1] = GetOperandValue(operands[i]);
        return Operand.FromOperands(moreOperands);
    }

    public void AddUniform(string name, Operand value)
    {
        if (m_uniforms.Contains(name))
            throw new RuntimeException($"Uniform '{name}' already defined.");
        CurrentFrame.DefineVariable($"{m_uniforms.Count}", value);
        m_uniforms.Add(name);
    }
}