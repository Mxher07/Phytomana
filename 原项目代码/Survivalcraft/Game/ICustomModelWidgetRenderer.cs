namespace Game {
    public interface ICustomModelWidgetRenderer : IDisposable {
        void Initialize();

        void Render(ModelWidgetRenderContext context);
    }
}
