using Godot;
using System;

namespace ADK.UI
{
    public partial class HuePickerButton : Button
    {
        [Export] PackedScene huePopupPrefab;
        public float Hue { get; set; }
        public Action<float> HueChanged;
        public Action<float> HuePicked;

        ColorPickerPopup colorPicker;

        public override void _Pressed()
        {
            base._Pressed();

            colorPicker = huePopupPrefab.Instantiate<ColorPickerPopup>();

            var hueSlider = colorPicker.HueSlider;
            hueSlider.SetHueNoNotify(Hue);
            hueSlider.HueChanged += OnHueChanged;
            CallDeferred(MethodName.DeferredUpdateSliderHue);

            var closeButton = colorPicker.BgButton;
            closeButton.Pressed += () => HuePicked?.Invoke(Hue);

            // place picker as overlay at mouse position
            GetRootControl(this).AddChild(colorPicker);
            colorPicker.GlobalPosition = GetGlobalMousePosition() - colorPicker.Size / 2;
        }

        void OnHueChanged(double hue)
        {
            Hue = (float)hue;
            HueChanged?.Invoke(Hue);
        }

        Control GetRootControl(Control startControl)
        {
            if (startControl.GetParent() is not Control)
            {
                return startControl;
            }
            return GetRootControl(startControl.GetParentControl());
        }

        void DeferredUpdateSliderHue()
        {
            var hueSlider = colorPicker.HueSlider;
            hueSlider.SetHueNoNotify(Hue);
        }
    }
}
