using System;
using Engine.Serialization;

namespace TemplatesDatabase {
    [HumanReadableConverter(typeof(ProceduralValue))]
    public class ProceduralValueStringConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((ProceduralValue)value).Procedure;

        public object ConvertFromString(Type type, string data) {
            ProceduralValue proceduralValue = default;
            proceduralValue.Procedure = data;
            return proceduralValue;
        }
    }
}