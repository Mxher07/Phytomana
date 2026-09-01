using Engine;
using Engine.Graphics;
using Silk.NET.OpenGLES;

namespace Game {
    public class ViewWidget : TouchInputWidget, IDragTargetWidget {
        public SubsystemDrawing m_subsystemDrawing;

        public RenderTarget2D m_scalingRenderTarget;
        public static RenderTarget2D ScreenTexture = new(Window.Size.X, Window.Size.Y, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
        public PrimitivesRenderer2D m_vrFadePr2 = new();

        public GameWidget GameWidget { get; set; }

        public Point2? ScalingRenderTargetSize {
            get {
                if (m_scalingRenderTarget == null) {
                    return null;
                }
                return new Point2(m_scalingRenderTarget.Width, m_scalingRenderTarget.Height);
            }
        }

        public override void ChangeParent(ContainerWidget parentWidget) {
            if (parentWidget is GameWidget) {
                GameWidget = (GameWidget)parentWidget;
                m_subsystemDrawing = GameWidget.SubsystemGameWidgets.Project.FindSubsystem<SubsystemDrawing>(true);
                base.ChangeParent(parentWidget);
                return;
            }
            throw new InvalidOperationException("ViewWidget must be a child of GameWidget.");
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
            base.MeasureOverride(parentAvailableSize);
        }

        public override void Draw(DrawContext dc) {
            if (GameWidget.PlayerData.ComponentPlayer != null
                && GameWidget.PlayerData.IsReadyForPlaying) {
                DrawToScreen(dc);
            }
        }

        public override void Dispose() {
            base.Dispose();
            Utilities.Dispose(ref m_scalingRenderTarget);
        }

        public virtual void DragOver(Widget dragWidget, object data) { }

        public virtual void DragDrop(Widget dragWidget, object data) {
            if (data is InventoryDragData inventoryDragData
                && GameManager.Project != null) {
                SubsystemPickables subsystemPickables = GameManager.Project.FindSubsystem<SubsystemPickables>(true);
                ComponentPlayer componentPlayer = GameWidget.PlayerData.ComponentPlayer;
                int slotValue = inventoryDragData.Inventory.GetSlotValue(inventoryDragData.SlotIndex);
                int count = componentPlayer != null
                    && componentPlayer.ComponentInput.SplitSourceInventory == inventoryDragData.Inventory
                    && componentPlayer.ComponentInput.SplitSourceSlotIndex == inventoryDragData.SlotIndex ? 1 :
                    inventoryDragData.DragMode != DragMode.SingleItem ? inventoryDragData.Inventory.GetSlotCount(inventoryDragData.SlotIndex) :
                    MathUtils.Min(inventoryDragData.Inventory.GetSlotCount(inventoryDragData.SlotIndex), 1);
                int num = inventoryDragData.Inventory.RemoveSlotItems(inventoryDragData.SlotIndex, count);
                if (num > 0) {
                    Vector2 vector = dragWidget.WidgetToScreen(dragWidget.ActualSize / 2f);
                    Vector3 value = Vector3.Normalize(
                            GameWidget.ActiveCamera.ScreenToWorld(new Vector3(vector.X, vector.Y, 1f), Matrix.Identity)
                            - GameWidget.ActiveCamera.ViewPosition
                        )
                        * 12f;
                    subsystemPickables.AddPickable(slotValue, num, GameWidget.ActiveCamera.ViewPosition, value, null, componentPlayer.Entity);
                }
            }
        }

        public virtual void SetupScalingRenderTarget() {
            float num = SettingsManager.ResolutionMode == ResolutionMode.Low ? 0.5f : SettingsManager.ResolutionMode != ResolutionMode.Medium ? 1f : 0.75f;
            float num2 = GlobalTransform.Right.Length();
            float num3 = GlobalTransform.Up.Length();
            Vector2 vector = new(ActualSize.X * num2, ActualSize.Y * num3);
            Point2 point = default;
            point.X = (int)MathF.Round(vector.X * num);
            point.Y = (int)MathF.Round(vector.Y * num);
            Point2 point2 = point;
            if ((num < 1f || GlobalColorTransform != Color.White)
                && point2.X > 0
                && point2.Y > 0) {
                if (m_scalingRenderTarget == null
                    || m_scalingRenderTarget.Width != point2.X
                    || m_scalingRenderTarget.Height != point2.Y) {
                    Utilities.Dispose(ref m_scalingRenderTarget);
                    m_scalingRenderTarget = new RenderTarget2D(point2.X, point2.Y, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
                }
                Display.RenderTarget = m_scalingRenderTarget;
                Display.Clear(Color.Black, 1f, 0);
            }
            else {
                Utilities.Dispose(ref m_scalingRenderTarget);
            }
        }

        public virtual void ApplyScalingRenderTarget(DrawContext dc) {
            if (m_scalingRenderTarget != null) {
                BlendState blendState = GlobalColorTransform.A < byte.MaxValue ? BlendState.AlphaBlend : BlendState.Opaque;
                TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(
                    m_scalingRenderTarget,
                    false,
                    0,
                    DepthStencilState.None,
                    RasterizerState.CullNoneScissor,
                    blendState,
                    SamplerState.PointClamp
                );
                int count = texturedBatch2D.TriangleVertices.Count;
                texturedBatch2D.QueueQuad(Vector2.Zero, ActualSize, 0f, Vector2.Zero, Vector2.One, GlobalColorTransform);
                texturedBatch2D.TransformTriangles(GlobalTransform, count);
                dc.PrimitivesRenderer2D.Flush();
            }
        }

        public virtual void DrawToScreen(DrawContext dc) {
            if (VrManager.IsFrameActive
                && (Input.Devices & WidgetInputDevice.VrControllers) != WidgetInputDevice.None
                && GameWidget.ActiveCamera is BasePerspectiveCamera camera) {
                DrawToScreenVr(camera);
                return;
            }
            GameWidget.GuiWidget.IsDrawEnabled = true;
            GameWidget.ActiveCamera.PrepareForDrawing(null);
            RenderTarget2D renderTarget = Display.RenderTarget;
            SetupScalingRenderTarget();
            try {
                m_subsystemDrawing.Draw(GameWidget.ActiveCamera);
            }
            finally {
                Display.RenderTarget = renderTarget;
            }
            ApplyScalingRenderTarget(dc);
            ModsManager.HookAction(
                "DrawToScreen",
                loader => {
                    loader.DrawToScreen(this, dc);
                    return false;
                }
            );
        }

        void DrawToScreenVr(BasePerspectiveCamera camera) {
#if WINDOWS
            int desktopFbo = GLWrapper.m_mainFramebuffer;
#endif

            VrManager.RenderToEyes((vrEye, eyeFrame) => {
                    camera.PrepareForDrawing(vrEye);
                    m_subsystemDrawing.Draw(camera);

                    GameWidget.DrawVrGui(eyeFrame);

                    // Snap turn fade overlay
                    float fadeAlpha = ComponentInput.VrFadeAlpha;
                    if (fadeAlpha > 0.001f) {
                        var flatBatch = m_vrFadePr2?.FlatBatch(0, null, null, null);
                        flatBatch?.QueueQuad(
                            Vector2.Zero, new Vector2(VrManager.SwapchainWidth, VrManager.SwapchainHeight),
                            0f, new Color(0f, 0f, 0f, fadeAlpha));
                        m_vrFadePr2?.Flush();
                    }

#if WINDOWS
                    if (vrEye == VrEye.Left) {
                        BlitVrEyeToDesktop(eyeFrame.Fbo, desktopFbo,
                            VrManager.SwapchainWidth, VrManager.SwapchainHeight,
                            GameWidget);
                    }
#endif
                }
            );

            ModsManager.HookAction(
                "DrawToScreen",
                loader => {
                    loader.DrawToScreen(this, null);
                    return false;
                }
            );
        }

#if WINDOWS
        static void BlitVrEyeToDesktop(int srcFbo, int dstFbo, int vrW, int vrH, GameWidget gameWidget) {
            if (srcFbo == 0) return;

            Vector2 origin = Vector2.Transform(Vector2.Zero, gameWidget.GlobalTransform);
            Vector2 end = Vector2.Transform(gameWidget.ActualSize, gameWidget.GlobalTransform);
            int x1 = Math.Max(0, (int)MathF.Min(origin.X, end.X));
            int y1 = Math.Max(0, (int)MathF.Min(origin.Y, end.Y));
            int x2 = Math.Min(Display.BackbufferSize.X, (int)MathF.Max(origin.X, end.X));
            int y2 = Math.Min(Display.BackbufferSize.Y, (int)MathF.Max(origin.Y, end.Y));
            if (x2 <= x1 || y2 <= y1) return;

            GLWrapper.GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, (uint)dstFbo);
            GLWrapper.ApplyViewportScissor(
                new Viewport(0, 0, Display.BackbufferSize.X, Display.BackbufferSize.Y),
                new Rectangle(x1, y1, x2 - x1, y2 - y1), true);
            GLWrapper.Enable(EnableCap.ScissorTest);
            GLWrapper.ClearColor(Vector4.Zero);
            GLWrapper.GL.Clear(ClearBufferMask.ColorBufferBit);
            GLWrapper.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, (uint)srcFbo);

            float vrAspect = (float)vrW / vrH;
            float regionW = x2 - x1;
            float regionH = y2 - y1;
            float regionAspect = regionW / regionH;

            int drawW, drawH, offsetX, offsetY;
            if (regionAspect > vrAspect) {
                drawH = (int)regionH;
                drawW = (int)(regionH * vrAspect);
                offsetX = x1 + ((int)regionW - drawW) / 2;
                offsetY = y1;
            }
            else {
                drawW = (int)regionW;
                drawH = (int)(regionW / vrAspect);
                offsetX = x1;
                offsetY = y1 + ((int)regionH - drawH) / 2;
            }

            GLWrapper.GL.BlitFramebuffer(
                0, 0, vrW, vrH,
                offsetX, offsetY, offsetX + drawW, offsetY + drawH,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Linear);
            GLWrapper.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, (uint)dstFbo);
        }
#endif
    }
}
