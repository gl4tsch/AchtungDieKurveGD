using Godot;
using System;

namespace ADK.UI
{
    public partial class SettingsWindow : Control
    {
        [Export] Button backButton;
        [Export] Slider masterVolumeSlider;
        [Export] Slider musicVolumeSlider;
        [Export] Slider soundVolumeSlider;

        AudioSettings audioSettings;
        ArenaSettings arenaSettings;
        SnakeSettings snakeSettings;

        public override void _Ready()
        {
            base._Ready();

            audioSettings = GameManager.Instance.Settings.AudioSettings;
            arenaSettings = GameManager.Instance.Settings.ArenaSettings;
            snakeSettings = GameManager.Instance.Settings.SnakeSettings;

            backButton.Pressed += OnBackButtonClicked;

            masterVolumeSlider.SetValueNoSignal(AudioManager.Instance.MasterVolume);
            masterVolumeSlider.ValueChanged += OnMasterVolumeInput;
            musicVolumeSlider.SetValueNoSignal(AudioManager.Instance.MusicVolume);
            musicVolumeSlider.ValueChanged += OnMusicVolumeInput;
            soundVolumeSlider.SetValueNoSignal(AudioManager.Instance.SoundVolume);
            soundVolumeSlider.ValueChanged += OnSoundVolumeInput;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            GameManager.Instance.ApplySnakeSettings(snakeSettings);
            GameManager.Instance.Settings.SaveSettings();
        }

        void OnBackButtonClicked()
        {
            Visible = false;
        }

        void OnMasterVolumeInput(double value)
        {
            audioSettings.MasterVolume = (float)value;
            AudioManager.Instance?.SetMasterVolume((float)value);
        }

        private void OnMusicVolumeInput(double value)
        {
            audioSettings.MusicVolume = (float)value;
            AudioManager.Instance?.SetMusicVolume((float)value);
        }

        private void OnSoundVolumeInput(double value)
        {
            audioSettings.SoundVolume = (float)value;
            AudioManager.Instance?.SetSoundVolume((float)value);
        }
    }
}
