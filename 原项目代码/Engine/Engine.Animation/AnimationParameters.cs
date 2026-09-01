namespace Engine.Animation {
    /// <summary>
    /// 参数容器类，用于存储动画系统需要的各种参数。
    /// 支持脏检查，避免无变化时重复评估状态规则。
    /// </summary>
    public class AnimationParameters {
        public readonly Dictionary<string, float> m_floatParams = new();
        public readonly Dictionary<string, bool> m_boolParams = new();
        public readonly Dictionary<string, Vector3> m_vector3Params = new();
        public readonly Dictionary<string, Vector2> m_vector2Params = new();
        public readonly Dictionary<string, string> m_stringParams = new();

        // 脏标记：有参数变化时设为 true
        public bool m_isDirty = true;

        /// <summary>
        /// 是否有参数变化（脏标记）
        /// </summary>
        public bool IsDirty => m_isDirty;

        /// <summary>
        /// 清除脏标记（在评估完状态规则后调用）
        /// </summary>
        public void ClearDirty() => m_isDirty = false;

        /// <summary>
        /// 设置脏标记（用于强制重新评估状态规则）
        /// </summary>
        public void SetDirty() => m_isDirty = true;

        public void SetFloat(string name, float value) {
            // 检查值是否变化
            if (m_floatParams.TryGetValue(name, out float existing)
                && existing == value) {
                return; // 值未变化，不设置脏标记
            }
            m_floatParams[name] = value;
            m_isDirty = true;
        }

        public void SetBool(string name, bool value) {
            if (m_boolParams.TryGetValue(name, out bool existing)
                && existing == value) {
                return;
            }
            m_boolParams[name] = value;
            m_isDirty = true;
        }

        public void SetVector3(string name, Vector3 value) {
            if (m_vector3Params.TryGetValue(name, out Vector3 existing)
                && existing == value) {
                return;
            }
            m_vector3Params[name] = value;
            m_isDirty = true;
        }

        public void SetVector2(string name, Vector2 value) {
            if (m_vector2Params.TryGetValue(name, out Vector2 existing)
                && existing == value) {
                return;
            }
            m_vector2Params[name] = value;
            m_isDirty = true;
        }

        public void SetString(string name, string value) {
            if (m_stringParams.TryGetValue(name, out string existing)
                && existing == value) {
                return;
            }
            m_stringParams[name] = value;
            m_isDirty = true;
        }

        /// <summary>
        /// 设置参数值（通用方法，根据类型自动分发）
        /// </summary>
        public void SetParameter(string name, object value) {
            if (value is float f) {
                SetFloat(name, f);
            }
            else if (value is bool b) {
                SetBool(name, b);
            }
            else if (value is Vector3 v) {
                SetVector3(name, v);
            }
            else if (value is int i) {
                SetFloat(name, i);
            }
            else if (value is double d) {
                SetFloat(name, (float)d);
            }
            else if (value is System.Text.Json.JsonElement jsonElement) {
                // 处理 JsonElement 类型
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number) {
                    SetFloat(name, jsonElement.GetSingle());
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.True
                    || jsonElement.ValueKind == System.Text.Json.JsonValueKind.False) {
                    SetBool(name, jsonElement.GetBoolean());
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String) {
                    SetFloat(name, float.Parse(jsonElement.GetString()));
                }
            }
            else if (value != null) {
                SetFloat(name, Convert.ToSingle(value));
            }
        }

        public float GetFloat(string name) => m_floatParams.TryGetValue(name, out float v) ? v : 0f;
        public bool GetBool(string name) => m_boolParams.TryGetValue(name, out bool v) ? v : false;
        public Vector3 GetVector3(string name) => m_vector3Params.TryGetValue(name, out Vector3 v) ? v : Vector3.Zero;
        public Vector2 GetVector2(string name) => m_vector2Params.TryGetValue(name, out Vector2 v) ? v : Vector2.Zero;
        public string GetString(string name) => m_stringParams.TryGetValue(name, out string v) ? v : string.Empty;

        /// <summary>
        /// 按名称获取参数值（通用方法）
        /// </summary>
        public object GetValue(string name) {
            if (m_floatParams.TryGetValue(name, out float f)) {
                return f;
            }
            if (m_boolParams.TryGetValue(name, out bool b)) {
                return b;
            }
            if (m_vector3Params.TryGetValue(name, out Vector3 v)) {
                return v;
            }
            if (m_vector2Params.TryGetValue(name, out Vector2 v2)) {
                return v2;
            }
            if (m_stringParams.TryGetValue(name, out string s)) {
                return s;
            }
            return 0;
        }

        /// <summary>
        /// 尝试获取 float 参数
        /// </summary>
        public bool TryGetFloat(string name, out float value) => m_floatParams.TryGetValue(name, out value);

        /// <summary>
        /// 尝试获取 bool 参数
        /// </summary>
        public bool TryGetBool(string name, out bool value) => m_boolParams.TryGetValue(name, out value);

        public bool HasParameter(string name) => m_floatParams.ContainsKey(name)
            || m_boolParams.ContainsKey(name)
            || m_vector3Params.ContainsKey(name)
            || m_vector2Params.ContainsKey(name)
            || m_stringParams.ContainsKey(name);

        /// <summary>
        /// 获取所有参数用于表达式绑定
        /// </summary>
        public Dictionary<string, object> GetAllParameters() {
            Dictionary<string, object> result = new();
            foreach (KeyValuePair<string, float> kvp in m_floatParams) {
                result[kvp.Key] = kvp.Value;
            }
            foreach (KeyValuePair<string, bool> kvp in m_boolParams) {
                result[kvp.Key] = kvp.Value;
            }
            foreach (KeyValuePair<string, Vector3> kvp in m_vector3Params) {
                result[kvp.Key] = kvp.Value;
            }
            foreach (KeyValuePair<string, Vector2> kvp in m_vector2Params) {
                result[kvp.Key] = kvp.Value;
            }
            foreach (KeyValuePair<string, string> kvp in m_stringParams) {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }
    }
}