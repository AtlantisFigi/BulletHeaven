using System;
using System.IO;
using System.Text.Json;

namespace BulletHeaven.Game.Settings;

public sealed class GameSettings
{
    private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public bool IsFullscreen { get; set; } = true;
    public bool ShowFPS { get; set; } = true;
    public bool DamageNumbers { get; set; } = true;
    public float ScreenShake { get; set; } = 1.0f;
    public float MasterVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 0.8f;
    public float MusicVolume { get; set; } = 0.7f;

    public static GameSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var loaded = JsonSerializer.Deserialize<GameSettings>(json);
                if (loaded != null) return loaded;
            }
        }
        catch
        {
        }

        var defaultSettings = new GameSettings();
        defaultSettings.Save();
        return defaultSettings;
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
        }
    }
}

