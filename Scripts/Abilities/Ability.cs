using Godot;
using System;
using System.Collections.Generic;

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

        public Ability(AbilitySettings settings)
        {
            ApplySettings(settings);
        }

        public abstract void ApplySettings(AbilitySettings settings);
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
