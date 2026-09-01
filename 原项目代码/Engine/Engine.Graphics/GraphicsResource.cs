namespace Engine.Graphics {
    public abstract class GraphicsResource : IDisposable {
        public static HashSet<GraphicsResource> m_resources = [];

        public bool m_isDisposed;

        public GraphicsResource() {
            m_resources.Add(this);
        }

        ~GraphicsResource() {
            Dispatcher.Dispatch(delegate { Dispose(); });
        }

        public virtual void Dispose() {
            m_isDisposed = true;
            m_resources.Remove(this);
        }

        public abstract int GetGpuMemoryUsage();

        public abstract void HandleDeviceLost();

        public abstract void HandleDeviceReset();

        public void VerifyNotDisposed() {
            if (m_isDisposed) {
                throw new InvalidOperationException("GraphicsResource is disposed.");
            }
        }
    }
}