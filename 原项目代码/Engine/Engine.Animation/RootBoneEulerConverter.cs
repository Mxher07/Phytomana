using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Animation {
    /// <summary>
    /// 根骨骼旋转欧拉角转换器（单位：度）。
    /// 单数字 → (0, value, 0)（绕 Y，向后兼容旧 float 配置）；
    /// 数组 [x,y,z] 或对象 {"X":..,"Y":..,"Z":..} → Vector3；
    /// 缺省/null → null。
    /// </summary>
    public class RootBoneEulerConverter : JsonConverter<Vector3?> {
        public override Vector3? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return null;
            }
            if (reader.TokenType == JsonTokenType.Number) {
                return new Vector3(0f, reader.GetSingle(), 0f);
            }
            if (reader.TokenType == JsonTokenType.StartArray
                || reader.TokenType == JsonTokenType.StartObject) {
                return JsonSerializer.Deserialize<Vector3>(ref reader, options);
            }
            throw new JsonException($"Invalid rootBoneRotation token: {reader.TokenType} (expected number, array, or object)");
        }

        public override void Write(Utf8JsonWriter writer, Vector3? value, JsonSerializerOptions options) {
            if (!value.HasValue) {
                writer.WriteNullValue();
                return;
            }
            Vector3 v = value.Value;
            // 纯 Y → 单数字（向后兼容输出）
            if (v.X == 0f && v.Z == 0f) {
                writer.WriteNumberValue(v.Y);
                return;
            }
            writer.WriteStartArray();
            writer.WriteNumberValue(v.X);
            writer.WriteNumberValue(v.Y);
            writer.WriteNumberValue(v.Z);
            writer.WriteEndArray();
        }
    }
}
