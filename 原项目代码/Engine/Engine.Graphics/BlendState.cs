namespace Engine.Graphics {
    public class BlendState : LockOnFirstUse {
        public BlendFunction m_alphaBlendFunction;

        public Blend m_alphaSourceBlend = Blend.One;

        public Blend m_alphaDestinationBlend;

        public BlendFunction m_colorBlendFunction;

        public Blend m_colorSourceBlend = Blend.One;

        public Blend m_colorDestinationBlend;

        public Vector4 m_blendFactor = Vector4.Zero;

        public static readonly BlendState Opaque = new() { IsLocked = true };

        public static readonly BlendState Additive = new() {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.One,
            IsLocked = true
        };

        public static readonly BlendState AlphaBlend = new() {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            IsLocked = true
        };

        public static readonly BlendState NonPremultiplied = new() {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
            IsLocked = true
        };

        public BlendFunction AlphaBlendFunction {
            get => m_alphaBlendFunction;
            set {
                ThrowIfLocked();
                m_alphaBlendFunction = value;
            }
        }

        public Blend AlphaSourceBlend {
            get => m_alphaSourceBlend;
            set {
                ThrowIfLocked();
                m_alphaSourceBlend = value;
            }
        }

        public Blend AlphaDestinationBlend {
            get => m_alphaDestinationBlend;
            set {
                ThrowIfLocked();
                m_alphaDestinationBlend = value;
            }
        }

        public BlendFunction ColorBlendFunction {
            get => m_colorBlendFunction;
            set {
                ThrowIfLocked();
                m_colorBlendFunction = value;
            }
        }

        public Blend ColorSourceBlend {
            get => m_colorSourceBlend;
            set {
                ThrowIfLocked();
                m_colorSourceBlend = value;
            }
        }

        public Blend ColorDestinationBlend {
            get => m_colorDestinationBlend;
            set {
                ThrowIfLocked();
                m_colorDestinationBlend = value;
            }
        }

        public Vector4 BlendFactor {
            get => m_blendFactor;
            set {
                ThrowIfLocked();
                m_blendFactor = value;
            }
        }

        public bool BaseEquals(object obj) {
            if (obj is not BlendState blendState) {
                return false;
            }
            return m_colorSourceBlend == blendState.m_colorSourceBlend
                && m_colorDestinationBlend == blendState.m_colorDestinationBlend
                && m_alphaSourceBlend == blendState.m_alphaSourceBlend
                && m_alphaDestinationBlend == blendState.m_alphaDestinationBlend;
        }
    }
}