using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Animation {
    /// <summary>
    /// Vector3 JSON 转换器，支持数组和对象两种格式
    /// 数组：[1.0, 2.0, 3.0]
    /// 对象：{ "X": 1.0, "Y": 2.0, "Z": 3.0 }
    /// </summary>
    public class Vector3JsonConverter : JsonConverter<Vector3> {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.StartArray) {
                reader.Read();
                float x = reader.GetSingle();
                reader.Read();
                float y = reader.GetSingle();
                reader.Read();
                float z = reader.GetSingle();
                reader.Read(); // EndArray
                return new Vector3(x, y, z);
            }
            if (reader.TokenType == JsonTokenType.StartObject) {
                float x = 0, y = 0, z = 0;
                while (reader.Read()) {
                    if (reader.TokenType == JsonTokenType.EndObject) break;
                    string prop = reader.GetString();
                    reader.Read();
                    switch (prop?.ToUpperInvariant()) {
                        case "X": x = reader.GetSingle(); break;
                        case "Y": y = reader.GetSingle(); break;
                        case "Z": z = reader.GetSingle(); break;
                    }
                }
                return new Vector3(x, y, z);
            }
            return Vector3.Zero;
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteNumberValue(value.Z);
            writer.WriteEndArray();
        }
    }
}
