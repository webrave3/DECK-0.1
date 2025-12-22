using System.Collections.Generic;

public interface IConfigurable
{
    string GetInspectorTitle();
    string GetInspectorStatus();

    // UI Generation
    List<BuildingSetting> GetSettings();
    void OnSettingChanged(string settingId, int newValue);

    // --- NEW: Copy/Paste Support ---
    // Returns all current settings to be stored in clipboard/blueprint
    Dictionary<string, int> GetConfigurationState();

    // Applies settings from clipboard/blueprint
    void SetConfigurationState(Dictionary<string, int> state);
}

// Keep the BuildingSetting class as it was
public class BuildingSetting
{
    public string settingId;
    public string displayName;
    public List<string> options;
    public int currentIndex;
}