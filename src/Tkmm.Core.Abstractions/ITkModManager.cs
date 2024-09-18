namespace Tkmm.Core.Abstractions;

public interface ITkModManager
{
    /// <summary>
    /// The current profile in use by the frontend.
    /// </summary>
    ITkProfile? CurrentProfile { get; set; }

    /// <summary>
    /// The collection of custom profiles.
    /// </summary>
    IList<ITkProfile> Profiles { get; }

    /// <summary>
    /// The master collection of installed mods.
    /// </summary>
    IList<ITkMod> Mods { get; }

    /// <summary>
    /// Imports the provided <paramref name="mod"/> into the <see cref="CurrentProfile"/>.
    /// </summary>
    /// <param name="mod">The <see cref="ITkMod"/> to import.</param>
    virtual ValueTask Import(ITkMod mod) => Import(mod, CurrentProfile);

    /// <summary>
    /// Imports the provided <paramref name="mod"/> into the provided <paramref name="profile"/>.
    /// </summary>
    /// <param name="mod">The <see cref="ITkMod"/> to import.</param>
    /// <param name="profile"></param>
    virtual ValueTask Import(ITkMod mod, ITkProfile profile)
    {
        profile.Mods.Add(mod.GetProfileMod());
        Mods.Add(mod);
        return ValueTask.CompletedTask;
    }

    ValueTask InitializeAsync();
}