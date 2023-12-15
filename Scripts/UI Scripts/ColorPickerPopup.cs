using ADK.UI;
using Godot;
using System;

namespace ADK.UI
{
    public partial class ColorPickerPopup : Control
    {
        [Export] public HueSlider HueSlider { get; private set; }
        [Export] public Button BgButton {get; private set;}

        public override void _Ready()
        {
            base._Ready();
            BgButton.Pressed += OnBgButtonClicked;
        }

        private void OnBgButtonClicked()
        {
            QueueFree();
        }
    }
}
