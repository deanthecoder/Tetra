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
using System.Numerics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DTC.Core.ViewModels;
using Vector = Avalonia.Vector;

namespace TetraShade.ViewModels;

public class MainViewModel : ViewModelBase
{
    public const int PixelWidth = 360;
    public const int PixelHeight = 180;
    
    private readonly Vector3[] m_rawPixels;
    private WriteableBitmap m_previewImage;
    private double m_time;
    
    public event EventHandler RefreshPreview;

    public WriteableBitmap PreviewImage => m_previewImage ??= new WriteableBitmap(new PixelSize(PixelWidth, PixelHeight), new Vector(96, 96), PixelFormat.Rgb32);

    public double Time
    {
        get => m_time;
        set
        {
            if (SetField(ref m_time, value))
                UpdatePreview();
        }
    }

    public MainViewModel()
    {
        m_rawPixels = new Vector3[PixelWidth * PixelHeight];
        for (var y = 0; y < PixelHeight; y++)
        for (var x = 0; x < PixelWidth; x++)
        {
            var pixel = new Vector3((float)x / PixelWidth, (float)y / PixelHeight, 0.0f);
            m_rawPixels[y * PixelWidth + x] = pixel;
        }

        UpdatePreview();
    }

    public unsafe void UpdatePreview()
    {
        using var frameBuffer = PreviewImage.Lock();
        var ptr = new Span<byte>((byte*)frameBuffer.Address, frameBuffer.RowBytes * frameBuffer.Size.Height);

        for (var i = 0; i < m_rawPixels.Length; i++)
        {
            Vector3 v = Vector3.Clamp(m_rawPixels[i], Vector3.Zero, Vector3.One) * 255.0f;
            ptr[i * 4 + 0] = (byte)v.X;
            ptr[i * 4 + 1] = (byte)v.Y;
            ptr[i * 4 + 2] = (byte)v.Z;
            ptr[i * 4 + 3] = 255;
        }

        RefreshPreview?.Invoke(this, EventArgs.Empty);
    }
}