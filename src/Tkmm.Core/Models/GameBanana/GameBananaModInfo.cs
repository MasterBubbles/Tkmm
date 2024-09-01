﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json.Serialization;

namespace Tkmm.Core.Models.GameBanana;

public partial class GameBananaModInfo : ObservableObject
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_bHasContentRatings")]
    public bool IsContentRated { get; set; }

    [JsonPropertyName("_bIsObsolete")]
    public bool IsObsolete { get; set; }

    [JsonPropertyName("_sProfileUrl")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("_aPreviewMedia")]
    public GameBananaMedia Media { get; set; } = new();

    [JsonPropertyName("_aSubmitter")]
    public GameBananaSubmitter Submitter { get; set; } = new();

    [JsonPropertyName("_sVersion")]
    public string Version { get; set; } = string.Empty;

    [ObservableProperty]
    private object? _thumbnail;

    [JsonIgnore]
    public GameBananaMod? Full { get; set; } = new();

    [RelayCommand]
    public async Task FetchMetadata()
    {
        Full = await GameBananaMod.DownloadMetaData(Id.ToString());
    }
}
