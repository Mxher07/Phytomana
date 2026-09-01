using Engine.Graphics;

namespace Engine.Media {
    public class ModelLightData {
        public ModelLightType Type;
        public Vector3 Position;
        public Vector3 Direction = -Vector3.UnitY;
        public Vector3 Color = Vector3.One;
        public float Intensity = 1f;
        public float Range = float.PositiveInfinity;
        public float InnerConeCos = 1.0f; // cos(0°), KHR_lights_punctual default
        public float OuterConeCos = 0.70710678118f; // cos(PI/4), KHR_lights_punctual default
        public int BoneIndex = -1; // 对应的骨骼索引，用于动画驱动的灯光位置更新
        public bool IsVisible = true;
    }
}