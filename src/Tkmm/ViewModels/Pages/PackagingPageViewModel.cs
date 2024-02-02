﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConfigFactory.Avalonia.Helpers;
using ConfigFactory.Core.Attributes;
using System.IO.Compression;
using System.Text.Json;
using Tkmm.Core;
using Tkmm.Core.Components;
using Tkmm.Core.Models.Mods;

namespace Tkmm.ViewModels.Pages;

public partial class PackagingPageViewModel : ObservableObject
{
    private string? _previousSourceFolder = null;

    [ObservableProperty]
    private string _exportPath = string.Empty;

    [ObservableProperty]
    private Mod _mod = new();

    [RelayCommand]
    private async Task BrowseExportPath()
    {
        BrowserDialog dialog = new(BrowserMode.SaveFile, "Export Location", "TKCL Files:*.tkcl");
        if (await dialog.ShowDialog() is string result) {
            ExportPath = result;
        }
    }

    [RelayCommand]
    private async Task BrowseSourceFolder()
    {
        BrowserDialog dialog = new(BrowserMode.OpenFolder, "Source Folder");
        if (await dialog.ShowDialog() is string result) {
            Mod.SourceFolder = result;
        }
    }

    [RelayCommand]
    private async Task BrowseThumbnail()
    {
        BrowserDialog dialog = new(BrowserMode.OpenFile, "Thumbnail", "Image Files:*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif");
        if (await dialog.ShowDialog() is string result) {
            Mod.ThumbnailUri = result;
        }
    }

    [RelayCommand]
    private async Task Create()
    {
        if (string.IsNullOrEmpty(Mod.Name)) {
            Mod.Name = Path.GetFileName(Mod.SourceFolder);
        }

        if (string.IsNullOrEmpty(Mod.SourceFolder)) {
            App.Toast("Packaging requires a mod to package. Please provide a mod folder.");

            return;
        }

        if (string.IsNullOrEmpty(ExportPath)) {
            ExportPath = Path.Combine(Path.GetDirectoryName(Mod.SourceFolder) ?? string.Empty, $"{Path.GetFileName(Mod.SourceFolder)}.tkcl");
        }

        string tmpOutput = Path.Combine(Path.GetTempPath(), "tkmm", Mod.Id.ToString());

        await Task.Run(async () => {
            CheckId();
            PackageBuilder.CreateMetaData(Mod, tmpOutput);
            await PackageBuilder.CopyContents(Mod, tmpOutput);
            PackageBuilder.Package(tmpOutput, ExportPath);

            Directory.Delete(tmpOutput, true);
        });
    }

    [RelayCommand]
    private async Task ImportInfo()
    {
        BrowserDialog dialog = new(BrowserMode.OpenFile, "Import Mod Info", "JSON Metadata:info.json|Mod Archive:*.tkcl");
        if (await dialog.ShowDialog() is string result) {
            Stream? stream;
            if (result.EndsWith("info.json")) {
                stream = File.OpenRead(result);
            }
            else {
                ZipArchive archive = ZipFile.OpenRead(result);
                stream = archive.Entries.FirstOrDefault(x => x.Name == "info.json")?.Open();
            }

            if (stream is null) {
                AppStatus.Set("Could not read mod metadata!", "fa-solid fa-triangle-exclamation", temporaryStatusTime: 1.5, isWorkingStatus: false);
                return;
            }

            Mod = JsonSerializer.Deserialize<Mod>(stream)
                ?? throw new InvalidOperationException("""
                    Error parsing metadata: The JSON deserializer returned null.
                    """);

            await stream.DisposeAsync();
        }
    }

    [RelayCommand]
    private async Task ExportMetadata()
    {
        BrowserDialog dialog = new(BrowserMode.OpenFolder, "Export Mod Metadata");
        if (await dialog.ShowDialog() is string result) {
            CheckId();
            PackageBuilder.CreateMetaData(Mod, result);
            AppStatus.Set("Exported metadata!", "fa-solid fa-circle-check", temporaryStatusTime: 1.5, isWorkingStatus: false);
        }
    }

    [RelayCommand]
    private Task WriteMetadata()
    {
        if (!string.IsNullOrEmpty(Mod.SourceFolder) && Directory.Exists(Mod.SourceFolder)) {
            CheckId();
            PackageBuilder.CreateMetaData(Mod, Mod.SourceFolder);
            AppStatus.Set("Exported metadata!", "fa-solid fa-circle-check", temporaryStatusTime: 1.5, isWorkingStatus: false);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ImportOptionGroup()
    {
        BrowserDialog dialog = new(BrowserMode.OpenFolder, "Import Mod Option Group");
        if (await dialog.ShowDialog() is string result) {
            Mod.OptionGroups.Add(ModOptionGroup.FromFolder(result));
        }
    }

    [RelayCommand]
    private Task RefreshOptions()
    {
        string store = Mod.SourceFolder;
        Mod.SourceFolder = string.Empty;
        Mod.SourceFolder = store;
        return Task.CompletedTask;
    }

    private void CheckId()
    {
        if (_previousSourceFolder is null || Mod.SourceFolder != _previousSourceFolder) {
            Console.WriteLine("SET GUID");
            _previousSourceFolder = Mod.SourceFolder;
            Mod.Id = Guid.NewGuid();
        }
    }
}