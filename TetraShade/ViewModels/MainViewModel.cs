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

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DTC.Core;
using DTC.Core.ViewModels;
using TetraCore;
using TextCopy;
using Vector = Avalonia.Vector;

namespace TetraShade.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    public const int PixelWidth = 320;
    public const int PixelHeight = 180;

    private readonly Vector3[,] m_rawPixels = new Vector3[PixelHeight, PixelWidth];
    private readonly bool[,] m_computed = new bool[PixelHeight, PixelWidth];
    private readonly Stopwatch m_frameTimer = new Stopwatch();
    private readonly BlockingCollection<RenderTask> m_renderQueue = new BlockingCollection<RenderTask>();
    private readonly AutoResetEvent m_queueClearedEvent = new AutoResetEvent(false);
    private CancellationTokenSource m_renderCts = new();
    private readonly string[] m_uniforms =
    [
        "fragCoord",
        "iResolution",
        "iTime"
    ];

    private TetraCore.Program m_program;
    private WriteableBitmap m_previewImage;
    private double m_time;
    private double m_fps;
    private bool m_isPaused = true;
    private double m_playStartTime;
    private Stopwatch m_playStopwatch;
    private Task m_renderLoopTask;

    /// <summary>
    /// Raised when the preview image needs to be updated in the UI.
    /// </summary>
    public event EventHandler RefreshPreview;

    /// <summary>
    /// Gets the bitmap used for displaying the preview image in the UI.
    /// </summary>
    public WriteableBitmap PreviewImage =>
        m_previewImage ??= new WriteableBitmap(new PixelSize(PixelWidth, PixelHeight), new Vector(96, 96), PixelFormat.Rgb32);

    /// <summary>
    /// Get or set the current time in seconds.
    /// </summary>
    public double Time
    {
        get => m_time;
        set
        {
            if (SetField(ref m_time, value))
                RefreshPreviewAsync();
        }
    }

    /// <summary>
    /// Gets the current FPS.
    /// </summary>
    public double Fps
    {
        get => m_fps;
        private set => SetField(ref m_fps, value);
    }

    public bool IsPaused
    {
        get => m_isPaused;
        set
        {
            if (!SetField(ref m_isPaused, value))
                return;
            
            // Record when playing started.
            m_playStartTime = Time;
            
            // Allow us to keep track of elapsed time.§
            m_playStopwatch = Stopwatch.StartNew();
            
            // Start frame updates.
            RefreshPreviewAsync();
        }
    }
    
    public MainViewModel()
    {
        GenerateShaderCode();
    }

    private void GenerateShaderCode()
    {
        var assembler = new Assembler();
        m_program = assembler.Assemble(
            """
                call main
                halt
            main:
                ld $uv, $fragCoord
                div $uv, $iResolution
                ld $theta, 0.0, 2.0, 4.0
                add $theta, $uv[0], $uv[1], $uv[0]
                add $theta, $iTime
                cos $col, $theta
                mul $col, 0.5
                add $col, 0.5
                ret $col
            """,
            m_uniforms);
    }

    internal void ImportFromClipboard()
    {
        var code = ClipboardService.GetText();
        if (string.IsNullOrWhiteSpace(code))
            return; // Nothing to do.

        try
        {
            var assembler = new Assembler();
            m_program = assembler.Assemble(code, m_uniforms);
            Time = 0.0;
            RefreshPreviewAsync();
        }
        catch
        {
            // Import failed.
        }
    }

    /// <summary>
    /// Starts or restarts the progressive rendering workflow.
    /// Queues up render tasks at different block sizes for the preview image.
    /// </summary>
    public void RefreshPreviewAsync()
    {
        m_renderLoopTask ??= Task.Run(RenderLoop);

        // Cancel any current rendering.
        m_renderCts.Cancel();

        // Remove any queued rendering requests.
        m_renderQueue.Add(RenderTask.QueueClearer);
        m_queueClearedEvent.WaitOne();

        m_renderCts.Dispose();
        m_renderCts = new CancellationTokenSource();

        // Queue new renders.
        var iTime = new Operand((float)Time);
        var iResolution = new Operand(PixelWidth, PixelHeight);
        m_renderQueue.Add(new RenderTask(16, iTime, iResolution));
        m_renderQueue.Add(new RenderTask(8, iTime, iResolution));
        m_renderQueue.Add(new RenderTask(4, iTime, iResolution));
        m_renderQueue.Add(new RenderTask(2, iTime, iResolution));
        m_renderQueue.Add(new RenderTask(1, iTime, iResolution));
    }

    /// <summary>
    /// Worker loop that processes render tasks from the queue.
    /// Handles queue clearing, quitting, and invokes rendering passes as needed.
    /// </summary>
    private void RenderLoop()
    {
        foreach (var renderTask in m_renderQueue.GetConsumingEnumerable())
        {
            if (renderTask.IsQuitter)
            {
                m_renderQueue.CompleteAdding();
                return;
            }
            if (renderTask.IsQueueClearer)
            {
                while (m_renderQueue.TryTake(out _))
                {
                    // Drain remaining items
                }

                m_queueClearedEvent.Set();
                continue;
            }

            // Compute single pixel to catch obvious errors.
            ComputePixelColor(new Operand(0, 0), renderTask.Resolution, renderTask.Time, out var didError);

            if (renderTask.IsLargest || didError)
            {
                m_frameTimer.Restart();

                // Reset computed flags.
                Array.Clear(m_computed, 0, m_computed.Length);
                Array.Clear(m_rawPixels, 0, m_rawPixels.Length);
            }

            if (!didError)
                RenderPass(renderTask);
            
            if (m_renderCts.IsCancellationRequested && renderTask.CanCancel)
                continue;
            
            BlitPixelsToPreviewImage(m_rawPixels, renderTask.BlockSize);
            RefreshPreview?.Invoke(this, EventArgs.Empty);

            if (renderTask.BlockSize == 1)
            {
                // Frame rendering complete (at highest resolution).
                Fps = 1000.0 / Math.Max(1, m_frameTimer.ElapsedMilliseconds);

                if (!IsPaused)
                {
                    Dispatcher.UIThread.InvokeAsync(() => Time = m_playStartTime + m_playStopwatch.Elapsed.TotalSeconds);
                }
            }
        }
    }
    
    /// <summary>
    /// Executes rendering of a pixel grid at the given block size.
    /// Each block is rendered in parallel, computing pixel colors as needed.
    /// </summary>
    private void RenderPass(RenderTask renderTask)
    {
        var blockSize = renderTask.BlockSize;
        var blocksWide = (PixelWidth + blockSize - 1) / blockSize;
        var blocksHigh = (PixelHeight + blockSize - 1) / blockSize;

        Parallel.For(0, blocksWide * blocksHigh, i =>
        {
            var x = (i % blocksWide) * blockSize;
            var y = (i / blocksWide) * blockSize;

            if (x >= PixelWidth || y >= PixelHeight || m_computed[y, x])
                return;

            m_computed[y, x] = true;

            if (renderTask.CanCancel && m_renderCts.IsCancellationRequested)
            {
                m_rawPixels[y, x] = Vector3.One;
                return;
            }

            m_rawPixels[y, x] = ComputePixelColor(new Operand(x, PixelHeight - y), renderTask.Resolution, renderTask.Time, out _);
        });
    }
    
    /// <summary>
    /// Executes the Tetra VM to compute the color of a single pixel.
    /// Returns the color as a Vector3, and sets didError if computation fails.
    /// </summary>
    private Vector3 ComputePixelColor(Operand fragCoord, Operand iResolution, Operand iTime, out bool didError)
    {
        didError = false;
        
        // Run the shader for this pixel.
        var vm = new TetraVm(m_program);
        vm.AddUniform("fragCoord", fragCoord);
        vm.AddUniform("iResolution", iResolution);
        vm.AddUniform("iTime", iTime);
            
        try
        {
            vm.Run();

            if (vm.CurrentFrame.Retval == null)
                Logger.Instance.Error("No return value from shader.");
            else if (vm.CurrentFrame.Retval.Length < 3)
                Logger.Instance.Error($"Shader returned unexpected value ({vm.CurrentFrame.Retval}).");
            else
            {
                var rgba = new Vector3(vm.CurrentFrame.Retval?.Floats);
                return Vector3.Clamp(rgba, Vector3.Zero, Vector3.One) * 255.0f;
            }
        }
        catch
        {
            // Fall through.
        }
        
        didError = true;
        return Vector3.Zero;
    }

    /// <summary>
    /// Writes rendered pixel data into the preview bitmap.
    /// Updates the preview image by copying colors from the raw pixel array.
    /// </summary>
    private unsafe void BlitPixelsToPreviewImage(Vector3[,] rawPixels, int blockSize)
    {
        using var frameBuffer = PreviewImage.Lock();
        var ptr = new Span<byte>((byte*)frameBuffer.Address, frameBuffer.RowBytes * frameBuffer.Size.Height);
        
        for (var y = 0; y < PixelHeight; y += blockSize)
        {
            for (var x = 0; x < PixelWidth; x += blockSize)
            {
                // Clamp block in case width/height not divisible by blockSize
                var blockWidth = Math.Min(blockSize, PixelWidth - x);
                var blockHeight = Math.Min(blockSize, PixelHeight - y);

                var color = rawPixels[y, x];

                for (var dy = 0; dy < blockHeight; dy++)
                {
                    for (var dx = 0; dx < blockWidth; dx++)
                    {
                        var px = x + dx;
                        var py = y + dy;
                        var offset = (py * PixelWidth + px) * 4;

                        ptr[offset + 0] = (byte)color.X;
                        ptr[offset + 1] = (byte)color.Y;
                        ptr[offset + 2] = (byte)color.Z;
                        ptr[offset + 3] = 255;
                    }
                }
            }
        }
    }
    
    internal void TogglePause() =>
        IsPaused = !IsPaused;

    public void Dispose()
    {
        m_renderCts.Cancel();
        m_renderQueue.Add(RenderTask.Quitter);
        m_renderQueue.CompleteAdding();
        m_renderLoopTask?.Wait();
        m_renderLoopTask?.Dispose();
        m_renderQueue.Dispose();
        m_queueClearedEvent.Dispose();
        m_renderCts.Dispose();
    }
}