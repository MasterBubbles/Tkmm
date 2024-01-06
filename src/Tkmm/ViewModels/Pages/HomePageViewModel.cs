﻿using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using System.Text.Json;
using Tkmm.Core;
using Tkmm.Core.Components;
using Tkmm.Core.Models.Mods;

namespace Tkmm.ViewModels.Pages;

public partial class HomePageViewModel : ObservableObject
{
    [ObservableProperty]
    private Mod? _currentMod;

    [RelayCommand]
    private async Task ShowContributors()
    {
        ContentDialog dialog = new() {
            Title = "Contributors",
            Content = new TextBlock {
                Text = $"""
                {string.Join("\n", CurrentMod?.Contributors
                    .Select(x => $"{x.Name}: {string.Join(", ", x.Contributions)}") ?? [])}
                """,
                TextWrapping = TextWrapping.WrapWithOverflow
            },
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = "Dismiss"
        };

        await dialog.ShowAsync();
    }

    [RelayCommand]
    private Task Apply()
    {
        ModManager.Shared.Apply();
        AppStatus.Set("Saved mods profile!", "fa-solid fa-list-check", isWorkingStatus: false, temporaryStatusTime: 1.5);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task MoveUp()
    {
        Move(-1);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task MoveDown()
    {
        Move(1);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task Remove()
    {
        if (CurrentMod is null) {
            return Task.CompletedTask;
        }

        int removeIndex = ModManager.Shared.Mods.IndexOf(CurrentMod);
        ModManager.Shared.Mods.RemoveAt(removeIndex);

        if (ModManager.Shared.Mods.Count == 0) {
            return Task.CompletedTask;
        }

        while (removeIndex >= ModManager.Shared.Mods.Count) {
            removeIndex--;
        }

        CurrentMod = ModManager.Shared.Mods[removeIndex];
        return Task.CompletedTask;
    }

    private void Move(int offset)
    {
        if (CurrentMod is null) {
            return;
        }

        int currentIndex = ModManager.Shared.Mods.IndexOf(CurrentMod);
        int newIndex = currentIndex + offset;

        if (newIndex < 0 || newIndex >= ModManager.Shared.Mods.Count) {
            return;
        }

        Mod store = ModManager.Shared.Mods[newIndex];
        ModManager.Shared.Mods[newIndex] = CurrentMod;
        ModManager.Shared.Mods[currentIndex] = store;

        CurrentMod = ModManager.Shared.Mods[newIndex];
    }

    public HomePageViewModel()
    {
        CurrentMod = ModManager.Shared.Mods.FirstOrDefault();
    }
}
