// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Settings;

public static class InternalSettings
{
    private static readonly SettingsProfile profile;

    public static SettingsContainer SettingsContainer { get; } = new();

    public static readonly SettingsKey<MRUDictionary> MostRecentlyUsedSessions = new("Internal/MostRecentlyUsedSessions", SettingsContainer, () => new MRUDictionary());

    static InternalSettings()
    {
        profile = LoadProfile(true);
        SettingsContainer.CurrentProfile = profile;
    }

    /// <summary>
    /// Loads a copy of the internal settings from the file.
    /// </summary>
    public static SettingsProfile LoadProfileCopy()
    {
        return LoadProfile(false);
    }

    /// <summary>
    /// Loads the settings from the file.
    /// </summary>
    private static SettingsProfile LoadProfile(bool registerProfile)
    {
        return SettingsContainer.LoadSettingsProfile(EditorPath.InternalConfigPath, false, null, registerProfile) ?? SettingsContainer.CreateSettingsProfile(false);
    }

    /// <summary>
    /// Saves the settings into the settings file.
    /// </summary>
    public static void SaveProfile()
    {
        // Special case for MRU: we always reload the latest version from the file.
        // Actually modifying and saving MRU is done in a specific class.
        var profileCopy = LoadProfileCopy();
        var mruList = MostRecentlyUsedSessions.GetValue(profileCopy, true);
        MostRecentlyUsedSessions.SetValue(mruList);
        SettingsContainer.SaveSettingsProfile(profile, EditorPath.InternalConfigPath);
    }
}
