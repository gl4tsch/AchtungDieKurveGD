using Godot;
using System;

namespace ADK
{
    public class TBarAbility : Ability
    {
        public static string DisplayName => "TBar";
        public override string Name => DisplayName;
        
        public override void Activate()
        {
            GD.Print("Activating " + Name);
        }
    }
}
