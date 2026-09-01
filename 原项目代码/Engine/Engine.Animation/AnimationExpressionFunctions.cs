namespace Engine.Animation {
    /// <summary>
    /// 动画表达式自定义函数
    /// 使用静态方法避免每次调用时创建委托
    /// </summary>
    public static class AnimationExpressionFunctions {
        /// <summary>
        /// 预定义的函数委托，避免每次调用时创建
        /// </summary>
        public static readonly EvaluateFunctionHandler s_evaluateFunctionHandler = EvaluateFunction;

        /// <summary>
        /// 注册自定义函数到表达式对象
        /// </summary>
        public static void RegisterFunctions(Expression expression) {
            expression.EvaluateFunction += s_evaluateFunctionHandler;
        }

        /// <summary>
        /// 从表达式对象移除自定义函数
        /// </summary>
        public static void UnregisterFunctions(Expression expression) {
            expression.EvaluateFunction -= s_evaluateFunctionHandler;
        }

        public static void EvaluateFunction(string name, FunctionEventArgs args) {
            switch (name.ToLowerInvariant()) {
                case "lerp": {
                    float a = Convert.ToSingle(args.Parameters.Evaluate(0));
                    float b = Convert.ToSingle(args.Parameters.Evaluate(1));
                    float t = Convert.ToSingle(args.Parameters.Evaluate(2));
                    args.Result = a + (b - a) * t;
                }
                    break;
                case "smoothstep": {
                    float t = Convert.ToSingle(args.Parameters.Evaluate(0));
                    args.Result = t * t * (3 - 2 * t);
                }
                    break;
                case "degtorad": {
                    args.Result = Convert.ToSingle(args.Parameters.Evaluate(0)) * MathF.PI / 180f;
                }
                    break;
                case "radtodeg": {
                    args.Result = Convert.ToSingle(args.Parameters.Evaluate(0)) * 180f / MathF.PI;
                }
                    break;
                case "clamp": {
                    args.Result = Math.Clamp(Convert.ToSingle(args.Parameters.Evaluate(0)),
                                             Convert.ToSingle(args.Parameters.Evaluate(1)),
                                             Convert.ToSingle(args.Parameters.Evaluate(2)));
                }
                    break;
                case "pi": args.Result = MathF.PI; break;
            }
        }
    }
}