namespace Phytomana.Api {
    /// <summary>
    /// 花朵状态机状态。Idle=待机，Working=工作中，Sleep=休眠（基座无效），Dead=死亡（终态，不再参与调度）。
    /// </summary>
    public enum FlowerState {
        Idle,
        Working,
        Sleep,
        Dead
    }
}
