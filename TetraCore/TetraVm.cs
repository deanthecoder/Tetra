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

using System.Globalization;
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
    private readonly Instruction[] m_instructions;
    private readonly Stack<ScopeFrame> m_frames = [];
    private readonly Stack<int> m_callStack = [];
    private int m_ip;

    private ScopeFrame CurrentFrame => m_frames.Peek();

    public event EventHandler<string> OutputWritten; 

    public TetraVm(Instruction[] instructions)
    {
        m_instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
        
        OutputWritten += (_, message) => Console.WriteLine(message);
    }
    
    public Operand this[string variableName] => CurrentFrame.GetVariable(variableName);

    public void Run()
    {
        try
        {
            RunImpl();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }
    
    private void RunImpl()
    {
        const int maxInstructionExecutions = 10_000;

        // Reset the VM.
        m_frames.Clear();
        m_frames.Push(new ScopeFrame()); // Global scope
        m_callStack.Clear();
        m_ip = 0;

        // Execute the instructions.
        var instructionExecutions = 0;
        while (m_ip < m_instructions.Length)
        {
            var instr = m_instructions[m_ip];
            var keepRunning = Execute(instr);
            if (!keepRunning)
                break;
            instructionExecutions++;
            
            if (instructionExecutions >= maxInstructionExecutions)
                throw new RuntimeException("Too many instruction executions.");
        }
    }

    private bool Execute(Instruction instr)
    {
        switch (instr.OpCode)
        {
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
            case OpCode.PopFrame: ExecutePopFrame(instr); break;
            case OpCode.Call: ExecuteCall(instr); break;
            case OpCode.Ret: ExecuteRet(instr); break;
            default:
                throw new InvalidOperationException($"Instruction not supported: '{instr}'");
        }
        return true;
    }

    /// <summary>
    /// E.g. ld $a, 3.141
    /// E.g. ld $a, $b      (a = b)
    /// </summary>
    private void ExecuteLd(Instruction instr)
    {
        var variable = instr.Operands[0];
        var value = instr.Operands[1];
        
        CurrentFrame.DefineVariable(variable.Name, value);
        m_ip++;
    }

    /// <summary>
    /// E.g. add $a, 3.141
    /// E.g. add $a, $b     (a += b)
    /// </summary>
    private void ExecuteAdd(Instruction instr)
    {
        var variable = instr.Operands[0];
        var variableName = variable.Name;
        var value = instr.Operands[1];

        // If the operand is a variable, get its value.
        if (value.Type == OperandType.Variable)
            value = CurrentFrame.GetVariable(value.Name);

        var current = CurrentFrame.GetVariable(variableName);
        Operand result;
        if (current.Type == OperandType.Float || value.Type == OperandType.Float)
            result = new Operand(current.AsFloat() + value.AsFloat());
        else if (current.Type == OperandType.Int && value.Type == OperandType.Int)
            result = new Operand(current.IntValue + value.IntValue);
        else
            throw new RuntimeException($"'{instr}': Cannot add with {variable.Type} and {value.Type}.");

        CurrentFrame.SetVariable(variableName, result);
        m_ip++;
    }

    /// <summary>
    /// E.g. sub $a, 3.141
    /// E.g. sub $a, $b     (a -= b)
    /// </summary>
    private void ExecuteSub(Instruction instr)
    {
        var variable = instr.Operands[0];
        var variableName = variable.Name;
        var value = instr.Operands[1];
        
        // If the operand is a variable, get its value.
        if (value.Type == OperandType.Variable)
            value = CurrentFrame.GetVariable(value.Name);

        var current = CurrentFrame.GetVariable(variableName);
        Operand result;
        if (current.Type == OperandType.Float || value.Type == OperandType.Float)
            result = new Operand(current.AsFloat() - value.AsFloat());
        else if (current.Type == OperandType.Int && value.Type == OperandType.Int)
            result = new Operand(current.IntValue - value.IntValue);
        else
            throw new RuntimeException($"'{instr}': Cannot subtract with {variable.Type} and {value.Type}.");

        CurrentFrame.SetVariable(variableName, result);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. inc $a
    /// </summary>
    private void ExecuteInc(Instruction instr)
    {
        var variable = instr.Operands[0];
        var variableName = variable.Name;
        
        var current = CurrentFrame.GetVariable(variableName);
        var result = current.Type switch
        {
            OperandType.Float => new Operand(current.FloatValue + 1.0f),
            OperandType.Int => new Operand(current.IntValue + 1),
            _ => throw new RuntimeException($"'{instr}': Cannot increment {variable.Type}.")
        };

        CurrentFrame.SetVariable(variableName, result);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. dec $a
    /// </summary>
    private void ExecuteDec(Instruction instr)
    {
        var variable = instr.Operands[0];
        var variableName = variable.Name;
        
        var current = CurrentFrame.GetVariable(variableName);
        var result = current.Type switch
        {
            OperandType.Float => new Operand(current.AsFloat() - 1.0f),
            OperandType.Int => new Operand(current.IntValue - 1),
            _ => throw new RuntimeException($"'{instr}': Cannot decrement {variable.Type}.")
        };

        CurrentFrame.SetVariable(variableName, result);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. neg $a  (a = -a)
    /// </summary>
    private void ExecuteNeg(Instruction instr)
    {
        var variable = instr.Operands[0];
        var variableName = variable.Name;
        
        var current = CurrentFrame.GetVariable(variableName);
        var result = current.Type switch
        {
            OperandType.Float => new Operand(-current.AsFloat()),
            OperandType.Int => new Operand(-current.IntValue),
            _ => throw new RuntimeException($"'{instr}': Cannot negate {variable.Type}.")
        };

        CurrentFrame.SetVariable(variableName, result);
        m_ip++;
    }
    
    /// <summary>
    /// Unconditional jump.
    /// E.g. jmp NN
    /// </summary>
    private void ExecuteJmp(Instruction instr)
    {
        var label = instr.Operands[0];
        if (label.Type != OperandType.Int)
            throw new RuntimeException($"'{instr}': Integer operand expected.");
        m_ip = label.IntValue;
    }
    
    /// <summary>
    /// E.g. jmp_eq $a, $b, label
    /// E.g. jmp_eq $a, 2, label
    /// E.g. jmp_eq $a, 2.3, label
    /// </summary>
    private void ExecuteJmpEq(Instruction instr)
    {
        var a = instr.Operands[0];
        var b = instr.Operands[1];
        var label = instr.Operands[2];

        var aValue = CurrentFrame.GetVariable(a.Name);
        if (b.Type == OperandType.Variable)
            b = CurrentFrame.GetVariable(b.Name);
        
        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat().IsApproximately(b.AsFloat());
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.IntValue == b.IntValue;
        else
            throw new RuntimeException($"'{instr}': Cannot compare {a.Type} and {b.Type}.");
        
        if (jump)
            m_ip = label.IntValue;
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
        var b = instr.Operands[1];
        var label = instr.Operands[2];

        var aValue = CurrentFrame.GetVariable(a.Name);
        if (b.Type == OperandType.Variable)
            b = CurrentFrame.GetVariable(b.Name);
        
        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = !aValue.AsFloat().IsApproximately(b.AsFloat());
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.IntValue != b.IntValue;
        else
            throw new RuntimeException($"'{instr}': Cannot compare {a.Type} and {b.Type}.");
        
        if (jump)
            m_ip = label.IntValue;
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
        var b = instr.Operands[1];
        var label = instr.Operands[2];

        var aValue = CurrentFrame.GetVariable(a.Name);
        if (b.Type == OperandType.Variable)
            b = CurrentFrame.GetVariable(b.Name);
        
        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat() < b.AsFloat();
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.IntValue < b.IntValue;
        else
            throw new RuntimeException($"'{instr}': Cannot compare {a.Type} and {b.Type}.");
        
        if (jump)
            m_ip = label.IntValue;
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
        var b = instr.Operands[1];
        var label = instr.Operands[2];

        var aValue = CurrentFrame.GetVariable(a.Name);
        if (b.Type == OperandType.Variable)
            b = CurrentFrame.GetVariable(b.Name);
        
        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat() <= b.AsFloat();
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.IntValue <= b.IntValue;
        else
            throw new RuntimeException($"'{instr}': Cannot compare {a.Type} and {b.Type}.");
        
        if (jump)
            m_ip = label.IntValue;
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
        var b = instr.Operands[1];
        var label = instr.Operands[2];

        var aValue = CurrentFrame.GetVariable(a.Name);
        if (b.Type == OperandType.Variable)
            b = CurrentFrame.GetVariable(b.Name);
        
        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat() > b.AsFloat();
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.IntValue > b.IntValue;
        else
            throw new RuntimeException($"'{instr}': Cannot compare {a.Type} and {b.Type}.");
        
        if (jump)
            m_ip = label.IntValue;
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
        var b = instr.Operands[1];
        var label = instr.Operands[2];

        var aValue = CurrentFrame.GetVariable(a.Name);
        if (b.Type == OperandType.Variable)
            b = CurrentFrame.GetVariable(b.Name);
        
        bool jump;
        if (aValue.Type == OperandType.Float || b.Type == OperandType.Float)
            jump = aValue.AsFloat() >= b.AsFloat();
        else if (aValue.Type == OperandType.Int && b.Type == OperandType.Int)
            jump = aValue.IntValue >= b.IntValue;
        else
            throw new RuntimeException($"'{instr}': Cannot compare {a.Type} and {b.Type}.");
        
        if (jump)
            m_ip = label.IntValue;
        else
            m_ip++;
    }

    /// <summary>
    /// E.g. mul $a, 3.141
    /// E.g. mul $a, $b     (a *= b)
    /// </summary>
    private void ExecuteMul(Instruction instr)
    {
        var a = instr.Operands[0];
        var b = instr.Operands[1];

        // If the operand is a variable, get its value.
        if (b.Type == OperandType.Variable)
            b = CurrentFrame.GetVariable(b.Name);

        var current = CurrentFrame.GetVariable(a.Name);
        Operand result;
        if (current.Type == OperandType.Float || b.Type == OperandType.Float)
            result = new Operand(current.AsFloat() * b.AsFloat());
        else if (current.Type == OperandType.Int && b.Type == OperandType.Int)
            result = new Operand(current.IntValue * b.IntValue);
        else
            throw new RuntimeException($"'{instr}': Cannot multiply with {a.Type} and {b.Type}.");

        CurrentFrame.SetVariable(a.Name, result);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. div $a, 3.141
    /// E.g. div $a, $b     (a /= b)
    /// </summary>
    private void ExecuteDiv(Instruction instr)
    {
        var a = instr.Operands[0];
        var b = instr.Operands[1];

        // If the operand is a variable, get its value.
        if (b.Type == OperandType.Variable)
            b = CurrentFrame.GetVariable(b.Name);
        
        var current = CurrentFrame.GetVariable(a.Name);
        Operand result;
        try
        {
            if (current.Type == OperandType.Float || b.Type == OperandType.Float)
                result = new Operand(current.AsFloat() / b.AsFloat());
            else if (current.Type == OperandType.Int && b.Type == OperandType.Int)
                result = new Operand(current.IntValue / b.IntValue);
            else
                throw new RuntimeException($"'{instr}': Cannot divide with {a.Type} and {b.Type}.");
        }
        catch (DivideByZeroException)
        {
            throw new RuntimeException($"'{instr}': Cannot divide by zero.");
        }

        CurrentFrame.SetVariable(a.Name, result);
        m_ip++;
    }
    
    /// <summary>
    /// E.g. print 3.141
    /// E.g. print $a
    /// </summary>
    private void ExecutePrint(Instruction instr)
    {
        var a = instr.Operands[0];

        // If the operand is a variable, get its value.
        var toPrint = a;
        if (toPrint.Type == OperandType.Variable)
            toPrint = CurrentFrame.GetVariable(a.Name);

        // Print the value.
        string s;
        if (toPrint.Type == OperandType.Float)
            s = toPrint.FloatValue.ToString(CultureInfo.InvariantCulture);
        else if (toPrint.Type == OperandType.Int)
            s = toPrint.IntValue.ToString();
        else
            throw new RuntimeException($"'{instr}': Cannot print type '{toPrint.Type}'.");

        OutputWritten?.Invoke(this, a.Type == OperandType.Variable ? $"{a.Name} = {s}" : s);
        m_ip++;
    }
    
    /// <summary>
    /// Pops the top-scoped variable frame from the stack.
    /// E.g. pop_frame
    /// </summary>
    private void ExecutePopFrame(Instruction instr)
    {
        if (m_frames.Count == 1)
            throw new RuntimeException($"'{instr}': Cannot pop the last remaining frame.");
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
            throw new RuntimeException($"'{instr}': Integer operand expected.");
        m_frames.Push(new ScopeFrame(CurrentFrame));
        m_callStack.Push(m_ip + 1);
        m_ip = label.IntValue;
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
            throw new RuntimeException($"'{instr}': No procedure to return to.");

        Operand? a = instr.Operands.Length > 0 ? instr.Operands[0] : null;
        if (a.HasValue)
        {
            // If the operand is a variable, get its value.       
            if (a.Value.Type == OperandType.Variable)
                a = CurrentFrame.GetVariable(a.Value.Name);
        }
        
        // Pop the scoped variable frame.
        m_frames.Pop();
        
        // Make the return value available to the caller.
        if (a.HasValue)
            CurrentFrame.DefineVariable("retval", a.Value);
        
        // Restore the IP.
        m_ip = m_callStack.Pop();
    }
}