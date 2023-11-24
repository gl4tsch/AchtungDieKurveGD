
using System;
using System.Collections.Generic;
using Godot;

namespace ADK
{
    public class Settings
    {
        string settingsFilePath = "user://settings.cfg";
        ConfigFile config = new();

        public AudioSettings AudioSettings { get; private set; }
        public ArenaSettings ArenaSettings { get; private set; }
        public SnakeSettings SnakeSettings { get; private set; }
        public AbilitySettings AbilitySettings { get; private set; }

        /// <summary>
        /// loads settings from settings.cfg file
        /// </summary>
        public void LoadSettings()
        {
            var error = config.Load(settingsFilePath);
            GD.Print($"Settings File Loading: {error}");
            AudioSettings = new(config);
            ArenaSettings = new(config);
            SnakeSettings = new(config);
            AbilitySettings = new(config);
        }

        /// <summary>
        /// saves settings to settings.cfg file
        /// </summary>
        public void SaveSettings()
        {
            AudioSettings.SaveToConfig(config);
            ArenaSettings.SaveToConfig(config);
            SnakeSettings.SaveToConfig(config);
            AbilitySettings.SaveToConfig(config);
            // save to file
            var error = config.Save(settingsFilePath);
            GD.Print($"Settings File Saving: {error}");
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

    public class AudioSettings
    {
        static readonly string ConfigSectionName = "Audio";
        public float MasterVolume { get; set; }
        public float MusicVolume { get; set; }
        public float SoundVolume { get; set; }

        public AudioSettings(ConfigFile config)
        {
            MasterVolume = (float)config.GetValue(ConfigSectionName, nameof(MasterVolume), 100);
            MusicVolume = (float)config.GetValue(ConfigSectionName, nameof(MusicVolume), 100);
            SoundVolume = (float)config.GetValue(ConfigSectionName, nameof(SoundVolume), 100);
        }

        public void SaveToConfig(ConfigFile config)
        {
            config.SetValue(ConfigSectionName, nameof(MasterVolume), MasterVolume);
            config.SetValue(ConfigSectionName, nameof(MusicVolume), MusicVolume);
            config.SetValue(ConfigSectionName, nameof(SoundVolume), SoundVolume);
        }
    }

    public class ArenaSettings
    {
        static readonly string ConfigSectionName = "Arena";
        public int PxWidth { get; set; }
        public int PxHeight { get; set; }

        public ArenaSettings(ConfigFile config)
        {
            PxWidth = (int)config.GetValue(ConfigSectionName, nameof(PxWidth), 1024);
            PxHeight = (int)config.GetValue(ConfigSectionName, nameof(PxHeight), 1024);
        }

        public void SaveToConfig(ConfigFile config)
        {
            config.SetValue(ConfigSectionName, nameof(PxWidth), PxWidth);
            config.SetValue(ConfigSectionName, nameof(PxHeight), PxHeight);
        }
    }

    public class SnakeSettings
    {
        public static readonly string ConfigSectionName = "Snake";
        public float Thickness { get; private set; }
        public float MoveSpeed { get; private set; }
        public float TurnRate { get; private set; }
        public float GapFrequency { get; private set; }
        public float GapWidthRelToThickness { get; private set; }

        public SnakeSettings(ConfigFile config)
        {
            Thickness = (float)config.GetValue(ConfigSectionName, nameof(Thickness), 10);
            MoveSpeed = (float)config.GetValue(ConfigSectionName, nameof(MoveSpeed), 100);
            TurnRate = (float)config.GetValue(ConfigSectionName, nameof(TurnRate), 3);
            GapFrequency = (float)config.GetValue(ConfigSectionName, nameof(GapFrequency), 400);
            GapWidthRelToThickness = (float)config.GetValue(ConfigSectionName, nameof(GapWidthRelToThickness), 3);
        }

        public void SaveToConfig(ConfigFile config)
        {
            config.SetValue(ConfigSectionName, nameof(Thickness), Thickness);
            config.SetValue(ConfigSectionName, nameof(MoveSpeed), MoveSpeed);
            config.SetValue(ConfigSectionName, nameof(TurnRate), TurnRate);
            config.SetValue(ConfigSectionName, nameof(GapFrequency), GapFrequency);
            config.SetValue(ConfigSectionName, nameof(GapWidthRelToThickness), GapWidthRelToThickness);
        }
    }

    public class AbilitySettings
    {
        public static readonly string ConfigSectionName = "Abilities";
        /// <summary>
        /// Ability settings with their config file key
        /// </summary>
        public Dictionary<string, Variant> Settings = new();

        public AbilitySettings(ConfigFile config)
        {
            // either: handle it like this and have each Ability look for its settings in the dict
            // or: pass the ConfigFile directly to each Ability and let them handle saving and loading
            // with their own Section Name
            if (config.HasSection(ConfigSectionName))
            {
                foreach (var key in config.GetSectionKeys(ConfigSectionName))
                {
                    Settings.Add(key, config.GetValue(ConfigSectionName, key));
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
}