namespace Engine.Graphics {
    public class ModelLight {
        public const int MaxPunctualLights = 7;

        public ModelLightType Type { get; set; } = ModelLightType.Point;
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; } = -Vector3.UnitY;
        public Vector3 Color { get; set; } = Vector3.One;
        public float Intensity { get; set; } = 1f;
        public float Range { get; set; } = float.PositiveInfinity;
        public float InnerConeCos { get; set; } = 1.0f; // cos(0°), KHR_lights_punctual default
        public float OuterConeCos { get; set; } = 0.70710678118f; // cos(PI/4), KHR_lights_punctual default
        public int BoneIndex { get; set; } = -1;
        public bool IsVisible { get; set; } = true;
    }

    public enum ModelLightType {
        Directional,
        Point,
        Spot
    }
}