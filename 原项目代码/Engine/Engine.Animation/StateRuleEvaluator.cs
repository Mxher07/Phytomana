namespace Engine.Animation {
    /// <summary>
    /// 状态规则求值器，使用 NCalc 评估条件表达式。
    /// 支持表达式缓存和参数优化。
    /// 内部委托给 ExpressionEvaluator 实现。
    /// </summary>
    public class StateRuleEvaluator {
        /// <summary>
        /// 内部表达式求值器
        /// </summary>
        public readonly ExpressionEvaluator m_evaluator = new();

        /// <summary>
        /// 暴露内部求值器供动态属性使用
        /// </summary>
        public ExpressionEvaluator Evaluator => m_evaluator;

        /// <summary>
        /// 评估单个条件表达式。
        /// </summary>
        /// <param name="condition">条件表达式字符串</param>
        /// <param name="parameters">参数容器</param>
        /// <returns>表达式求值结果</returns>
        public bool EvaluateCondition(string condition, AnimationParameters parameters) => m_evaluator.EvaluateBool(condition, parameters);

        /// <summary>
        /// 清除编译缓存。
        /// </summary>
        public void ClearCache() {
            m_evaluator.ClearCache();
        }
    }
}