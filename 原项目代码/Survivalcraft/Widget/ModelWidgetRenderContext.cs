using Engine;
using Engine.Graphics;

namespace Game {
    public sealed class ModelWidgetRenderContext {
        private readonly ModelWidget m_widget;

        public IReadOnlyList<Model> Models { get; }

        public Matrix ViewMatrix { get; }

        public Matrix ProjectionMatrix { get; }

        public Matrix ModelTransform { get; }

        public Color Color { get; }

        public bool UseAlphaThreshold { get; }

        internal ModelWidgetRenderContext(
            ModelWidget widget,
            Matrix viewMatrix,
            Matrix projectionMatrix,
            Matrix modelTransform,
            Color color,
            bool useAlphaThreshold
        ) {
            m_widget = widget;
            Models = Array.AsReadOnly(widget.Models.ToArray());
            ViewMatrix = viewMatrix;
            ProjectionMatrix = projectionMatrix;
            ModelTransform = modelTransform;
            Color = color;
            UseAlphaThreshold = useAlphaThreshold;
        }

        public Matrix GetMeshTransform(Model model, ModelMesh mesh) {
            return m_widget.GetMeshTransform(model, mesh);
        }

        public Texture2D GetTextureOverride(Model model) {
            return m_widget.GetTextureOverride(model);
        }

        public Texture2D GetTexture(Model model, ModelMeshPart meshPart) {
            return m_widget.GetTexture(model, meshPart);
        }

        public int CalculateJointMatrices(Model model, Matrix[] destination) {
            return m_widget.CalculateJointMatrices(model, ModelTransform, destination);
        }

        public void SetupShaderParameters(Shader shader, Model model, ModelMesh mesh) {
            m_widget.OnSetupShaderParameters?.Invoke(m_widget, shader, model, mesh);
        }
    }
}
