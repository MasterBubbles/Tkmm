﻿using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tkmm.Core.Components;
using Tkmm.Core.Generics;
using Tkmm.Core.Helpers;

namespace Tkmm.Core.Models.Mods;

public enum ModOptionGroupType
{
    Multi,
    MultiRequired,
    Single,
    SingleRequired
}

public partial class ModOptionGroup : ObservableObject, IReferenceItem, IModItem
{
    public static readonly ModOptionGroupType[] OptionGroupTypes = Enum.GetValues<ModOptionGroupType>();

    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private ModOptionGroupType _type;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private int? _priority;

    [ObservableProperty]
    private string? _thumbnailUri = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private object? _thumbnail;

    [ObservableProperty]
    private ObservableCollection<ModOptionDependency> _dependencies = [];

    [ObservableProperty]
    private ObservableCollection<Guid> _optionReferences = [];

    [ObservableProperty]
    private ObservableCollection<Guid> _selectedOptionReferences = [];

    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<ModOption> _options = [];

    [JsonIgnore]
    public ObservableCollection<ModOption> SelectedOptions { get; } = [];

    [JsonIgnore]
    public string SourceFolder { get; private set; } = string.Empty;

    private bool _isUpdatingSelection = false;

    public ModOptionGroup()
    {
        Options.CollectionChanged += (_, e)
            => ReferenceCollectionHelper.ResolveCollectionChanged(OptionReferences, e);
        SelectedOptions.CollectionChanged += OnSelectedOptionsChanged;
    }

    private void OnSelectedOptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ReferenceCollectionHelper.ResolveCollectionChanged(SelectedOptionReferences, e);
        if (!_isUpdatingSelection)
        {
            Save();
        }
    }

    private void EnsureSelectionConstraints()
    {
        _isUpdatingSelection = true;

        try
        {
            switch (_type)
            {
                case ModOptionGroupType.MultiRequired:
                case ModOptionGroupType.SingleRequired:
                    if (!SelectedOptions.Any() && Options.Any())
                    {
                        var context = SynchronizationContext.Current;
                        if (context != null)
                        {
                            context.Post(_ =>
                            {
                                if (!SelectedOptions.Any())
                                {
                                    SelectedOptions.Add(Options.First());
                                }
                            }, null);
                        }
                        else
                        {
                            if (!SelectedOptions.Any())
                            {
                                SelectedOptions.Add(Options.First());
                            }
                        }
                    }
                    break;
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    public static ModOptionGroup? FromFolder(string path)
    {
        ModOptionGroup group;

        if (TryGetMetadata(path, out string metadataPath)) {
            try {
                using FileStream fs = File.OpenRead(metadataPath);
                group = JsonSerializer.Deserialize<ModOptionGroup>(fs)!;
            }
            catch (Exception ex) {
                AppLog.Log(ex);
                return null;
            }
        }
        else {
            group = new() {
                Name = Path.GetFileName(path),
                Id = Guid.NewGuid()
            };
        }

        group.SourceFolder = path;

        foreach (string folder in Directory.EnumerateDirectories(path)) {
            if (ModOption.FromFolder(folder) is not ModOption option) {
                continue;
            }

            group.Options.Add(option);
            
            if (group.SelectedOptionReferences.Contains(option.Id)) {
                group.SelectedOptions.Add(option);
            }
        }

        group.Options = [.. group.Options.OrderBy(x => x.Name)];
        return group;
    }

    public void Save()
    {
        EnsureSelectionConstraints();
        string metadata = Path.Combine(SourceFolder, PackageBuilder.METADATA);
        using FileStream fs = File.Create(metadata);
        JsonSerializer.Serialize(fs, this);
    }

    private static bool TryGetMetadata(string path, out string metadataPath)
    {
        metadataPath = Path.Combine(path, "info.json");
        if (!File.Exists(metadataPath)) {
            return false;
        }

        return true;
    }

    async partial void OnThumbnailUriChanged(string? value)
    {
        if (Mod.ResolveThumbnail?.Invoke(this, false) is Task task) {
            await task;
        }
    }
}
