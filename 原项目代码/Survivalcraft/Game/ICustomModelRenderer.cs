using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game {
    /// <summary>
    /// 自定义模型渲染器接口
    /// 模组实现此接口以提供自定义渲染（如 PBR）
    /// 每个模型按 mesh part 独立分类到不同渲染队列
    /// </summary>
    public interface ICustomModelRenderer : IDisposable {
        /// <summary>
        /// 初始化渲染器。需要在模组中自行调用
        /// </summary>
        void Initialize(SubsystemModelsRenderer subsystemModelsRenderer);

        /// <summary>
        /// 开始新帧：准备渲染队列 + 设置光源 + 更新 UBO
        /// 在所有模型 PrepareModel 完成后调用
        /// </summary>
        void BeginFrame(Camera camera, List<SubsystemModelsRenderer.ModelData> allModels);

        /// <summary>
        /// drawOrder 1 调用：scatter pass + opaque pass + 排队阴影 + DrawExtras
        /// 自定义渲染器全权管理 GL 状态和渲染
        /// </summary>
        void RenderOpaquePass();

        /// <summary>
        /// drawOrder 201 调用两次(underwater=false/true)：捕获 transmission FBO，
        /// 合并所有透明条目并统一 back-to-front 排序渲染。
        /// 实现可在第一次调用时完成全部工作，第二次调用时 no-op。
        /// </summary>
        /// <param name="underwater">是否为水后透明物体 pass（实现可忽略此参数）</param>
        void RenderTransparentPass(bool underwater);

        /// <summary>
        /// 当前激活的方向光方向（世界空间）
        /// 引擎用于计算太阳遮挡 raycast
        /// </summary>
        Vector3 ActiveLightDirection { get; }
    }
}
