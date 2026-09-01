namespace Engine.Animation {
    /// <summary>
    /// Expression evaluator - unified handling of condition expressions and property expressions.
    /// Uses NCalc for expression parsing and evaluation with caching for performance.
    /// </summary>
    public class ExpressionEvaluator {
        /// <summary>
        /// Cache of compiled expressions.
        /// </summary>
        public readonly Dictionary<string, Expression> m_compiledExpressions = new();

        /// <summary>
        /// Cache of required parameter names per expression to avoid re-extraction.
        /// </summary>
        public readonly Dictionary<string, string[]> m_requiredParameters = new();

        /// <summary>
        /// Evaluate a boolean expression (for state conditions).
        /// </summary>
        /// <param name="expression">Expression string</param>
        /// <param name="parameters">Parameter container</param>
        /// <param name="defaultValue">Default value if evaluation fails</param>
        /// <returns>Boolean result</returns>
        public bool EvaluateBool(string expression, AnimationParameters parameters, bool defaultValue = false) {
            object result = Evaluate<object>(expression, parameters, defaultValue);
            return Convert.ToBoolean(result);
        }

        /// <summary>
        /// Evaluate a float expression (for speed, duration, etc.).
        /// </summary>
        /// <param name="expression">Expression string</param>
        /// <param name="parameters">Parameter container</param>
        /// <param name="defaultValue">Default value if evaluation fails</param>
        /// <returns>Float result</returns>
        public float EvaluateFloat(string expression, AnimationParameters parameters, float defaultValue = 0f) {
            object result = Evaluate<object>(expression, parameters, defaultValue);
            return Convert.ToSingle(result);
        }

        /// <summary>
        /// Evaluate an integer expression.
        /// </summary>
        /// <param name="expression">Expression string</param>
        /// <param name="parameters">Parameter container</param>
        /// <param name="defaultValue">Default value if evaluation fails</param>
        /// <returns>Integer result</returns>
        public int EvaluateInt(string expression, AnimationParameters parameters, int defaultValue = 0) {
            object result = Evaluate<object>(expression, parameters, defaultValue);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Generic expression evaluation.
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="expression">Expression string</param>
        /// <param name="parameters">Parameter container</param>
        /// <param name="defaultValue">Default value if evaluation fails</param>
        /// <returns>Evaluated result converted to target type</returns>
        public T Evaluate<T>(string expression, AnimationParameters parameters, T defaultValue) {
            if (string.IsNullOrEmpty(expression)) {
                return defaultValue;
            }
            try {
                // Special case: if expression is a pure parameter name, return its value directly
                if (parameters.HasParameter(expression)) {
                    object paramValue = parameters.GetValue(expression);
                    return ConvertResult<T>(paramValue);
                }

                // Get or compile expression
                Expression expr = GetOrCompileExpression(expression);

                // Bind parameters
                BindParameters(expression, expr, parameters);

                // Evaluate
                object result = expr.Evaluate();
                return ConvertResult<T>(result);
            }
            catch (Exception ex) {
                Log.Error($"[ExpressionEvaluator] Expression: {expression}, Error: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Check if a string value is an expression.
        /// An expression is detected if:
        /// 1. It starts with "expr:" prefix
        /// 2. It contains parameter reference [ParameterName]
        /// </summary>
        /// <param name="value">String value to check</param>
        /// <returns>True if the value is an expression</returns>
        public static bool IsExpression(string value) {
            if (string.IsNullOrEmpty(value)) {
                return false;
            }

            // Explicit declaration with expr: prefix
            if (value.StartsWith("expr:")) {
                return true;
            }

            // Contains parameter reference [ParameterName]
            // This is the primary indicator - most expressions will reference parameters
            if (value.Contains("[")
                && value.Contains("]")) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Strip the expr: prefix from an expression string.
        /// </summary>
        /// <param name="expression">Expression string (may have expr: prefix)</param>
        /// <returns>Expression without prefix</returns>
        public static string StripPrefix(string expression) {
            if (string.IsNullOrEmpty(expression)) {
                return expression;
            }
            if (expression.StartsWith("expr:")) {
                return expression.Substring(5);
            }
            return expression;
        }

        /// <summary>
        /// Get the list of parameter names required by an expression.
        /// </summary>
        /// <param name="expression">Expression string</param>
        /// <returns>Array of required parameter names</returns>
        public string[] GetRequiredParameters(string expression) {
            if (string.IsNullOrEmpty(expression)) {
                return Array.Empty<string>();
            }
            GetOrCompileExpression(expression);
            return m_requiredParameters.TryGetValue(expression, out string[] params_) ? params_ : Array.Empty<string>();
        }

        /// <summary>
        /// Clear the compilation cache.
        /// </summary>
        public void ClearCache() {
            // Remove event handlers to avoid memory leaks
            foreach (KeyValuePair<string, Expression> kvp in m_compiledExpressions) {
                AnimationExpressionFunctions.UnregisterFunctions(kvp.Value);
            }
            m_compiledExpressions.Clear();
            m_requiredParameters.Clear();
        }

        /// <summary>
        /// Get or compile an expression.
        /// </summary>
        public Expression GetOrCompileExpression(string expression) {
            // Strip expr: prefix if present
            string normalizedExpr = StripPrefix(expression);
            string cacheKey = normalizedExpr;
            if (!m_compiledExpressions.TryGetValue(cacheKey, out Expression expr)) {
                expr = new Expression(normalizedExpr);
                m_compiledExpressions[cacheKey] = expr;

                // Extract and cache parameter names
                List<string> paramNames = expr.GetParameterNames();
                m_requiredParameters[cacheKey] = paramNames?.ToArray() ?? Array.Empty<string>();

                // Register custom functions once per expression
                AnimationExpressionFunctions.RegisterFunctions(expr);
            }
            return expr;
        }

        /// <summary>
        /// Bind parameters to an expression.
        /// </summary>
        public void BindParameters(string expression, Expression expr, AnimationParameters parameters) {
            string normalizedExpr = StripPrefix(expression);
            string cacheKey = normalizedExpr;
            string[] requiredParams = m_requiredParameters.TryGetValue(cacheKey, out string[] params_) ? params_ : Array.Empty<string>();
            if (requiredParams.Length == 0) {
                expr.Parameters.Clear();
                return;
            }

            foreach (string paramName in requiredParams) {
                expr.Parameters[paramName] = parameters.GetValue(paramName);
            }
        }

        /// <summary>
        /// Convert result to target type.
        /// </summary>
        public static T ConvertResult<T>(object result) {
            Type targetType = typeof(T);
            if (result == null) {
                return default;
            }
            if (result is T typed) {
                return typed;
            }
            if (targetType == typeof(bool)) {
                return (T)(object)Convert.ToBoolean(result);
            }
            if (targetType == typeof(float)) {
                return (T)(object)Convert.ToSingle(result);
            }
            if (targetType == typeof(int)) {
                return (T)(object)Convert.ToInt32(result);
            }
            if (targetType == typeof(double)) {
                return (T)(object)Convert.ToDouble(result);
            }
            return (T)Convert.ChangeType(result, targetType);
        }
    }
}