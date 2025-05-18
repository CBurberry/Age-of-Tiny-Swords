using System;

public interface IGameSettings
{
    event Action OnSettingsSaved;
    event Action OnSettingsLoaded;

    void SaveSettings();
    void LoadSettings();
}
