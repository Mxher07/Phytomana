using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemGlow : Subsystem, IDrawable {
        public SubsystemSky m_subsystemSky;

        public Dictionary<GlowPoint, bool> m_glowPoints = [];

        public PrimitivesRenderer3D m_primitivesRenderer = new();

        public TexturedBatch3D[] m_batchesByType = new TexturedBatch3D[4];

        public static int[] m_drawOrders = [110];

        public int[] DrawOrders => m_drawOrders;

        public GlowPoint AddGlowPoint() {
            GlowPoint glowPoint = new();
            m_glowPoints.Add(glowPoint, true);
            return glowPoint;
        }

        public void RemoveGlowPoint(GlowPoint glowPoint) {
            m_glowPoints.Remove(glowPoint);
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            foreach (GlowPoint key in m_glowPoints.Keys) {
                if (key.Color.A > 0) {
                    Vector3 vector = key.Position - camera.ViewPosition;
                    float num = Vector3.Dot(vector, camera.ViewDirection);
                    if (num > 0.01f) {
                        float num2 = vector.Length();
                        if (num2 < m_subsystemSky.VisibilityRange) {
                            float num3 = key.Size;
                            if (key.FarDistance > 0f) {
                                num3 += (key.FarSize - key.Size) * MathUtils.Saturate(num2 / key.FarDistance);
                            }
                            Vector3 v = (0f - (0.01f + 0.02f * num)) / num2 * vector;
                            Color color = Color.LerpNotSaturated(
                                key.Color,
                                m_subsystemSky.ViewFogColor,
                                m_subsystemSky.CalculateFog(camera.ViewPosition, key.Position)
                            );
                            Vector3 p = key.Position + num3 * (-key.Right - key.Up) + v;
                            Vector3 p2 = key.Position + num3 * (key.Right - key.Up) + v;
                            Vector3 p3 = key.Position + num3 * (key.Right + key.Up) + v;
                            Vector3 p4 = key.Position + num3 * (-key.Right + key.Up) + v;
                            m_batchesByType[(int)key.Type]
                            .QueueQuad(
                                p,
                                p2,
                                p3,
                                p4,
                                new Vector2(0f, 0f),
                                new Vector2(1f, 0f),
                                new Vector2(1f, 1f),
                                new Vector2(0f, 1f),
                                color
                            );
                        }
                    }
                }
            }
            m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_batchesByType[0] = m_primitivesRenderer.TexturedBatch(
                ContentManager.Get<Texture2D>("Textures/RoundGlow"),
                false,
                0,
                DepthStencilState.DepthRead,
                RasterizerState.CullCounterClockwiseScissor,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp
            );
            m_batchesByType[1] = m_primitivesRenderer.TexturedBatch(
                ContentManager.Get<Texture2D>("Textures/SquareGlow"),
                false,
                0,
                DepthStencilState.DepthRead,
                RasterizerState.CullCounterClockwiseScissor,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp
            );
            m_batchesByType[2] = m_primitivesRenderer.TexturedBatch(
                ContentManager.Get<Texture2D>("Textures/HorizontalRectGlow"),
                false,
                0,
                DepthStencilState.DepthRead,
                RasterizerState.CullCounterClockwiseScissor,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp
            );
            m_batchesByType[3] = m_primitivesRenderer.TexturedBatch(
                ContentManager.Get<Texture2D>("Textures/VerticalRectGlow"),
                false,
                0,
                DepthStencilState.DepthRead,
                RasterizerState.CullCounterClockwiseScissor,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp
            );
        }
    }
}