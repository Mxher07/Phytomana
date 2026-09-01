namespace Engine.Graphics {
    public abstract class BaseBatch {
        public int Layer { get; set; }

        public DepthStencilState DepthStencilState { get; set; }

        public RasterizerState RasterizerState { get; set; }

        public BlendState BlendState { get; set; }

        public abstract bool IsEmpty();

        public abstract void Clear();

        public abstract void Flush(Matrix matrix, Vector4 color, bool clearAfterFlush = true);
    }
}