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
using System.Windows.Input;
using Avalonia;
using DTC.Core.Commands;
using DTC.Core.Extensions;
using DTC.Core.ViewModels;
using TetraShade.Views;

namespace TetraShade.ViewModels;

public class AppViewModel : ViewModelBase
{
    public ICommand AboutCommand { get; }

    public AppViewModel()
    {
        var isOpen = false;
        AboutCommand = new RelayCommand(
            _ =>
            {
                if (isOpen)
                    return;
                var dialog = new AboutDialog();
                dialog.Opened += (_, _) => isOpen = true;
                dialog.Closed += (_, _) => isOpen = false;
                
                var window = Application.Current?.GetMainWindow();
                if (window != null)
                    dialog.ShowDialog(window);
            });
    }
}