using Engine.Media;

namespace Engine.Graphics {
    public class PrimitivesRenderer3D : BasePrimitivesRenderer<FlatBatch3D, TexturedBatch3D, FontBatch3D> {
        public FlatBatch3D FlatBatch(int layer = 0,
            DepthStencilState depthStencilState = null,
            RasterizerState rasterizerState = null,
            BlendState blendState = null) {
            depthStencilState = depthStencilState ?? DepthStencilState.Default;
            rasterizerState = rasterizerState ?? RasterizerState.CullNoneScissor;
            blendState = blendState ?? BlendState.AlphaBlend;
            return FindFlatBatch(layer, depthStencilState, rasterizerState, blendState);
        }

        public TexturedBatch3D TexturedBatch(Texture2D texture,
            bool useAlphaTest = false,
            int layer = 0,
            DepthStencilState depthStencilState = null,
            RasterizerState rasterizerState = null,
            BlendState blendState = null,
            SamplerState samplerState = null) {
            depthStencilState = depthStencilState ?? DepthStencilState.Default;
            rasterizerState = rasterizerState ?? RasterizerState.CullNoneScissor;
            blendState = blendState ?? BlendState.AlphaBlend;
            samplerState = samplerState ?? SamplerState.LinearClamp;
            return FindTexturedBatch(
                texture,
                useAlphaTest,
                layer,
                depthStencilState,
                rasterizerState,
                blendState,
                samplerState
            );
        }

        public FontBatch3D FontBatch(BitmapFont font,
            int layer = 0,
            DepthStencilState depthStencilState = null,
            RasterizerState rasterizerState = null,
            BlendState blendState = null,
            SamplerState samplerState = null) {
            depthStencilState = depthStencilState ?? DepthStencilState.Default;
            rasterizerState = rasterizerState ?? RasterizerState.CullNoneScissor;
            blendState = blendState ?? BlendState.AlphaBlend;
            samplerState = samplerState ?? SamplerState.LinearClamp;
            return FindFontBatch(font, layer, depthStencilState, rasterizerState, blendState, samplerState);
        }
    }
}