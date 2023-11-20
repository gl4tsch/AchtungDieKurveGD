using Godot;

namespace ADK
{
    public static class HelperExtensions
    {
        public static float Map(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            if (fromMax == fromMin || toMax == toMin) return toMin;
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
    }
}