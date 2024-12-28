using System.Text;
using Avalonia.Platform.Storage;
using LibHac.Common.Keys;
using LibHac.Tools.Fs;
using Tkmm.Core;
using Tkmm.Dialogs;
using Tkmm.Helpers;

namespace Tkmm.Wizard.Actions;

public static class WizActions
{
    private static readonly FilePickerFileType _exe = new("Executable") {
        Patterns = [
            OperatingSystem.IsWindows() ? "*.exe" : "*"
        ]
    };

    public static async ValueTask<(bool, int?)> SetupOtherEmulator()
    {
        string? emulatorFilePath = await App.XamlRoot.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title = "Select emulator executable",
            AllowMultiple = false,
            FileTypeFilter = [
                _exe
            ]
        }) switch {
            [IStorageFile target] => target.TryGetLocalPath(),
            _ => null
        };

        if (emulatorFilePath is null) {
            return (false, null);
        }

        return await StartEmulatorSetup(emulatorFilePath);
    }

    public static async ValueTask<(bool, int?)> StartRyujinxSetup()
    {
        if (RyujinxHelper.GetRyujinxDataFolder(out string? ryujinxExeFilePath) is not string ryujinxDataFolder || ryujinxExeFilePath is null) {
            await MessageDialog.Show(
                "Ryujinx is not running or could not be identified. Please ensure Ryujinx is running.",
                "Setup Error");
            return (false, null);
        }

        if (RyujinxHelper.GetKeys(ryujinxDataFolder, out string ryujinxKeysFolder) is not KeySet keys) {
            await MessageDialog.Show(
                "The required keys could not be found in your Ryujinx installation.",
                "Setup Error");
            return (false, null);
        }

        (string FilePath, string Version)[] tkFiles = RyujinxHelper.GetTotkFiles(ryujinxDataFolder, keys).ToArray();

        if (tkFiles.Length == 0) {
            await MessageDialog.Show(
                "Tears of the Kingdom could not be found in your Ryujinx installation.",
                "Setup Error");
            return (false, null);
        }

        if (tkFiles[0].Version == "1.0.0" && tkFiles.Length == 1) {
            await MessageDialog.Show(
                "Tears of the Kingdom was found, but no update is installed. Please install TotK v1.1.0 or later in Ryujinx and try again.",
                "Setup Error");
            return (false, null);
        }

        if (tkFiles.FirstOrDefault(x => x.Version == "1.0.0") is not (FilePath: string, Version: string) baseGameFilePath) {
            await MessageDialog.Show(
                "Tears of the Kingdom updates were was found, but the base game file was not. Please install TotK in Ryujinx and try again.",
                "Setup Error");
            return (false, null);
        }

        Config.Shared.EmulatorPath = ryujinxExeFilePath;

        TkConfig.Shared.KeysFolderPath = ryujinxKeysFolder;
        TkConfig.Shared.BaseGameFilePath = baseGameFilePath.FilePath;
        TkConfig.Shared.GameUpdateFilePath = tkFiles
            .OrderBy(x => x.Version)
            .Last()
            .FilePath;

        return (true, null);
    }

    public static async ValueTask<(bool, int?)> StartEmulatorSetup(string emulatorFilePath)
    {
        Config.Shared.EmulatorPath = emulatorFilePath;

        string emulatorName = Path.GetFileNameWithoutExtension(emulatorFilePath);
        string emulatorDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), emulatorName);

        if (!Directory.Exists(emulatorDataFolder)) {
            return (true, null);
        }

        if (EmulatorHelper.GetKeys(emulatorDataFolder, out string emulatorKeysFolder) is not KeySet keys) {
            await MessageDialog.Show(
                $"The required keys could not be found in your {emulatorName} installation.",
                "Setup Error");
            return (false, null);
        }

        TkConfig.Shared.KeysFolderPath = emulatorKeysFolder;

        (string FilePath, string Version)[] tkFiles = EmulatorHelper.GetTotkFiles(emulatorName, emulatorDataFolder, keys).ToArray();

        if (tkFiles.Length == 0) {
            return (true, null);
        }

        if (tkFiles is [(FilePath: string, Version: string) target]) {
            if (target.Version == "1.0.0") {
                TkConfig.Shared.BaseGameFilePath = tkFiles[0].FilePath;
            }
            else {
                TkConfig.Shared.GameUpdateFilePath = tkFiles[0].FilePath;
            }

            return (true, null);
        }

        TkConfig.Shared.GameUpdateFilePath = tkFiles
            .OrderBy(x => x.Version)
            .Last()
            .FilePath;

        if (tkFiles.FirstOrDefault(x => x.Version == "1.0.0") is not (FilePath: string, Version: string) baseGameFilePath) {
            return (true, null);
        }

        TkConfig.Shared.BaseGameFilePath = baseGameFilePath.FilePath;

        return (true, null);
    }

    public static async ValueTask<(bool, int?)> VerifyConfig()
    {
        StringBuilder reasons = new();

        if (string.IsNullOrWhiteSpace(TkConfig.Shared.GameDumpFolderPath)) {
            reasons.AppendLine("[RomFS Dump] The game dump folder path is empty.");
            goto UseNspXci;
        }

        if (TkRomHelper.GetVersionFromRomfs(TkConfig.Shared.GameDumpFolderPath) is not int version) {
            reasons.AppendLine("[RomFS Dump] Invalid game dump folder path.");
            goto UseNspXci;
        }

        if (version == 100) {
            reasons.AppendLine("[RomFS Dump] Invalid game dump version (1.0.0), TKMM requires version 1.1.0 or later.");
        }

    UseNspXci:
        if (TkConfig.Shared.KeysFolderPath is not string keysFolderPath || TkRomHelper.GetKeys(keysFolderPath) is not KeySet keys) {
            reasons.AppendLine("[XCI/NSP] The keys folder is invalid.");
            goto Fail;
        }

        if (TkConfig.Shared.BaseGameFilePath is not string baseGameFilePath ||
            !TkRomHelper.IsTotkRomFile(baseGameFilePath, keys, out Application? app) || app.DisplayVersion != "1.0.0") {
            reasons.AppendLine("[XCI/NSP] The base game file is invalid.");
            goto Fail;
        }

        if (TkConfig.Shared.GameUpdateFilePath is not string gameUpdateFilePath ||
            !TkRomHelper.IsTotkRomFile(gameUpdateFilePath, keys, out Application? update) || update.DisplayVersion == "1.0.0") {
            reasons.AppendLine("[XCI/NSP] The game update file is invalid.");
            goto Fail;
        }

        return (true, null);

    Fail:
        await MessageDialog.Show(reasons, "Configuration Errors");
        return (false, null);
    }
}