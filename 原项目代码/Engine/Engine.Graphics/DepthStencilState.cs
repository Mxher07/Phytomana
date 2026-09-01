namespace Engine.Graphics {
    public class DepthStencilState : LockOnFirstUse {
        bool m_depthBufferTestEnable = true;

        bool m_depthBufferWriteEnable = true;

        CompareFunction m_depthBufferFunction = CompareFunction.LessEqual;

        public static readonly DepthStencilState Default = new() { IsLocked = true };

        public static readonly DepthStencilState DepthRead = new() { DepthBufferWriteEnable = false, IsLocked = true };

        public static readonly DepthStencilState DepthWrite = new() { DepthBufferTestEnable = false, IsLocked = true };

        public static readonly DepthStencilState None = new() { DepthBufferTestEnable = false, DepthBufferWriteEnable = false, IsLocked = true };

        public bool DepthBufferTestEnable {
            get => m_depthBufferTestEnable;
            set {
                ThrowIfLocked();
                m_depthBufferTestEnable = value;
            }
        }

        public bool DepthBufferWriteEnable {
            get => m_depthBufferWriteEnable;
            set {
                ThrowIfLocked();
                m_depthBufferWriteEnable = value;
            }
        }

        public CompareFunction DepthBufferFunction {
            get => m_depthBufferFunction;
            set {
                ThrowIfLocked();
                m_depthBufferFunction = value;
            }
        }
    }
}