using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Animation.RootMotion;

namespace Engine.Animation {
    /// <summary>
    /// AnimationReference 的自定义 JSON 转换器
    /// 处理动态属性（speed、loop 等可以是静态值或表达式字符串）
    /// </summary>
    public class AnimationReferenceConverter : JsonConverter<AnimationReference> {
        // 处理 null 字面量：使 Read 收到 Null token（默认 System.Text.Json 对 null 跳过 converter）
        public override bool HandleNull => true;

        public override AnimationReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            AnimationReference reference = new();
            // 简写：animation 为字符串时等价于 { "source": <字符串> }
            if (reader.TokenType == JsonTokenType.String) {
                reference.Source = reader.GetString();
                return reference;
            }
            // null 简写：animation:null 等价于 { "source": null }（reference.Source 默认 null）
            if (reader.TokenType == JsonTokenType.Null) {
                return reference;
            }
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject, String, or Null token");
            }
            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    break;
                }
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }
                string propertyName = reader.GetString();
                reader.Read();
                switch (propertyName.ToLowerInvariant()) {
                    case "source": reference.Source = reader.GetString(); break;
                    case "speed": reference.SpeedValue = ReadDynamicValue(ref reader); reference.HasSpeed = true; break;
                    case "loop": reference.LoopValue = ReadDynamicValue(ref reader); reference.HasLoop = true; break;
                    case "startphase": reference.StartPhaseValue = ReadDynamicValue(ref reader); reference.HasStartPhase = true; break;
                    case "endphase": reference.EndPhaseValue = ReadDynamicValue(ref reader); reference.HasEndPhase = true; break;
                    case "preservepose": reference.PreservePose = reader.GetBoolean(); reference.HasPreservePose = true; break;
                    case "blendduration": reference.BlendDurationValue = ReadDynamicValue(ref reader); reference.HasBlendDuration = true; break;
                    case "driverargs": reference.DriverArgs = ReadDriverArgs(ref reader, options); break;
                    case "events": reference.Events = JsonSerializer.Deserialize<List<AnimationEventConfig>>(ref reader, options); break;
                    case "oncomplete": reference.OnComplete = JsonSerializer.Deserialize<OnCompleteAction>(ref reader, options); break;
                    case "oninterrupt": reference.OnInterrupt = JsonSerializer.Deserialize<OnCompleteAction>(ref reader, options); break;
                    case "rootmotion":
                        try {
                            reference.RootMotion = JsonSerializer.Deserialize<RootMotionConfig>(ref reader, options);
                        }
                        catch (JsonException) {
                            // RootMotion 配置格式错误，跳过并使用默认值（无根运动）
                            reader.Skip();
                        }
                        break;
                    case "rootbonerotation":
                        reference.RootBoneRotationValue = ReadRootBoneRotationValue(ref reader, options);
                        reference.HasRootBoneRotation = true;
                        break;
                    case "rootbonetranslation":
                        reference.RootBoneTranslation = ReadVector3Nullable(ref reader, options);
                        reference.HasRootBoneTranslation = true;
                        break;
                    default:
                        reader.Skip(); break;
                }
            }
            return reference;
        }

        public override void Write(Utf8JsonWriter writer, AnimationReference value, JsonSerializerOptions options) {
            // HandleNull=true 时 Write 可能收到 null（序列化路径，配置通常不序列化，保险处理）
            if (value == null) {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStartObject();
            if (!string.IsNullOrEmpty(value.Source)) {
                writer.WriteString("source", value.Source);
            }
            WriteDynamicValue(writer, "speed", value.SpeedValue);
            WriteDynamicValue(writer, "loop", value.LoopValue);
            WriteDynamicValue(writer, "startPhase", value.StartPhaseValue);
            WriteDynamicValue(writer, "endPhase", value.EndPhaseValue);
            if (value.PreservePose) {
                writer.WritePropertyName("preservePose");
                writer.WriteBooleanValue(true);
            }
            WriteDynamicValue(writer, "blendDuration", value.BlendDurationValue);
            if (value.DriverArgs != null
                && value.DriverArgs.Count > 0) {
                writer.WritePropertyName("driverArgs");
                JsonSerializer.Serialize(writer, value.DriverArgs, options);
            }
            if (value.Events != null
                && value.Events.Count > 0) {
                writer.WritePropertyName("events");
                JsonSerializer.Serialize(writer, value.Events, options);
            }
            if (value.OnComplete != null) {
                writer.WritePropertyName("onComplete");
                JsonSerializer.Serialize(writer, value.OnComplete, options);
            }
            if (value.OnInterrupt != null) {
                writer.WritePropertyName("onInterrupt");
                JsonSerializer.Serialize(writer, value.OnInterrupt, options);
            }
            if (value.RootMotion != null) {
                writer.WritePropertyName("rootMotion");
                JsonSerializer.Serialize(writer, value.RootMotion, options);
            }
            if (value.HasRootBoneRotation) {
                writer.WritePropertyName("rootBoneRotation");
                WriteRootBoneRotationValue(writer, value.RootBoneRotationValue);
            }
            if (value.HasRootBoneTranslation && value.RootBoneTranslation.HasValue) {
                writer.WritePropertyName("rootBoneTranslation");
                JsonSerializer.Serialize(writer, value.RootBoneTranslation.Value, options);
            }
            writer.WriteEndObject();
        }

        public object ReadDynamicValue(ref Utf8JsonReader reader) {
            return reader.TokenType switch {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt32(out int intVal) ? intVal : reader.GetSingle(),
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Null => null,
                _ => null
            };
        }

        public void WriteDynamicValue(Utf8JsonWriter writer, string propertyName, object value) {
            if (value == null) {
                return;
            }
            writer.WritePropertyName(propertyName);
            switch (value) {
                case string s: writer.WriteStringValue(s); break;
                case float f: writer.WriteNumberValue(f); break;
                case int i: writer.WriteNumberValue(i); break;
                case double d: writer.WriteNumberValue(d); break;
                case bool b: writer.WriteBooleanValue(b); break;
                default: writer.WriteStringValue(value.ToString()); break;
            }
        }

        private object ReadRootBoneRotationValue(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            switch (reader.TokenType) {
                case JsonTokenType.Number:
                    return reader.TryGetInt32(out int intVal) ? intVal : (object)reader.GetSingle();
                case JsonTokenType.StartArray:
                case JsonTokenType.StartObject:
                    return JsonSerializer.Deserialize<Vector3>(ref reader, options);
                default:
                    throw new JsonException($"Invalid rootBoneRotation token: {reader.TokenType} (expected number, array, or object)");
            }
        }

        private Vector3? ReadVector3Nullable(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return null;
            }
            if (reader.TokenType == JsonTokenType.StartArray
                || reader.TokenType == JsonTokenType.StartObject) {
                return JsonSerializer.Deserialize<Vector3>(ref reader, options);
            }
            throw new JsonException($"Invalid rootBoneTranslation token: {reader.TokenType} (expected array or object)");
        }

        private void WriteRootBoneRotationValue(Utf8JsonWriter writer, object value) {
            switch (value) {
                case float f: writer.WriteNumberValue(f); break;
                case int i: writer.WriteNumberValue(i); break;
                case double d: writer.WriteNumberValue(d); break;
                case Vector3 v:
                    if (v.X == 0f && v.Z == 0f) {
                        writer.WriteNumberValue(v.Y);
                    }
                    else {
                        writer.WriteStartArray();
                        writer.WriteNumberValue(v.X);
                        writer.WriteNumberValue(v.Y);
                        writer.WriteNumberValue(v.Z);
                        writer.WriteEndArray();
                    }
                    break;
                default: throw new InvalidOperationException($"Unexpected rootBoneRotation value type: {value?.GetType().Name ?? "null"}");
            }
        }

        public Dictionary<string, object> ReadDriverArgs(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                return null;
            }
            Dictionary<string, object> dict = new();
            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    break;
                }
                if (reader.TokenType != JsonTokenType.PropertyName) {
                    continue;
                }
                string key = reader.GetString();
                reader.Read();
                object value = reader.TokenType switch {
                    JsonTokenType.String => reader.GetString(),
                    JsonTokenType.Number => reader.TryGetInt32(out int intVal) ? intVal : reader.GetSingle(),
                    JsonTokenType.True => true,
                    JsonTokenType.False => false,
                    JsonTokenType.Null => null,
                    JsonTokenType.StartObject => JsonElement.ParseValue(ref reader),
                    JsonTokenType.StartArray => JsonElement.ParseValue(ref reader),
                    _ => null
                };
                dict[key] = value;
            }
            return dict;
        }
    }
}