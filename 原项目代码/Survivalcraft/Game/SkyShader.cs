namespace Engine.Graphics {
    public class SkyShader : TransformedShader {
        public ShaderParameter m_worldViewProjectionMatrixParameter;

        public ShaderParameter m_textureParameter;

        public ShaderParameter m_samplerStateParameter;

        public ShaderParameter m_colorParameter;

        public ShaderParameter m_alphaThresholdParameter;

        public Texture2D Texture {
            set => m_textureParameter.SetValue(value);
        }

        public SamplerState SamplerState {
            set => m_samplerStateParameter.SetValue(value);
        }

        public Vector4 Color {
            set => m_colorParameter.SetValue(value);
        }

        public float AlphaThreshold {
            set => m_alphaThresholdParameter.SetValue(value);
        }

        public SkyShader(string vsc, string psc, bool useVertexColor, bool useTexture, bool useAlphaThreshold) : base(
            vsc,
            psc,
            1,
            PrepareShaderMacros(useVertexColor, useTexture, useAlphaThreshold)
        ) {
            SetParameter();
            Color = Vector4.One;
        }

        public SkyShader(string vsc, string psc, bool useVertexColor, bool useTexture, bool useAlphaThreshold, ShaderMacro[] shaderMacros = null) :
            base(vsc, psc, 1, PrepareShaderMacros(useVertexColor, useTexture, useAlphaThreshold, shaderMacros)) {
            SetParameter();
            Color = Vector4.One;
        }

        public virtual void SetParameter() {
            m_worldViewProjectionMatrixParameter = base.GetParameter("u_worldViewProjectionMatrix", true);
            m_textureParameter = base.GetParameter("u_texture", true);
            m_samplerStateParameter = base.GetParameter("u_samplerState", true);
            m_colorParameter = base.GetParameter("u_color", true);
            m_alphaThresholdParameter = base.GetParameter("u_alphaThreshold", true);
        }

        public override void PrepareForDrawingOverride() {
            Transforms.UpdateMatrices(1, false, false, true);
            m_worldViewProjectionMatrixParameter.SetValue(Transforms.WorldViewProjection, 1);
        }

        public static ShaderMacro[] PrepareShaderMacros(bool useVertexColor,
            bool useTexture,
            bool useAlphaThreshold,
            ShaderMacro[] shaderMacros = null) {
            List<ShaderMacro> list = [];
            if (useVertexColor) {
                list.Add(new ShaderMacro("USE_VERTEXCOLOR"));
            }
            if (useTexture) {
                list.Add(new ShaderMacro("USE_TEXTURE"));
            }
            if (useAlphaThreshold) {
                list.Add(new ShaderMacro("USE_ALPHATHRESHOLD"));
            }
            if (shaderMacros != null
                && shaderMacros.Length > 0) {
                foreach (ShaderMacro shaderMacro in shaderMacros) {
                    list.Add(shaderMacro);
                }
            }
            return list.ToArray();
        }
    }
}