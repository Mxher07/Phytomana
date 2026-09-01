namespace Engine.Serialization {
    [AttributeUsage(AttributeTargets.Class)]
    public class HumanReadableConverterAttribute : Attribute {
        public Type[] Types;

        public HumanReadableConverterAttribute(params Type[] types) => Types = types.ToArray();
    }
}