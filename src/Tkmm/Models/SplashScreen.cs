﻿using Avalonia.Media;
using FluentAvalonia.UI.Windowing;
using Tkmm.Core.Components;
using Tkmm.Helpers;
using Tkmm.Helpers.Models;
using Tkmm.Views.Common;

namespace Tkmm.Models;

public class SplashScreen : IApplicationSplashScreen
{
    public string? AppName { get; }
    public IImage? AppIcon { get; }
    public object SplashScreenContent { get; } = new SplashScreenView();

    public int MinimumShowTime { get; } = 1500;

    public async Task RunTasks(CancellationToken cancellationToken)
    {
        List<Task> tasks = [];

        foreach (var mod in ProfileManager.Shared.Mods) {
            tasks.Add(ModHelper.ResolveThumbnail(mod));
        }

        await Task.WhenAll(tasks);

        PageModel? store = PageManager.Shared.Current;
        PageManager.Shared.Focus(Page.Settings);
        PageManager.Shared.Current = store;
    }
}
