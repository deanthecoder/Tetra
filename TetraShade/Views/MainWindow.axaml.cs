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

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TetraShade.ViewModels;

namespace TetraShade.Views;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => DataContext as MainViewModel;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnImageControlLoaded(object sender, RoutedEventArgs e)
    {
        var image = (Image)sender;
        
        image.Width = MainViewModel.PixelWidth;
        image.Height = MainViewModel.PixelHeight;

        // Force the image control to redraw.
        ViewModel.RefreshPreview += (_, _) => Dispatcher.UIThread.InvokeAsync(() => image.InvalidateVisual());
        ViewModel.RefreshPreviewAsync();
    }
}