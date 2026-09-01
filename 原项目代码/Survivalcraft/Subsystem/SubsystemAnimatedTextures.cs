using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemAnimatedTextures : Subsystem, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemBlocksTexture m_subsystemBlocksTexture;

        public RenderTarget2D m_animatedBlocksTexture;

        public PrimitivesRenderer2D m_primitivesRenderer = new();

        public ScreenSpaceFireRenderer m_screenSpaceFireRenderer = new(200);

        public Random m_random = new();

        public bool m_waterOrder;

        public Vector2 m_waterOffset1;

        public Vector2 m_waterOffset2;

        public bool m_magmaOrder;

        public Vector2 m_magmaOffset1;

        public Vector2 m_magmaOffset2;

        public double m_lastAnimateGameTime;

        public bool DisableTextureAnimation;

        public bool ShowAnimatedTexture;

        public Texture2D AnimatedBlocksTexture {
            get {
                if (DisableTextureAnimation || m_animatedBlocksTexture == null) {
                    return m_subsystemBlocksTexture.BlocksTexture;
                }
                return m_animatedBlocksTexture;
            }
        }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void Update(float dt) {
            if (!DisableTextureAnimation
                && !m_subsystemTime.FixedTimeStep.HasValue
                && SettingsManager.AnimatedTextureRefreshLimit > 0
                && (SettingsManager.AnimatedTextureRefreshLimit >= 7 || Time.PeriodicEvent(0.1 / SettingsManager.AnimatedTextureRefreshLimit, 0))) {
                float dt2 = (float)Math.Min(m_subsystemTime.GameTime - m_lastAnimateGameTime, 1.0);
                Texture2D blocksTexture = m_subsystemBlocksTexture.BlocksTexture;
                if (m_animatedBlocksTexture == null
                    || m_animatedBlocksTexture.Width != blocksTexture.Width
                    || m_animatedBlocksTexture.Height != blocksTexture.Height
                    || m_animatedBlocksTexture.MipLevelsCount > 1 != SettingsManager.TerrainMipmapsEnabled) {
                    Utilities.Dispose(ref m_animatedBlocksTexture);
                    m_animatedBlocksTexture = new RenderTarget2D(
                        blocksTexture.Width,
                        blocksTexture.Height,
                        !SettingsManager.TerrainMipmapsEnabled ? 1 : 4,
                        ColorFormat.Rgba8888,
                        DepthFormat.None
                    );
                    AnimatedBlocksTexture.Tag = blocksTexture.Tag;
                }
                Rectangle scissorRectangle = Display.ScissorRectangle;
                RenderTarget2D renderTarget = Display.RenderTarget;
                Display.RenderTarget = m_animatedBlocksTexture;
                try {
                    Display.Clear(new Vector4(Color.Transparent));
                    m_primitivesRenderer.TexturedBatch(
                            blocksTexture,
                            false,
                            -1,
                            DepthStencilState.None,
                            RasterizerState.CullNone,
                            BlendState.Opaque,
                            SamplerState.PointClamp
                        )
                        .QueueQuad(
                            new Vector2(0f, 0f),
                            new Vector2(m_animatedBlocksTexture.Width, m_animatedBlocksTexture.Height),
                            0f,
                            Vector2.Zero,
                            Vector2.One,
                            Color.White
                        );
                    AnimateWaterBlocksTexture();
                    AnimateMagmaBlocksTexture();
                    m_primitivesRenderer.Flush();
                    Display.ScissorRectangle = AnimateFireBlocksTexture(dt2);
                    m_primitivesRenderer.Flush();
                }
                finally {
                    Display.RenderTarget = renderTarget;
                    Display.ScissorRectangle = scissorRectangle;
                }
                if (SettingsManager.TerrainMipmapsEnabled
                    && Time.FrameIndex % 2 == 0) {
                    m_animatedBlocksTexture.GenerateMipMaps();
                }
                m_lastAnimateGameTime = m_subsystemTime.GameTime;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemBlocksTexture = Project.FindSubsystem<SubsystemBlocksTexture>(true);
            Display.DeviceReset += Display_DeviceReset;
        }

        public override void Dispose() {
            Utilities.Dispose(ref m_animatedBlocksTexture);
            Display.DeviceReset -= Display_DeviceReset;
        }

        public void Display_DeviceReset() {
            m_animatedBlocksTexture = null;
        }

        public void AnimateWaterBlocksTexture() {
            TexturedBatch2D batch = m_primitivesRenderer.TexturedBatch(
                m_subsystemBlocksTexture.BlocksTexture,
                false,
                0,
                DepthStencilState.None,
                null,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            int num = BlocksManager.Blocks[18].DefaultTextureSlot % 16;
            int num2 = BlocksManager.Blocks[18].DefaultTextureSlot / 16;
            double num3 = 1.0 * m_subsystemTime.GameTime;
            double num4 = 1.0 * m_lastAnimateGameTime;
            float num5 = MathUtils.Min((float)MathUtils.Remainder(num3, 2.0), 1f);
            float num6 = MathUtils.Min((float)MathUtils.Remainder(num3 + 1.0, 2.0), 1f);
            byte b = (byte)(255f * num5);
            byte b2 = (byte)(255f * num6);
            if (MathUtils.Remainder(num3, 2.0) >= 1.0
                && MathUtils.Remainder(num4, 2.0) < 1.0) {
                m_waterOrder = true;
                m_waterOffset2 = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
            }
            else if (MathUtils.Remainder(num3 + 1.0, 2.0) >= 1.0
                && MathUtils.Remainder(num4 + 1.0, 2.0) < 1.0) {
                m_waterOrder = false;
                m_waterOffset1 = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
            }
            Vector2 tcOffset = new Vector2(num, num2) - (m_waterOrder ? m_waterOffset1 : m_waterOffset2);
            Vector2 tcOffset2 = new Vector2(num, num2) - (m_waterOrder ? m_waterOffset2 : m_waterOffset1);
            Color color = m_waterOrder ? new Color(b, b, b, b) : new Color(b2, b2, b2, b2);
            Color color2 = m_waterOrder ? new Color(b2, b2, b2, b2) : new Color(b, b, b, b);
            float num7 = MathF.Floor((float)MathUtils.Remainder(1.75 * m_subsystemTime.GameTime, 1.0) * 16f) / 16f;
            float num8 = 0f - num7 + 1f;
            float num9 = MathF.Floor((float)MathUtils.Remainder(1.75f / MathF.Sqrt(2f) * m_subsystemTime.GameTime, 1.0) * 16f) / 16f;
            float num10 = 0f - num9 + 1f;
            Vector2 tc = new(0f, 0f);
            Vector2 tc2 = new(1f, 1f);
            DrawBlocksTextureSlot(
                batch,
                num,
                num2,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num,
                num2,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num7, 0f);
            tc2 = new Vector2(num7 + 1f, 1f);
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num8, 0f);
            tc2 = new Vector2(num8 + 1f, 1f);
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(0f, num7);
            tc2 = new Vector2(1f, num7 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num,
                num2 - 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num,
                num2 - 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(0f, num8);
            tc2 = new Vector2(1f, num8 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num,
                num2 + 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num,
                num2 + 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num9, num10);
            tc2 = new Vector2(num9 + 1f, num10 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2 + 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2 + 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num10, num10);
            tc2 = new Vector2(num10 + 1f, num10 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2 + 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2 + 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num9, num9);
            tc2 = new Vector2(num9 + 1f, num9 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2 - 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2 - 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num10, num9);
            tc2 = new Vector2(num10 + 1f, num9 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2 - 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2 - 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
        }

        public void AnimateMagmaBlocksTexture() {
            TexturedBatch2D batch = m_primitivesRenderer.TexturedBatch(
                m_subsystemBlocksTexture.BlocksTexture,
                false,
                0,
                DepthStencilState.None,
                null,
                BlendState.AlphaBlend,
                SamplerState.PointClamp
            );
            int num = BlocksManager.Blocks[92].DefaultTextureSlot % 16;
            int num2 = BlocksManager.Blocks[92].DefaultTextureSlot / 16;
            double num3 = 0.5 * m_subsystemTime.GameTime;
            double num4 = 0.5 * m_lastAnimateGameTime;
            float num5 = MathUtils.Min((float)MathUtils.Remainder(num3, 2.0), 1f);
            float num6 = MathUtils.Min((float)MathUtils.Remainder(num3 + 1.0, 2.0), 1f);
            byte b = (byte)(255f * num5);
            byte b2 = (byte)(255f * num6);
            if (MathUtils.Remainder(num3, 2.0) >= 1.0
                && MathUtils.Remainder(num4, 2.0) < 1.0) {
                m_magmaOrder = true;
                m_magmaOffset2 = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
            }
            else if (MathUtils.Remainder(num3 + 1.0, 2.0) >= 1.0
                && MathUtils.Remainder(num4 + 1.0, 2.0) < 1.0) {
                m_magmaOrder = false;
                m_magmaOffset1 = new Vector2(m_random.Float(0f, 1f), m_random.Float(0f, 1f));
            }
            Vector2 tcOffset = new Vector2(num, num2) - (m_magmaOrder ? m_magmaOffset1 : m_magmaOffset2);
            Vector2 tcOffset2 = new Vector2(num, num2) - (m_magmaOrder ? m_magmaOffset2 : m_magmaOffset1);
            Color color = m_magmaOrder ? new Color(b, b, b, b) : new Color(b2, b2, b2, b2);
            Color color2 = m_magmaOrder ? new Color(b2, b2, b2, b2) : new Color(b, b, b, b);
            float num7 = MathF.Floor((float)MathUtils.Remainder(0.4 * m_subsystemTime.GameTime, 1.0) * 16f) / 16f;
            float num8 = 0f - num7 + 1f;
            float num9 = MathF.Floor((float)MathUtils.Remainder(0.4 / Math.Sqrt(2) * m_subsystemTime.GameTime, 1.0) * 16f) / 16f;
            float num10 = 0f - num9 + 1f;
            Vector2 tc = new(0f, 0f);
            Vector2 tc2 = new(1f, 1f);
            DrawBlocksTextureSlot(
                batch,
                num,
                num2,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num,
                num2,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num7, 0f);
            tc2 = new Vector2(num7 + 1f, 1f);
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num8, 0f);
            tc2 = new Vector2(num8 + 1f, 1f);
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(0f, num7);
            tc2 = new Vector2(1f, num7 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num,
                num2 - 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num,
                num2 - 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(0f, num8);
            tc2 = new Vector2(1f, num8 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num,
                num2 + 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num,
                num2 + 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num9, num10);
            tc2 = new Vector2(num9 + 1f, num10 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2 + 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2 + 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num10, num10);
            tc2 = new Vector2(num10 + 1f, num10 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2 + 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2 + 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num9, num9);
            tc2 = new Vector2(num9 + 1f, num9 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2 - 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num - 1,
                num2 - 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
            tc = new Vector2(num10, num9);
            tc2 = new Vector2(num10 + 1f, num9 + 1f);
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2 - 1,
                tc,
                tc2,
                tcOffset,
                color
            );
            DrawBlocksTextureSlot(
                batch,
                num + 1,
                num2 - 1,
                tc,
                tc2,
                tcOffset2,
                color2
            );
        }

        public Rectangle AnimateFireBlocksTexture(float dt) {
            int defaultTextureSlot = BlocksManager.Blocks[104].DefaultTextureSlot;
            float num = m_animatedBlocksTexture.Width / 16;
            int num2 = defaultTextureSlot % 16;
            int num3 = defaultTextureSlot / 16;
            m_screenSpaceFireRenderer.ParticleSize = 1f * num;
            m_screenSpaceFireRenderer.ParticleSpeed = 1.9f * num;
            m_screenSpaceFireRenderer.ParticlesPerSecond = 24f;
            m_screenSpaceFireRenderer.MinTimeToLive = float.PositiveInfinity;
            m_screenSpaceFireRenderer.MaxTimeToLive = float.PositiveInfinity;
            m_screenSpaceFireRenderer.ParticleAnimationOffset = 1f;
            m_screenSpaceFireRenderer.ParticleAnimationPeriod = 3f;
            m_screenSpaceFireRenderer.Origin = new Vector2(num2, num3 + 3) * num + new Vector2(0f, 0.5f * m_screenSpaceFireRenderer.ParticleSize);
            m_screenSpaceFireRenderer.Width = num;
            m_screenSpaceFireRenderer.CutoffPosition = num3 * num;
            m_screenSpaceFireRenderer.Update(dt);
            m_screenSpaceFireRenderer.Draw(m_primitivesRenderer, 0f, Matrix.Identity, Color.White);
            return new Rectangle((int)(num2 * num), (int)(num3 * num), (int)num, (int)(num * 3f));
        }

        public virtual void DrawBlocksTextureSlot(TexturedBatch2D batch,
            int slotX,
            int slotY,
            Vector2 tc1,
            Vector2 tc2,
            Vector2 tcOffset,
            Color color) {
            float s = m_animatedBlocksTexture.Width / 16f;
            batch.QueueQuad(
                new Vector2(slotX, slotY) * s,
                new Vector2(slotX + 1, slotY + 1) * s,
                0f,
                (tc1 + tcOffset) / 16f,
                (tc2 + tcOffset) / 16f,
                color
            );
        }
    }
}