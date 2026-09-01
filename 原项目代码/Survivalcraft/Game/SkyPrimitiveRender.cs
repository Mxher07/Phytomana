using Engine;
using Engine.Graphics;

namespace Game {
    public class SkyPrimitiveRender {
        public SkyShader Shader;

        public SkyShader ShaderAlphaTest;

        public Camera Camera;

        public void Textured_FlushWithCurrentStateAndShader(BaseTexturedBatch baseTexturedBatch, Shader shader, bool clearAfterFlush = true) {
            int num = 0;
            int num2 = baseTexturedBatch.TriangleIndices.Count;
            while (num2 > 0) {
                int num3 = MathUtils.Min(num2, 196605);
                Display.DrawUserIndexed(
                    PrimitiveType.TriangleList,
                    shader,
                    VertexPositionColorTexture.VertexDeclaration,
                    baseTexturedBatch.TriangleVertices.Array,
                    0,
                    baseTexturedBatch.TriangleVertices.Count,
                    baseTexturedBatch.TriangleIndices.Array,
                    num,
                    num3
                );
                num += num3;
                num2 -= num3;
            }
            if (clearAfterFlush) {
                baseTexturedBatch.Clear();
            }
        }

        public void Flush(PrimitivesRenderer3D primitiveRend, Matrix matrix, bool clearAfterFlush = true, int maxLayer = 2147483647) {
            if (primitiveRend.m_sortNeeded) {
                primitiveRend.m_sortNeeded = false;
                primitiveRend.m_allBatches.Sort(
                    delegate(BaseBatch b1, BaseBatch b2) {
                        if (b1.Layer < b2.Layer) {
                            return -1;
                        }
                        if (b1.Layer <= b2.Layer) {
                            return 0;
                        }
                        return 1;
                    }
                );
            }
            foreach (BaseBatch baseBatch in primitiveRend.m_allBatches) {
                if (baseBatch.Layer > maxLayer) {
                    break;
                }
                if (!baseBatch.IsEmpty()
                    && baseBatch is TexturedBatch3D) {
                    BaseTexturedBatch baseTexturedBatch = (BaseTexturedBatch)baseBatch;
                    Display.DepthStencilState = baseTexturedBatch.DepthStencilState;
                    Display.RasterizerState = baseTexturedBatch.RasterizerState;
                    Display.BlendState = baseTexturedBatch.BlendState;
                    if (baseTexturedBatch.UseAlphaTest) {
                        ShaderAlphaTest.Texture = baseTexturedBatch.Texture;
                        ShaderAlphaTest.SamplerState = baseTexturedBatch.SamplerState;
                        ShaderAlphaTest.Transforms.World[0] = matrix;
                        ShaderAlphaTest.AlphaThreshold = 0f;
                        ModsManager.HookAction(
                            "SetShaderParameter",
                            modLoader => {
                                modLoader.SetShaderParameter(ShaderAlphaTest, Camera);
                                return true;
                            }
                        );
                        baseTexturedBatch.FlushWithDeviceState(ShaderAlphaTest, clearAfterFlush);
                    }
                    else {
                        Shader.Texture = baseTexturedBatch.Texture;
                        Shader.SamplerState = baseTexturedBatch.SamplerState;
                        Shader.Transforms.World[0] = matrix;
                        ModsManager.HookAction(
                            "SetShaderParameter",
                            modLoader => {
                                modLoader.SetShaderParameter(Shader, Camera);
                                return true;
                            }
                        );
                        baseTexturedBatch.FlushWithDeviceState(Shader, clearAfterFlush);
                    }
                }
                else {
                    baseBatch.Flush(matrix, Vector4.One);
                }
            }
        }
    }
}