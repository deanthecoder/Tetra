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
    private readonly Stack<ScopeFrame> m_frames = new();
    private int m_ip;

    public ScopeFrame CurrentFrame => m_frames.Peek();

    public TetraVm(Instruction[] instructions)
    {
        m_instructions = instructions ?? throw new ArgumentNullException(nameof(instructions));
    }

    public void Run()
    {
        const int maxInstructionExecutions = 10_000;

        // Reset the VM.
        m_frames.Clear();
        m_frames.Push(new ScopeFrame()); // Global scope

        // Execute the instructions.
        var instructionExecutions = 0;
        m_ip = 0;
        while (m_ip < m_instructions.Length)
        {
            var instr = m_instructions[m_ip];
            var keepRunning = Execute(instr);
            if (!keepRunning)
                break;
            m_ip++;
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
            case OpCode.Halt: return false;
            
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
        
        CurrentFrame.SetVariable(variable.Name, value);
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

        // If the value is a variable, get its value.
        if (value.Type == OperandType.Variable)
            value = CurrentFrame.GetVariable(value.Name);

        var current = CurrentFrame.GetVariable(variableName);
        Operand? result;
        if (current.Type == OperandType.Float || value.Type == OperandType.Float)
            result = new Operand(current.AsFloat() + value.AsFloat());
        else if (current.Type == OperandType.Int && value.Type == OperandType.Int)
            result = new Operand(current.IntValue + value.IntValue);
        else
            throw new RuntimeException($"'{instr}': Cannot add with {variable.Type} and {value.Type}.");

        CurrentFrame.SetVariable(variableName, result.Value);
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
        
        // If the value is a variable, get its value.
        if (value.Type == OperandType.Variable)
            value = CurrentFrame.GetVariable(value.Name);

        var current = CurrentFrame.GetVariable(variableName);
        Operand? result;
        if (current.Type == OperandType.Float || value.Type == OperandType.Float)
            result = new Operand(current.AsFloat() - value.AsFloat());
        else if (current.Type == OperandType.Int && value.Type == OperandType.Int)
            result = new Operand(current.IntValue - value.IntValue);
        else
            throw new RuntimeException($"'{instr}': Cannot subtract with {variable.Type} and {value.Type}.");

        CurrentFrame.SetVariable(variableName, result.Value);
    }
    
    /// <summary>
    /// E.g. inc $a
    /// </summary>
    private void ExecuteInc(Instruction instr)
    {
        var variable = instr.Operands[0];
        var variableName = variable.Name;
        
        var current = CurrentFrame.GetVariable(variableName);
        Operand? result = current.Type switch
        {
            OperandType.Float => new Operand(current.FloatValue + 1.0f),
            OperandType.Int => new Operand(current.IntValue + 1),
            _ => throw new RuntimeException($"'{instr}': Cannot increment {variable.Type}.")
        };

        CurrentFrame.SetVariable(variableName, result.Value);
    }
    
    /// <summary>
    /// E.g. dec $a
    /// </summary>
    private void ExecuteDec(Instruction instr)
    {
        var variable = instr.Operands[0];
        var variableName = variable.Name;
        
        var current = CurrentFrame.GetVariable(variableName);
        Operand? result = current.Type switch
        {
            OperandType.Float => new Operand(current.AsFloat() - 1.0f),
            OperandType.Int => new Operand(current.IntValue - 1),
            _ => throw new RuntimeException($"'{instr}': Cannot decrement {variable.Type}.")
        };

        CurrentFrame.SetVariable(variableName, result.Value);
    }
}