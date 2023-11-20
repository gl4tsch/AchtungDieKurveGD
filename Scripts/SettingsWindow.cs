using Godot;
using System;

namespace ADK
{
    public partial class SettingsWindow : Control
    {
        [Export] Button backButton;
        [Export] Slider masterVolumeSlider;
        [Export] Slider musicVolumeSlider;
        [Export] Slider soundVolumeSlider;

        public override void _Ready()
        {
            base._Ready();

            backButton.Pressed += OnBackButtonClicked;

            masterVolumeSlider.SetValueNoSignal(AudioManager.Instance.MasterVolume);
            masterVolumeSlider.ValueChanged += OnMasterVolumeInput;
            musicVolumeSlider.SetValueNoSignal(AudioManager.Instance.MusicVolume);
            musicVolumeSlider.ValueChanged += OnMusicVolumeInput;
            soundVolumeSlider.SetValueNoSignal(AudioManager.Instance.SoundVolume);
            soundVolumeSlider.ValueChanged += OnSoundVolumeInput;
        }

        void OnBackButtonClicked()
        {
            Visible = false;
        }

        void OnMasterVolumeInput(double value)
        {
            AudioManager.Instance?.SetMasterVolume((float)value);
        }

        private void OnMusicVolumeInput(double value)
        {
            AudioManager.Instance?.SetMusicVolume((float)value);
        }

        private void OnSoundVolumeInput(double value)
        {
            AudioManager.Instance?.SetSoundVolume((float)value);
        }
    }
}
