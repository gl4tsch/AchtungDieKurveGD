using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK.UI
{
    public partial class SettingsWindow : Control
    {
        [Export] Button confirmButton, cancelButton, wipeButton;

        [Export] OptionButton vSyncDD, fpsLimitDD;

        [Export] Slider masterVolumeSlider;
        [Export] Slider musicVolumeSlider;
        [Export] Slider soundVolumeSlider;

        [Export] Control arenaSettingsContainer, snakeSettingsContainer, abilitySettingsContainer;
        [Export] PackedScene numberFieldPrefab;

        // local copies
        Settings localSettings;
        GraphicsSettings localGraphicsSettings => localSettings.GraphicsSettings;
        SettingsSection localAudioSettings => localSettings.AudioSettings;
        SettingsSection localArenaSettings => localSettings.ArenaSettings;
        SettingsSection localSnakeSettings => localSettings.SnakeSettings;
        SettingsSection localAbilitySettings => localSettings.AbilitySettings;

        List<(string name, DisplayServer.VSyncMode mode)> vSyncOptions = new()
        {
            ("Disabled", DisplayServer.VSyncMode.Disabled),
            ("Enabled", DisplayServer.VSyncMode.Enabled),
            ("Adaptive", DisplayServer.VSyncMode.Adaptive),
            ("Mailbox", DisplayServer.VSyncMode.Mailbox)
        };

        List<(string name, int limit)> fpsLimitOptions = new()
        {
            ("Unlimited", 0),
            ("30", 30),
            ("60", 60),
            ("75", 75),
            ("120", 120),
            ("144", 144),
            ("240", 240)
        };

        public override void _Ready()
        {
            base._Ready();

            localSettings = GameManager.Instance.Settings.NewCopy();

            confirmButton.Pressed += OnConfirmButtonClicked;
            cancelButton.Pressed += OnCancelButtonClicked;
            wipeButton.Pressed += OnWipeButtonClicked;

            InitGraphicsSettings();

            masterVolumeSlider.SetValueNoSignal(AudioManager.Instance.MasterVolume);
            masterVolumeSlider.ValueChanged += OnMasterVolumeInput;
            musicVolumeSlider.SetValueNoSignal(AudioManager.Instance.MusicVolume);
            musicVolumeSlider.ValueChanged += OnMusicVolumeInput;
            soundVolumeSlider.SetValueNoSignal(AudioManager.Instance.SoundVolume);
            soundVolumeSlider.ValueChanged += OnSoundVolumeInput;

            SpawnSettings(localArenaSettings, arenaSettingsContainer);
            SpawnSettings(localSnakeSettings, snakeSettingsContainer);
            SpawnSettings(localAbilitySettings, abilitySettingsContainer);
        }

        void InitGraphicsSettings()
        {
            // VSync
            vSyncDD.Clear();
            foreach (var option in vSyncOptions)
            {
                vSyncDD.AddItem(option.name);
            }
            vSyncDD.Select(vSyncOptions.FindIndex(o => o.mode == localGraphicsSettings.VSyncSetting));
            vSyncDD.ItemSelected += OnVSyncOptionSelected;

            // fps limit
            fpsLimitDD.Clear();
            foreach (var item in fpsLimitOptions)
            {
                fpsLimitDD.AddItem(item.name);
            }
            fpsLimitDD.Select(fpsLimitOptions.FindIndex(o => o.limit == localGraphicsSettings.FPSLimitSetting));
            fpsLimitDD.ItemSelected += OnFpsLimitSelected;
        }

        void SpawnSettings(SettingsSection section, Control container)
        {
            foreach (var setting in section.Settings)
            {
                var settingField = numberFieldPrefab.Instantiate<NumberSettingInputField>().Init(setting.Key, setting.Value);
                container.AddChild(settingField);
                settingField.ValueChanged += v => section.Settings[setting.Key] = v;
            }
        }
        
        void OnConfirmButtonClicked()
        {
            // save settings
            GameManager.Instance.ApplySettings(localSettings);
            GameManager.Instance.Settings.SaveSettingsToConfig();
            QueueFree();
        }

        void OnCancelButtonClicked()
        {
            // revert to old settings
            GameManager.Instance.ApplySettings(GameManager.Instance.Settings);
            QueueFree();
        }

        void OnWipeButtonClicked()
        {
            // clear settings file
            GameManager.Instance.Settings.WipeSettings();
            QueueFree();
        }

        void OnVSyncOptionSelected(long value)
        {
            var mode = vSyncOptions[(int)value].mode;
            localGraphicsSettings.VSyncSetting = mode;
            // apply temporarily
            DisplayServer.WindowSetVsyncMode(mode);
        }

        void OnFpsLimitSelected(long value)
        {
            int limit = fpsLimitOptions[(int)value].limit;
            localGraphicsSettings.FPSLimitSetting = limit;
            // apply temporarily
            Engine.MaxFps = limit;
        }

        void OnMasterVolumeInput(double value)
        {
            localAudioSettings.Settings[nameof(AudioManager.MasterVolume)] = (float)value;
            AudioManager.Instance?.SetMasterVolume((float)value);
        }

        private void OnMusicVolumeInput(double value)
        {
            localAudioSettings.Settings[nameof(AudioManager.MusicVolume)] = (float)value;
            AudioManager.Instance?.SetMusicVolume((float)value);
        }

        private void OnSoundVolumeInput(double value)
        {
            localAudioSettings.Settings[nameof(AudioManager.SoundVolume)] = (float)value;
            AudioManager.Instance?.SetSoundVolume((float)value);
        }
    }
}
