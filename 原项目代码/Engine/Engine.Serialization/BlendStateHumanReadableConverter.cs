using Engine.Graphics;

namespace Engine.Serialization {
    [HumanReadableConverter(typeof(BlendState))]
    public class BlendStateHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) {
            BlendState blendState = (BlendState)value;
            if (blendState.BaseEquals(BlendState.Opaque)) {
                return "Opaque";
            }
            if (blendState.BaseEquals(BlendState.Additive)) {
                return "Additive";
            }
            if (blendState.BaseEquals(BlendState.AlphaBlend)) {
                return "AlphaBlend";
            }
            if (blendState.BaseEquals(BlendState.NonPremultiplied)) {
                return "NonPremultiplied";
            }
            return "Unknown";
        }

        public object ConvertFromString(Type type, string data) {
            switch (data.ToLower()) {
                case "additive": return BlendState.Additive;
                case "alphablend": return BlendState.AlphaBlend;
                case "nonpremultiplied": return BlendState.NonPremultiplied;
                default: return BlendState.Opaque;
            }
        }
    }
}