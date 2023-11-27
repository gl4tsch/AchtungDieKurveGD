using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK
{
    public abstract class Ability
    {
        public abstract string Name {get;}
        int uses = 999;
        public int Uses
        {
            get => uses;
            set
            {
                uses = value;
                UsesChanged?.Invoke(uses);
            }
        }
        public Action<int> UsesChanged;

        public static Dictionary<string, Variant> AllDefaultAbilitySettings
        {
            get
            {
                Dictionary<string, Variant> ret = new();
                EraserAbility.DefaultSettings.ToList().ForEach(ea => ret.Add(ea.Key, ea.Value));
                SpeedAbility.DefaultSettings.ToList().ForEach(sa => ret.Add(sa.Key, sa.Value));
                TBarAbility.DefaultSettings.ToList().ForEach(tb => ret.Add(tb.Key, tb.Value));
                VBarAbility.DefaultSettings.ToList().ForEach(vb => ret.Add(vb.Key, vb.Value));
                TeleportAbility.DefaultSettings.ToList().ForEach(ta => ret.Add(ta.Key, ta.Value));
                return ret;
            }
        }

        public Ability(SettingsSection settings)
        {
            ApplySettings(settings);
        }

        public abstract void ApplySettings(SettingsSection settings);
        //public abstract List<(string key, Variant setting)> GetDefaultSettings();

        public void Activate(Snake snake)
        {
            if (Uses <= 0)
            {
                return;
            }
            Uses--;
            Perform(snake);
        }

        protected abstract void Perform(Snake snake);

        /// <param name="deltaT">time since last tick in seconds</param>
        public virtual void Tick(float deltaT){}
    }
}
