using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public abstract class Ability
    {
        public abstract string Name {get;}
        public abstract void Activate(Snake snake);

        /// <param name="deltaT">time since last tick in seconds</param>
        public virtual void Tick(float deltaT){}
    }
}
