using Godot;

namespace ADK.Net
{
    public struct PlayerInfo
    {
        public PlayerInfo(){}
        public PlayerInfo(string name, Color color, int ability)
        {
            Name = name;
            Color = color;
            Ability = ability;
        }

        public string Name = "Snake";
        public Color Color = new Color(1,0,0);
        public int Ability = 0;
    }
}
