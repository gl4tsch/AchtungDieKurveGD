
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
            // save to file
            var error = config.Save(settingsFilePath);
            GD.Print($"Settings File Saving: {error}");
        }
    }

    public class AudioSettings
    {
        static readonly string ConfigSectionName = "Audio";
        static readonly string MasterVolumeKey = "MasterVolume";
        static readonly string MusicVolumeKey = "MusicVolume";
        static readonly string SoundVolumeKey = "SoundVolume";
        public float MasterVolume { get; set; }
        public float MusicVolume { get; set; }
        public float SoundVolume { get; set; }

        public AudioSettings(ConfigFile config)
        {
            MasterVolume = (float)config.GetValue(ConfigSectionName, MasterVolumeKey, 100);
            MusicVolume = (float)config.GetValue(ConfigSectionName, MusicVolumeKey, 100);
            SoundVolume = (float)config.GetValue(ConfigSectionName, SoundVolumeKey, 100);
        }

        public void SaveToConfig(ConfigFile config)
        {
            config.SetValue(ConfigSectionName, MasterVolumeKey, MasterVolume);
            config.SetValue(ConfigSectionName, MusicVolumeKey, MusicVolume);
            config.SetValue(ConfigSectionName, SoundVolumeKey, SoundVolume);
        }
    }

    public class ArenaSettings
    {
        public static readonly string ConfigSectionName = "Arena";
        public int PxWidth { get; private set; }
        public int PxHeight { get; private set; }

        public ArenaSettings(ConfigFile config)
        {
            PxWidth = (int)config.GetValue(ConfigSectionName, "PxWidth", 1024);
            PxHeight = (int)config.GetValue(ConfigSectionName, "PxHeight", 1024);
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
            Thickness = (float)config.GetValue(ConfigSectionName, "Thickness", 10);
            MoveSpeed = (float)config.GetValue(ConfigSectionName, "MoveSpeed", 100);
            TurnRate = (float)config.GetValue(ConfigSectionName, "TurnRate", 3);
            GapFrequency = (float)config.GetValue(ConfigSectionName, "GapFrequency", 400);
            GapWidthRelToThickness = (float)config.GetValue(ConfigSectionName, "GapWidthToThickness", 3);
        }
    }

    public class AbilitySettings
    {
        public static readonly string ConfigSectionName = "Abilities";
        public Dictionary<string, Variant> Settings;

        public AbilitySettings(ConfigFile config)
        {
            // TODO: either handle like this and have each Ability look for its settings in the dict
            // or: pass the ConfigFile directly to each Ability and let them handle saving and loading
            // with their own Section Name
            if (config.HasSection(ConfigSectionName))
            {
                //config.GetSectionKeys(ConfigSectionName);
            }
        }
    }
}