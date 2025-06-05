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

namespace TetraShade.ViewModels;

/// <summary>
/// Represents a single task to render a frame at a specific resolution.
/// </summary>
internal class RenderTask
{
    public bool IsQueueClearer { get; private init; }
    public bool IsQuitter { get; private init; }
    public int BlockSize { get; }
    public Operand Time { get; }
    public Operand Resolution { get; }
    public bool IsLargest => BlockSize == 16;
    public bool CanCancel => !IsLargest;

    public static RenderTask QueueClearer { get; } = new RenderTask { IsQueueClearer = true };
    public static RenderTask Quitter { get; } = new RenderTask { IsQuitter = true };

    public RenderTask(int blockSize, Operand iTime, Operand iResolution)
    {
        BlockSize = blockSize;
        Time = iTime;
        Resolution = iResolution;
    }

    private RenderTask()
    {
    }
}