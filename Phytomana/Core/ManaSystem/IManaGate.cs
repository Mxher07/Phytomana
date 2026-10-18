namespace Phytomana.Api {
    /// <summary>
    /// 供魔门：接收器在「任务尚未开始」（如结构无效、材料未齐）时拒绝法杖链路供魔——
    /// 即使发射器已绑定也不注入魔力；合成可以开始时门才打开。
    /// 未实现的接收器（魔力池 / 发射器等）保持原有供魔行为。
    /// </summary>
    public interface IManaGate {
        bool AcceptsMana();
    }
}
