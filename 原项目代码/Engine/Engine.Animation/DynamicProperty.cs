namespace Engine.Animation {
    /// <summary>
    /// Dynamic property that supports both static values and expressions.
    /// Used for animation properties like speed, loop, blendDuration, etc.
    /// </summary>
    /// <typeparam name="T">The type of the property value</typeparam>
    public class DynamicProperty<T> {
        public readonly T m_staticValue;
        public readonly string m_expression;
        public readonly bool m_isExpression;

        /// <summary>
        /// Create a dynamic property from a configuration value.
        /// </summary>
        /// <param name="value">
        /// Static value (int, float, bool, string) or expression string.
        /// Expression format: contains [Parameter] reference or starts with "expr:"
        /// </param>
        public DynamicProperty(object value) {
            if (value == null) {
                m_staticValue = default;
                m_isExpression = false;
                return;
            }
            if (value is string str
                && ExpressionEvaluator.IsExpression(str)) {
                m_expression = ExpressionEvaluator.StripPrefix(str);
                m_isExpression = true;
            }
            else {
                m_staticValue = ConvertValue(value);
                m_isExpression = false;
            }
        }

        /// <summary>
        /// public constructor for creating static/expression properties directly.
        /// </summary>
        public DynamicProperty(T staticValue, string expression, bool isExpression) {
            m_staticValue = staticValue;
            m_expression = expression;
            m_isExpression = isExpression;
        }

        /// <summary>
        /// Get the current value (static or dynamically computed).
        /// </summary>
        /// <param name="parameters">Animation parameters for expression evaluation</param>
        /// <param name="evaluator">Expression evaluator instance</param>
        /// <returns>The property value</returns>
        public T GetValue(AnimationParameters parameters, ExpressionEvaluator evaluator) {
            if (!m_isExpression) {
                return m_staticValue;
            }
            if (evaluator == null) {
                return m_staticValue;
            }
            return evaluator.Evaluate(m_expression, parameters, m_staticValue);
        }

        /// <summary>
        /// Check if this property is an expression (dynamic).
        /// </summary>
        public bool IsExpression => m_isExpression;

        /// <summary>
        /// Get the raw expression string (if this is an expression).
        /// </summary>
        public string Expression => m_isExpression ? m_expression : null;

        /// <summary>
        /// Get the static value (if this is not an expression).
        /// </summary>
        public T StaticValue => m_staticValue;

        /// <summary>
        /// Create a static property.
        /// </summary>
        /// <param name="value">The static value</param>
        /// <returns>A new DynamicProperty with the static value</returns>
        public static DynamicProperty<T> Static(T value) => new(value, null, false);

        /// <summary>
        /// Create an expression property.
        /// </summary>
        /// <param name="expression">The expression string</param>
        /// <returns>A new DynamicProperty with the expression</returns>
        public static DynamicProperty<T> FromExpression(string expression) => new(default, expression, true);

        /// <summary>
        /// Implicit conversion from T to DynamicProperty&lt;T&gt;.
        /// </summary>
        public static implicit operator DynamicProperty<T>(T value) => Static(value);

        /// <summary>
        /// Convert an object value to the target type T.
        /// </summary>
        public static T ConvertValue(object value) {
            Type targetType = typeof(T);
            if (value is T typed) {
                return typed;
            }

            // Handle numeric conversions
            if (targetType == typeof(float)) {
                if (value is int i) {
                    return (T)(object)(float)i;
                }
                if (value is double d) {
                    return (T)(object)(float)d;
                }
                if (value is decimal dec) {
                    return (T)(object)(float)dec;
                }
            }
            if (targetType == typeof(int)) {
                if (value is float f) {
                    return (T)(object)(int)f;
                }
                if (value is double d) {
                    return (T)(object)(int)d;
                }
            }
            if (targetType == typeof(double)) {
                if (value is int i) {
                    return (T)(object)(double)i;
                }
                if (value is float f) {
                    return (T)(object)(double)f;
                }
            }

            // Handle bool conversion
            if (targetType == typeof(bool)) {
                return (T)(object)Convert.ToBoolean(value);
            }

            // Handle JsonElement (from System.Text.Json)
            if (value is System.Text.Json.JsonElement jsonElement) {
                return ConvertJsonElement(jsonElement, targetType);
            }
            return (T)Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Convert a JsonElement to the target type.
        /// </summary>
        public static T ConvertJsonElement(System.Text.Json.JsonElement element, Type targetType) {
            if (targetType == typeof(float)) {
                return (T)(object)element.GetSingle();
            }
            if (targetType == typeof(int)) {
                return (T)(object)element.GetInt32();
            }
            if (targetType == typeof(double)) {
                return (T)(object)element.GetDouble();
            }
            if (targetType == typeof(bool)) {
                return (T)(object)element.GetBoolean();
            }
            if (targetType == typeof(string)) {
                return (T)(object)element.GetString();
            }
            return default;
        }

        /// <summary>
        /// String representation for debugging.
        /// </summary>
        public override string ToString() {
            if (m_isExpression) {
                return $"expr:{m_expression}";
            }
            return m_staticValue?.ToString() ?? "null";
        }
    }
}