using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public abstract class Ability
    {
        public abstract string Name {get;}
        public abstract void Activate();
    }
}
