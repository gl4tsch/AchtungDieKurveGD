using Godot;
using System;
using System.Collections;

namespace ADK.UI
{
    public partial class HueSlider : Control
    {
        [Export] Slider slider;

        public event Action<double> HueChanged;

        public override void _Ready()
        {
            base._Ready();
            slider.ValueChanged += OnSliderValueChanged;
        }

        /// <param name="hue">[0,1]</param>
        public void SetHue(float hue)
        {
            slider.Value = hue.Map(0, 1, (float)slider.MinValue, (float)slider.MaxValue);
        }

        public void SetHueNoNotify(float hue)
        {
            slider.SetValueNoSignal(hue.Map(0, 1, (float)slider.MinValue, (float)slider.MaxValue));
        }

        void OnSliderValueChanged(double value)
        {
            HueChanged?.Invoke(((float)value).Map((float)slider.MinValue, (float)slider.MaxValue, 0, 1));
        }
    }
}
