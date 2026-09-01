namespace Engine.Serialization {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public class SerializationOptionsAttribute : Attribute {
        public bool UseObjectInfo = true;

        public AutoConstructMode AutoConstruct;
    }
}