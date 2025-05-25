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

using Avalonia;
using System;
using System.Threading.Tasks;
using DTC.Core;
using TetraShade.Views;

namespace TetraShade;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        
        try
        {
            Logger.Instance.SysInfo();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Logger.Instance.Info("Application ended cleanly.");
        }
        catch (Exception ex)
        {
            HandleFatalException(ex);
        }
        // finally
        // {
        //    Settings.Instance.Save();
        // }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            HandleFatalException(ex);
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        Logger.Instance.Exception("Unobserved task exception.", e.Exception);
    }

    private static void HandleFatalException(Exception ex) =>
        Logger.Instance.Exception("A fatal error occurred.", ex);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}