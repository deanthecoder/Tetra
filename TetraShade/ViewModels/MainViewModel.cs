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
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DTC.Core.ViewModels;
using TetraCore;
using TextCopy;
using Vector = Avalonia.Vector;

namespace TetraShade.ViewModels;

public class MainViewModel : ViewModelBase
{
    public const int PixelWidth = 320;
    public const int PixelHeight = 180;

    private readonly Vector3[] m_rawPixels = new Vector3[PixelWidth * PixelHeight];
    private readonly string[] m_uniforms =
    [
        "fragCoord",
        "iResolution",
        "iTime"
    ];

    private TetraCore.Program m_program;
    private bool m_refreshTaskRunning;
    private WriteableBitmap m_previewImage;
    private double m_time;
    private double m_fps;
    private bool m_isPaused = true;
    private double m_playStartTime;
    private Stopwatch m_playStopwatch;

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
            
            // Allow us to keep track of elapsed time.
            m_playStopwatch = Stopwatch.StartNew();
            
            // Start frame updates.
            RefreshPreviewAsync();
        }
    }
    
    public MainViewModel()
    {
        GenerateShaderCode();
    }

    private void RefreshPreviewAsync()
    {
        if (m_refreshTaskRunning)
            return;

        Task.Run(async () =>
        {
            m_refreshTaskRunning = true;
            try
            {
                long elapsedMs = 0;
                await Task.Run(() =>
                {
                    var s = Stopwatch.StartNew();
                    UpdatePreview();
                    elapsedMs = s.ElapsedMilliseconds;
                });
                    
                Fps = 1000.0 / elapsedMs;
            }
            finally
            {
                m_refreshTaskRunning = false;
                OnFrameCompleted();
            }
        });
    }

    private void OnFrameCompleted()
    {
        if (IsPaused)
            return;
        Time = m_playStartTime + m_playStopwatch.Elapsed.TotalSeconds;
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
    /// Updates the preview image by executing the shader instructions for each pixel.
    /// </summary>
    /// <remarks>
    /// This method:
    /// - Processes each pixel in parallel using the current shader instructions
    /// - Updates the internal pixel buffer with computed color values
    /// - Copies the processed pixels to the preview bitmap
    /// - Triggers a UI refresh via the RefreshPreview event
    /// </remarks>
    public void UpdatePreview()
    {
        var iTime = new Operand((float)Time);
        var iResolution = new Operand(PixelWidth, PixelHeight);

        // Compute single pixel to catch obvious errors.
        ComputePixelColor(new Operand(0, PixelHeight - 0), iResolution, iTime, out var didError);
        if (didError)
            return;
        
        // Process each pixel in parallel.
        Parallel.For(0, PixelWidth * PixelHeight, i =>
        {
            var x = i % PixelWidth;
            var y = i / PixelWidth;

            var fragCoord = new Operand(x, PixelHeight - y);
            m_rawPixels[y * PixelWidth + x] = ComputePixelColor(fragCoord, iResolution, iTime, out _);
        });
        
        BlitPixelsToPreviewImage(m_rawPixels);

        RefreshPreview?.Invoke(this, EventArgs.Empty);
    }

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
            return new Vector3(vm.CurrentFrame.Retval?.Floats);
        }
        catch (Exception)
        {
            didError = true;
            return Vector3.Zero;
        }
    }

    private unsafe void BlitPixelsToPreviewImage(Vector3[] rawPixels)
    {
        using var frameBuffer = PreviewImage.Lock();
        var ptr = new Span<byte>((byte*)frameBuffer.Address, frameBuffer.RowBytes * frameBuffer.Size.Height);
        for (var i = 0; i < rawPixels.Length; i++)
        {
            var v = Vector3.Clamp(rawPixels[i], Vector3.Zero, Vector3.One) * 255.0f;
            ptr[i * 4 + 0] = (byte)v.X;
            ptr[i * 4 + 1] = (byte)v.Y;
            ptr[i * 4 + 2] = (byte)v.Z;
            ptr[i * 4 + 3] = 255;
        }
    }
    
    internal void TogglePause() =>
        IsPaused = !IsPaused;
}