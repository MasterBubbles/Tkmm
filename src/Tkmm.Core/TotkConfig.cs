﻿using CommunityToolkit.Mvvm.ComponentModel;
using ConfigFactory.Core;
using ConfigFactory.Core.Models;
using System.ComponentModel;
using System.Text.Json.Serialization;
using TotkCommon;

namespace Tkmm.Core;

public sealed partial class TotkConfig : ConfigModule<TotkConfig>
{
    public const string ROMFS = "romfs";
    public const string EXEFS = "exefs";
    public const string CHEATS = "cheats";
    public const string TITLE_ID = "0100F2C0115B6000";

    public static readonly string[] FileSystemFolders = [
        ROMFS,
        EXEFS,
        CHEATS
    ];

    [JsonIgnore]
    public override string Name => "totk";

    [ObservableProperty]
    [property: ConfigFactory.Core.Attributes.Config(
        Header = "Game Path",
        Description = """
            The absolute path to your TotK RomFS game dump
            (e.g. F:\Games\Totk\RomFS)
            """,
        Category = "TotK")]
    [property: ConfigFactory.Core.Attributes.BrowserConfig(
        BrowserMode = ConfigFactory.Core.Attributes.BrowserMode.OpenFolder,
        InstanceBrowserKey = "totk-config-game-path",
        Title = "TotK RomFS Game Path")]
    private string _gamePath = string.Empty;
    public static int GetVersion(string romfsFolder, int @default = 100)
    {
        string regionLangMask = Path.Combine(romfsFolder, "System", "RegionLangMask.txt");
        if (!File.Exists(regionLangMask)) {
            return @default;
        }

        string[] lines = File.ReadAllLines(regionLangMask);
        if (lines.Length >= 3 && int.TryParse(lines[2], out int value)) {
            return value;
        }

        return @default;
    }

    [JsonIgnore]
    public string ZsDicPath => Path.Combine(GamePath, "Pack", "ZsDic.pack.zs");

    [JsonIgnore]
    public int Version => GetVersion(GamePath);

    public TotkConfig()
    {
        FileInfo configFileInfo = new(LocalPath);
        if (configFileInfo is { Exists: true, Length: 0 }) {
            File.Delete(LocalPath);
        }

        OnSaving += () => {
            if (Validate(out string? _, out ConfigProperty? target) == false) {
                AppStatus.Set($"Invalid setting. {target.Property.Name} is invalid.",
                    "fa-solid fa-triangle-exclamation", isWorkingStatus: false);
                return false;
            }

            if (Version == 100) {
                AppStatus.Set($"Version 1.0.0 is not supported by TKMM, please dump TotK v1.1.0 or later.",
                    "fa-solid fa-triangle-exclamation", isWorkingStatus: false);
                return false;
            }

            AppStatus.Reset();
            return true;
        };
    }

    partial void OnGamePathChanged(string value)
    {
        Validate(() => GamePath, gamePathValue => {
            if (Version == 100) {
                return false;
            }

            Totk.Config.GamePath = GamePath;
            if (gamePathValue is null || !File.Exists(Path.Combine(gamePathValue, "Pack", "ZsDic.pack.zs"))) {
                return false;
            }

            Totk.Zstd.LoadDictionaries(ZsDicPath);
            return true;

        });
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (!Config.Shared.AutoSaveSettings) {
            return;
        }

        try {
            Save();
        }
        catch {
            // ignored
        }
    }
}