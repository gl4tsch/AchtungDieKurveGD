
using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace ADK
{
    public class Settings
    {
        static readonly string settingsFileName = "settings.cfg";
        static readonly string settingsFilePathRelative = $"user://{settingsFileName}";
        static readonly string settingsFilePathAbsolute = Path.Combine(OS.GetUserDataDir(), settingsFileName);
        ConfigFile config = new();

        public string AudioSectionName => "Audio";
        public string ArenaSectionName => "Arena";
        public string SnakeSectionName => "Snake";
        public string AbilitySectionName => "Abilities";

        public GraphicsSettings GraphicsSettings { get; private set; }
        public SettingsSection AudioSettings { get; private set; }
        public SettingsSection ArenaSettings { get; private set; }
        public SettingsSection SnakeSettings { get; private set; }
        public SettingsSection AbilitySettings { get; private set; }

        /// <summary>
        /// loads settings from settings.cfg file
        /// </summary>
        public void LoadSettings()
        {
            var error = config.Load(settingsFilePathRelative);
            GD.Print($"Settings File Loading: {error}");

            GraphicsSettings = new();
            GraphicsSettings.LoadFromConfig(config);
            AudioSettings = new(AudioSectionName, AudioManager.DefaultSettings);
            AudioSettings.LoadFromConfig(config);
            ArenaSettings = new(ArenaSectionName, Arena.DefaultSettings);
            ArenaSettings.LoadFromConfig(config);
            SnakeSettings = new(SnakeSectionName, Snake.DefaultSettings);
            SnakeSettings.LoadFromConfig(config);
            AbilitySettings = new(AbilitySectionName, Ability.AllDefaultAbilitySettings);
            AbilitySettings.LoadFromConfig(config);
        }

        /// <summary>
        /// saves settings to settings.cfg file
        /// </summary>
        public void SaveSettings()
        {
            GraphicsSettings.SaveToConfig(config);
            AudioSettings.SaveToConfig(config);
            ArenaSettings.SaveToConfig(config);
            SnakeSettings.SaveToConfig(config);
            AbilitySettings.SaveToConfig(config);

            // save to file
            var error = config.Save(settingsFilePathRelative);
            GD.Print($"Settings File Saving: {error}");
        }

        public void WipeSettings()
        {
            if (File.Exists(settingsFilePathAbsolute))
            {
                File.WriteAllText(settingsFilePathAbsolute, string.Empty);
                LoadSettings();
                SaveSettings();
            }
            else
            {
                GD.PrintErr("Config file not found at " + settingsFilePathAbsolute);
            }
        }

        /// <summary>
        /// this saves the current settings to the settings config file,
        /// loads the file into a new Settings object and returns the object
        /// </summary>
        public Settings NewCopy()
        {
            SaveSettings();
            Settings newSettings = new();
            newSettings.LoadSettings();
            return newSettings;
        }
    }

    public class SettingsSection
    {
        string ConfigSectionName;

        /// <summary>
        /// settings with their config file key
        /// </summary>
        public Dictionary<string, Variant> Settings = new();

        public SettingsSection(string sectionName, Dictionary<string, Variant> defaultSettings)
        {
            ConfigSectionName = sectionName;
            Settings = defaultSettings;
        }

        public void LoadFromConfig(ConfigFile config)
        {
            if (config.HasSection(ConfigSectionName))
            {
                foreach (var key in config.GetSectionKeys(ConfigSectionName))
                {
                    if (Settings.ContainsKey(key))
                    {
                        Settings[key] = config.GetValue(ConfigSectionName, key);
                    }
                    else
                    {
                        Settings.Add(key, config.GetValue(ConfigSectionName, key));
                    }
                }
            }
        }

        public void SaveToConfig(ConfigFile config)
        {
            foreach (var setting in Settings)
            {
                config.SetValue(ConfigSectionName, setting.Key, setting.Value);
            }
        }
    }

    public class GraphicsSettings : SettingsSection
    {
        static string vSyncSettingName = "VSync";
        static string fpsLimitSettingName = "FPSLimit";

        static Dictionary<string, Variant> defaultSettings => new()
        {
            {vSyncSettingName, (int)DisplayServer.VSyncMode.Disabled},
            {fpsLimitSettingName, 0} // 0 = unlimited
        };

        public GraphicsSettings() : base("Graphics", defaultSettings)
        {
        }

        public DisplayServer.VSyncMode VSyncSetting
        {
            get => (DisplayServer.VSyncMode)(int)Settings[vSyncSettingName];
            set => Settings[vSyncSettingName] = (int)value;
        }
        
        /// <summary>
        /// 0 = unlimited
        /// </summary>
        public int FPSLimitSetting
        {
            get => (int)Settings[fpsLimitSettingName];
            set => Settings[fpsLimitSettingName] = value;
        }
    }
}