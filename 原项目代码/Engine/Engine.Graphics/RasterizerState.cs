namespace Engine.Graphics {
    public class RasterizerState : LockOnFirstUse {
        CullMode m_cullMode = CullMode.CullCounterClockwise;

        bool m_scissorTestEnable;

        float m_depthBias;

        float m_slopeScaleDepthBias;

        public static readonly RasterizerState CullNone = new() { CullMode = CullMode.None, IsLocked = true };

        public static readonly RasterizerState CullNoneScissor = new() { CullMode = CullMode.None, ScissorTestEnable = true, IsLocked = true };

        public static readonly RasterizerState CullClockwise = new() { CullMode = CullMode.CullClockwise, IsLocked = true };

        public static readonly RasterizerState CullClockwiseScissor = new() {
            CullMode = CullMode.CullClockwise, ScissorTestEnable = true, IsLocked = true
        };

        public static readonly RasterizerState CullCounterClockwise = new() { CullMode = CullMode.CullCounterClockwise, IsLocked = true };

        public static readonly RasterizerState CullCounterClockwiseScissor = new() {
            CullMode = CullMode.CullCounterClockwise, ScissorTestEnable = true, IsLocked = true
        };

        public CullMode CullMode {
            get => m_cullMode;
            set {
                ThrowIfLocked();
                m_cullMode = value;
            }
        }

        public bool ScissorTestEnable {
            get => m_scissorTestEnable;
            set {
                ThrowIfLocked();
                m_scissorTestEnable = value;
            }
        }

        public float DepthBias {
            get => m_depthBias;
            set {
                ThrowIfLocked();
                m_depthBias = value;
            }
        }

        public float SlopeScaleDepthBias {
            get => m_slopeScaleDepthBias;
            set {
                ThrowIfLocked();
                m_slopeScaleDepthBias = value;
            }
        }
    }
}