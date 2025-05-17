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
            case OpCode.Halt: return false;
            
            default:
                throw new InvalidOperationException($"Instruction not supported: '{instr}'");
        }
        return true;
    }

    private void ExecuteLd(Instruction instr)
    {
        var variable = instr.Operands[0];
        var value = instr.Operands[1];
        
        CurrentFrame.SetVariable(variable.Name, value);
    }
}