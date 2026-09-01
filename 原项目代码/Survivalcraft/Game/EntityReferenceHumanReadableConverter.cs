using Engine.Serialization;

namespace Game {
    [HumanReadableConverter(typeof(EntityReference))]
    public class EntityReferenceHumanReadableConverter : IHumanReadableConverter {
        public string ConvertToString(object value) => ((EntityReference)value).ReferenceString;

        public object ConvertFromString(Type type, string data) => EntityReference.FromReferenceString(data);
    }
}