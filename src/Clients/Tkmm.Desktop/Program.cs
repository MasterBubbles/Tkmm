﻿using Avalonia;
using Microsoft.Extensions.Logging;
using Ninject;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Tkmm.Components;
using Tkmm.Core;
using Tkmm.Core.GameBanana.Modules;
using Tkmm.Core.Modules;
using Tkmm.IO.Desktop.Modules;

namespace Tkmm.Desktop;

internal abstract class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        TKMM.DI.Load<DesktopModule>();
        TKMM.DI.Load<TkModule>();
        TKMM.DI.Load<GameBananaModule>();

        try {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) {
            TKMM.Logger.LogError(ex, "An unhandled exception of type '{ErrorType}' occured.", ex.GetType());
#if DEBUG
            throw;
#endif
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register(new FontAwesomeIconProvider(FontAwesomeJsonStreamProvider.Instance));

#if DEBUG
        // if (OperatingSystem.IsWindows() && Config.Shared.ShowConsole == false) {
        //     Core.Helpers.Windows.WindowsOperations.SetWindowMode(Core.Helpers.Windows.WindowMode.Hidden);
        // }
#endif

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();
    }
}