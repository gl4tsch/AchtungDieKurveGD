using Godot;
using System;

namespace ADK.UI
{
    public partial class SettingsWindowEntry : Control
    {
        protected Label settingLabel => GetNode("Label") as Label;

        Variant value;
        Variant.Type valueType;
        public event Action<Variant> ValueChanged;

        public SettingsWindowEntry Init(string title, Variant initialValue)
        {
            settingLabel.Text = title;
            valueType = initialValue.VariantType;
            SetValueNoNotify(initialValue);
            return this;
        }

        protected void SetValue(Variant value)
        {
            SetValueNoNotify(value);
            ValueChanged?.Invoke(this.value);
        }

        protected virtual void SetValueNoNotify(Variant value)
        {
            this.value = value;
        }
    }
}
