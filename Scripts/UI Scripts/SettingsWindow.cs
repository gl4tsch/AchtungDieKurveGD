using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK.UI
{
    public partial class SettingsWindow : Control
    {
        [Export] Button confirmButton, cancelButton, wipeButton;

        [Export] Slider masterVolumeSlider;
        [Export] Slider musicVolumeSlider;
        [Export] Slider soundVolumeSlider;

        [Export] Control arenaSettingsContainer, snakeSettingsContainer, abilitySettingsContainer;
        [Export] PackedScene numberFieldPrefab;

        // local copies
        Settings localSettings;
        SettingsSection localAudioSettings => localSettings.AudioSettings;
        SettingsSection localArenaSettings => localSettings.ArenaSettings;
        SettingsSection localSnakeSettings => localSettings.SnakeSettings;
        SettingsSection localAbilitySettings => localSettings.AbilitySettings;

        public override void _Ready()
        {
            base._Ready();

            localSettings = GameManager.Instance.Settings.NewCopy();

            confirmButton.Pressed += OnConfirmButtonClicked;
            cancelButton.Pressed += OnCancelButtonClicked;
            wipeButton.Pressed += OnWipeButtonClicked;

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
            GameManager.Instance.Settings.SaveSettings();
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
