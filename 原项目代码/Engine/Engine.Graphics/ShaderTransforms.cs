namespace Engine.Graphics {
    public class ShaderTransforms {
        public Matrix[] m_world;

        public Matrix m_view = Matrix.Identity;

        public Matrix m_projection = Matrix.Identity;

        public Matrix[] m_worldView;

        public Matrix m_viewProjection = Matrix.Identity;

        public Matrix[] m_worldViewProjection;

        public int MaxWorldMatrices => m_world.Length;

        public Matrix[] World => m_world;

        public Matrix View {
            get => m_view;
            set => m_view = value;
        }

        public Matrix Projection {
            get => m_projection;
            set => m_projection = value;
        }

        public Matrix ViewProjection => m_viewProjection;

        public Matrix[] WorldView => m_worldView;

        public Matrix[] WorldViewProjection => m_worldViewProjection;

        public ShaderTransforms(int maxWorldMatrices) {
            m_world = new Matrix[maxWorldMatrices];
            m_worldView = new Matrix[maxWorldMatrices];
            m_worldViewProjection = new Matrix[maxWorldMatrices];
            for (int i = 0; i < maxWorldMatrices; i++) {
                m_world[i] = Matrix.Identity;
                m_worldView[i] = Matrix.Identity;
                m_worldViewProjection[i] = Matrix.Identity;
            }
        }

        public virtual void UpdateMatrices(int count, bool worldView, bool viewProjection, bool worldViewProjection) {
            if (count < 1
                || count > MaxWorldMatrices) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (worldView) {
                for (int i = 0; i < count; i++) {
                    Matrix.MultiplyRestricted(ref m_world[i], ref m_view, out m_worldView[i]);
                }
            }
            if (viewProjection) {
                Matrix.MultiplyRestricted(ref m_view, ref m_projection, out m_viewProjection);
            }
            if (!worldViewProjection) {
                return;
            }
            if (worldView) {
                for (int j = 0; j < count; j++) {
                    Matrix.MultiplyRestricted(ref m_worldView[j], ref m_projection, out m_worldViewProjection[j]);
                }
                return;
            }
            if (!viewProjection) {
                Matrix.MultiplyRestricted(ref m_view, ref m_projection, out m_viewProjection);
            }
            for (int k = 0; k < count; k++) {
                Matrix.MultiplyRestricted(ref m_world[k], ref m_viewProjection, out m_worldViewProjection[k]);
            }
        }
    }
}