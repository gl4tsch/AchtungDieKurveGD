
using Godot;

namespace ADK.UI
{
    public partial class NumberSettingInputField : SettingsWindowEntry
    {
        [Export] SpinBox inputField;

        public override void _Ready()
        {
            base._Ready();
            inputField.ValueChanged += OnFieldInput;
        }

        void OnFieldInput(double value)
        {
            SetValue(value);
        }

        protected override void SetValueNoNotify(Variant value)
        {
            base.SetValueNoNotify(value);
            inputField.Value = (double)value;
        }
    }
}