using Godot;
using System;

namespace ADK
{
    public class EraserAbility : Ability
    {
        public static string DisplayName => "Eraser";
        public override string Name => DisplayName;
        
        public override void Activate(Snake snake)
        {
            GD.Print("Activating " + Name);
        }
    }
}
