using ADK.UI;
using Godot;
using System;

namespace ADK.UI
{
    public partial class ColorPickerPopup : Control
    {
        [Export] public HueSlider HueSlider { get; private set; }
        [Export] Button bgButton;

        public override void _Ready()
        {
            base._Ready();
            bgButton.Pressed += OnBgButtonClicked;
        }

        private void OnBgButtonClicked()
        {
            QueueFree();
        }
    }
}
