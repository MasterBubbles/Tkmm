namespace Tkmm.Abstractions;

public interface ITkProfileMod
{
    ITkMod Mod { get; set; }

    bool IsEnabled { get; set; }

    bool IsEditingOptions { get; set; }
}