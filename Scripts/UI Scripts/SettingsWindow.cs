using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK.UI
{
    public partial class SettingsWindow : Control
    {
        [Export] Button confirmButton, cancelButton;
        [Export] Slider masterVolumeSlider;
        [Export] Slider musicVolumeSlider;
        [Export] Slider soundVolumeSlider;

        // local copies
        Settings localSettings;
        AudioSettings localAudioSettings => localSettings.AudioSettings;
        ArenaSettings localArenaSettings => localSettings.ArenaSettings;
        SnakeSettings localSnakeSettings => localSettings.SnakeSettings;
        AbilitySettings localAbilitySettings => localSettings.AbilitySettings;

        public override void _Ready()
        {
            base._Ready();

            localSettings = GameManager.Instance.Settings.NewCopy();
            FillAllAbilitySettings();

            confirmButton.Pressed += OnConfirmButtonClicked;
            cancelButton.Pressed += OnCancelButtonClicked;

            masterVolumeSlider.SetValueNoSignal(AudioManager.Instance.MasterVolume);
            masterVolumeSlider.ValueChanged += OnMasterVolumeInput;
            musicVolumeSlider.SetValueNoSignal(AudioManager.Instance.MusicVolume);
            musicVolumeSlider.ValueChanged += OnMusicVolumeInput;
            soundVolumeSlider.SetValueNoSignal(AudioManager.Instance.SoundVolume);
            soundVolumeSlider.ValueChanged += OnSoundVolumeInput;
        }

        /// <summary>
        /// fills AbilitySettings with default values for all missing entrys
        /// </summary>
        void FillAllAbilitySettings()
        {
            List<(string key, Variant setting)> defaultAbilitySettings = new();
            defaultAbilitySettings.AddRange(EraserAbility.DefaultSettings);
            defaultAbilitySettings.AddRange(SpeedAbility.DefaultSettings);
            defaultAbilitySettings.AddRange(TBarAbility.DefaultSettings);
            defaultAbilitySettings.AddRange(TeleportAbility.DefaultSettings);
            defaultAbilitySettings.AddRange(VBarAbility.DefaultSettings);

            foreach (var defaultSetting in defaultAbilitySettings)
            {
                if (!localAbilitySettings.Settings.ContainsKey(defaultSetting.key))
                {
                    localAbilitySettings.Settings.Add(defaultSetting.key, defaultSetting.setting);
                }
            }
        }

        void SpawnAbilitySettings()
        {
            foreach (var setting in localAbilitySettings.Settings)
            {
                // spawn the corresponding prefab to setting type
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

        void OnMasterVolumeInput(double value)
        {
            localAudioSettings.MasterVolume = (float)value;
            AudioManager.Instance?.SetMasterVolume((float)value);
        }

        private void OnMusicVolumeInput(double value)
        {
            localAudioSettings.MusicVolume = (float)value;
            AudioManager.Instance?.SetMusicVolume((float)value);
        }

        private void OnSoundVolumeInput(double value)
        {
            localAudioSettings.SoundVolume = (float)value;
            AudioManager.Instance?.SetSoundVolume((float)value);
        }
    }
}
