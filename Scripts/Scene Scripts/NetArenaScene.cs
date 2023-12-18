using Godot;
using System;

namespace ADK.Net
{
    public partial class NetArenaScene : Node
    {
        public override void _Ready()
        {
            base._Ready();
            NetworkManager.Instance.AllReady += OnAllPlayersReady;
            NetworkManager.Instance.SendReady();
            GD.Print("Waiting for other Players...");
        }

        void OnAllPlayersReady()
        {
            GD.Print("All Ready! Go!");
        }
    }
}
