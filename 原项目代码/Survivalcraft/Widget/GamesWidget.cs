using Engine;
using Engine.Graphics;

namespace Game {
    public class GamesWidget : ContainerWidget {
        public float m_spacing;
        public float m_bevel;

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            base.MeasureOverride(parentAvailableSize);
            IsOverdrawRequired = Children.Count > 1;
        }

        public override void ArrangeOverride() {
            float marginLeft = 0f;
            float marginRight = 0f;
            if (SettingsManager.AdaptEdgeToEdgeDisplay) {
                marginLeft = Window.DisplayCutoutInsets.X * ScreensManager.FinalUiScale;
                marginRight = Window.DisplayCutoutInsets.Z * ScreensManager.FinalUiScale;
            }
            if (Children.Count == 1) {
                ArrangeChildWidgetInCell(Vector2.Zero, ActualSize, Children[0]);
                if (SettingsManager.ScreenLayout1 == ScreenLayout.Single
                    && Children[0] is GameWidget gameWidget0) {
                    gameWidget0.LayoutTransform = Matrix.Identity;
                    if (SettingsManager.AdaptEdgeToEdgeDisplay) {
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = marginLeft;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight;
                        guiWidget.MarginBottom = 0f;
                    }
                }
            }
            else if (Children.Count == 2) {
                if (SettingsManager.ScreenLayout2 == ScreenLayout.DoubleVertical) {
                    m_spacing = 12f;
                    m_bevel = 3f;
                    float x = 0f;
                    float y = 0f;
                    float x2 = ActualSize.X / 2f + m_spacing / 2f;
                    float y2 = 0f;
                    float x3 = ActualSize.X / 2f - m_spacing / 2f;
                    float y3 = ActualSize.Y;
                    float num = 0.5f;
                    ArrangeChildWidgetInCell(new Vector2(x, y), new Vector2(x, y) + new Vector2(x3, y3) / num, Children[0]);
                    if (Children[0] is GameWidget gameWidget0) {
                        gameWidget0.LayoutTransform = Matrix.CreateScale(num, num, 1f);
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x2, y2), new Vector2(x2, y2) + new Vector2(x3, y3) / num, Children[1]);
                    if (Children[1] is GameWidget gameWidget1) {
                        gameWidget1.LayoutTransform = Matrix.CreateScale(num, num, 1f);
                        ContainerWidget guiWidget = gameWidget1.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num;
                        guiWidget.MarginBottom = 0f;
                    }
                }
                if (SettingsManager.ScreenLayout2 == ScreenLayout.DoubleHorizontal) {
                    m_spacing = 12f;
                    m_bevel = 3f;
                    float x4 = 0f;
                    float y4 = 0f;
                    float x5 = 0f;
                    float y5 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x6 = ActualSize.X;
                    float y6 = ActualSize.Y / 2f - m_spacing / 2f;
                    float num2 = 0.48f;
                    ArrangeChildWidgetInCell(new Vector2(x4, y4), new Vector2(x4, y4) + new Vector2(x6, y6) / num2, Children[0]);
                    if (Children[0] is GameWidget gameWidget0) {
                        gameWidget0.LayoutTransform = Matrix.CreateScale(num2, num2, 1f);
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num2;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num2;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x5, y5), new Vector2(x5, y5) + new Vector2(x6, y6) / num2, Children[1]);
                    if (Children[1] is GameWidget gameWidget1) {
                        gameWidget1.LayoutTransform = Matrix.CreateScale(num2, num2, 1f);
                        ContainerWidget guiWidget = gameWidget1.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num2;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num2;
                        guiWidget.MarginBottom = 0f;
                    }
                }
                if (SettingsManager.ScreenLayout2 == ScreenLayout.DoubleOpposite) {
                    m_spacing = 20f;
                    m_bevel = 4f;
                    float x7 = 0f;
                    float y7 = 0f;
                    float x8 = ActualSize.X / 2f + m_spacing / 2f;
                    float y8 = 0f;
                    float x9 = ActualSize.X / 2f - m_spacing / 2f;
                    float y9 = ActualSize.Y;
                    float num3 = Window.Size.Y / (float)Window.Size.X;
                    ArrangeChildWidgetInCell(new Vector2(x7, y7), new Vector2(x7, y7) + new Vector2(x9, y9) / num3, Children[0]);
                    if (Children[0] is GameWidget gameWidget0) {
                        gameWidget0.LayoutTransform = new Matrix(
                            0f,
                            num3,
                            0f,
                            0f,
                            0f - num3,
                            0f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f
                        );
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = marginLeft / num3;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x8, y8), new Vector2(x8, y8) + new Vector2(x9, y9) / num3, Children[1]);
                    if (Children[1] is GameWidget gameWidget1) {
                        gameWidget1.LayoutTransform = new Matrix(
                            0f,
                            0f - num3,
                            0f,
                            0f,
                            num3,
                            0f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f
                        );
                        ContainerWidget guiWidget = gameWidget1.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = marginRight / num3;
                    }
                }
            }
            else if (Children.Count == 3) {
                m_spacing = 12f;
                m_bevel = 3f;
                if (SettingsManager.ScreenLayout3 == ScreenLayout.TripleVertical) {
                    float x10 = 0f;
                    float y10 = 0f;
                    float x11 = ActualSize.X / 2f + m_spacing / 2f;
                    float y11 = 0f;
                    float x12 = ActualSize.X / 2f + m_spacing / 2f;
                    float y12 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x13 = ActualSize.X / 2f - m_spacing / 2f;
                    float y13 = ActualSize.Y;
                    float y14 = ActualSize.Y / 2f - m_spacing / 2f;
                    float num4 = 0.5f;
                    ArrangeChildWidgetInCell(new Vector2(x10, y10), new Vector2(x10, y10) + new Vector2(x13, y13) / num4, Children[0]);
                    if (Children[0] is GameWidget gameWidget0) {
                        gameWidget0.LayoutTransform = Matrix.CreateScale(num4, num4, 1f);
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num4;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x11, y11), new Vector2(x11, y11) + new Vector2(x13, y14) / num4, Children[1]);
                    if (Children[1] is GameWidget gameWidget1) {
                        gameWidget1.LayoutTransform = Matrix.CreateScale(num4, num4, 1f);
                        ContainerWidget guiWidget = gameWidget1.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num4;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x12, y12), new Vector2(x12, y12) + new Vector2(x13, y14) / num4, Children[2]);
                    if (Children[2] is GameWidget gameWidget2) {
                        gameWidget2.LayoutTransform = Matrix.CreateScale(num4, num4, 1f);
                        ContainerWidget guiWidget = gameWidget2.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num4;
                        guiWidget.MarginBottom = 0f;
                    }
                }
                if (SettingsManager.ScreenLayout3 == ScreenLayout.TripleHorizontal) {
                    float x14 = 0f;
                    float y15 = 0f;
                    float x15 = 0f;
                    float y16 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x16 = ActualSize.X / 2f + m_spacing / 2f;
                    float y17 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x17 = ActualSize.X;
                    float x18 = ActualSize.X / 2f - m_spacing / 2f;
                    float y18 = ActualSize.Y / 2f - m_spacing / 2f;
                    float num5 = 0.5f;
                    ArrangeChildWidgetInCell(new Vector2(x14, y15), new Vector2(x14, y15) + new Vector2(x17, y18) / num5, Children[0]);
                    if (Children[0] is GameWidget gameWidget0) {
                        gameWidget0.LayoutTransform = Matrix.CreateScale(num5, num5, 1f);
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num5;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num5;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x15, y16), new Vector2(x15, y16) + new Vector2(x18, y18) / num5, Children[1]);
                    if (Children[1] is GameWidget gameWidget1) {
                        gameWidget1.LayoutTransform = Matrix.CreateScale(num5, num5, 1f);
                        ContainerWidget guiWidget = gameWidget1.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num5;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x16, y17), new Vector2(x16, y17) + new Vector2(x18, y18) / num5, Children[2]);
                    if (Children[2] is GameWidget gameWidget2) {
                        gameWidget2.LayoutTransform = Matrix.CreateScale(num5, num5, 1f);
                        ContainerWidget guiWidget = gameWidget2.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num5;
                        guiWidget.MarginBottom = 0f;
                    }
                }
                if (SettingsManager.ScreenLayout3 == ScreenLayout.TripleEven) {
                    float x19 = 0f;
                    float y19 = 0f;
                    float x20 = ActualSize.X / 2f + m_spacing / 2f;
                    float y20 = 0f;
                    float x21 = ActualSize.X / 4f + m_spacing / 4f;
                    float y21 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x22 = ActualSize.X / 2f - m_spacing / 2f;
                    float y22 = ActualSize.Y / 2f - m_spacing / 2f;
                    float num6 = 0.5f;
                    ArrangeChildWidgetInCell(new Vector2(x19, y19), new Vector2(x19, y19) + new Vector2(x22, y22) / num6, Children[0]);
                    if (Children[0] is GameWidget gameWidget0) {
                        gameWidget0.LayoutTransform = Matrix.CreateScale(num6, num6, 1f);
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num6;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x20, y20), new Vector2(x20, y20) + new Vector2(x22, y22) / num6, Children[1]);
                    if (Children[1] is GameWidget gameWidget1) {
                        gameWidget1.LayoutTransform = Matrix.CreateScale(num6, num6, 1f);
                        ContainerWidget guiWidget = gameWidget1.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num6;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x21, y21), new Vector2(x21, y21) + new Vector2(x22, y22) / num6, Children[2]);
                    if (Children[2] is GameWidget gameWidget2) {
                        gameWidget2.LayoutTransform = Matrix.CreateScale(num6, num6, 1f);
                        ContainerWidget guiWidget = gameWidget2.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                }
                if (SettingsManager.ScreenLayout3 == ScreenLayout.TripleOpposite) {
                    float x23 = 0f;
                    float y23 = 0f;
                    float x24 = ActualSize.X / 2f + m_spacing / 2f;
                    float y24 = 0f;
                    float x25 = ActualSize.X / 2f + m_spacing / 2f;
                    float y25 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x26 = ActualSize.X / 2f - m_spacing / 2f;
                    float y26 = ActualSize.Y;
                    float y27 = ActualSize.Y / 2f - m_spacing / 2f;
                    float num7 = 0.5f;
                    ArrangeChildWidgetInCell(new Vector2(x23, y23), new Vector2(x23, y23) + new Vector2(x26, y26) / num7, Children[0]);
                    if (Children[0] is GameWidget gameWidget0) {
                        gameWidget0.LayoutTransform = new Matrix(
                            0f,
                            num7,
                            0f,
                            0f,
                            0f - num7,
                            0f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f
                        );
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = marginLeft / num7;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x24, y24), new Vector2(x24, y24) + new Vector2(x26, y27) / num7, Children[1]);
                    if (Children[1] is GameWidget gameWidget1) {
                        gameWidget1.LayoutTransform = new Matrix(
                            0f - num7,
                            0f,
                            0f,
                            0f,
                            0f,
                            0f - num7,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f
                        );
                        ContainerWidget guiWidget = gameWidget1.GuiWidget;
                        guiWidget.MarginLeft = marginRight / num7;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x25, y25), new Vector2(x25, y25) + new Vector2(x26, y27) / num7, Children[2]);
                    if (Children[2] is GameWidget gameWidget2) {
                        gameWidget2.LayoutTransform = new Matrix(
                            num7,
                            0f,
                            0f,
                            0f,
                            0f,
                            num7,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f
                        );
                        ContainerWidget guiWidget = gameWidget2.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num7;
                        guiWidget.MarginBottom = 0f;
                    }
                }
            }
            else if (Children.Count == 4) {
                if (SettingsManager.ScreenLayout4 == ScreenLayout.Quadruple) {
                    m_spacing = 12f;
                    m_bevel = 3f;
                    float x27 = 0f;
                    float y28 = 0f;
                    float x28 = ActualSize.X / 2f + m_spacing / 2f;
                    float y29 = 0f;
                    float x29 = 0f;
                    float y30 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x30 = ActualSize.X / 2f + m_spacing / 2f;
                    float y31 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x31 = ActualSize.X / 2f - m_spacing / 2f;
                    float y32 = ActualSize.Y / 2f - m_spacing / 2f;
                    float num8 = 0.5f;
                    ArrangeChildWidgetInCell(new Vector2(x27, y28), new Vector2(x27, y28) + new Vector2(x31, y32) / num8, Children[0]);
                    if (Children[0] is GameWidget gameWidget0) {
                        gameWidget0.LayoutTransform = Matrix.CreateScale(num8, num8, 1f);
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num8;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x28, y29), new Vector2(x28, y29) + new Vector2(x31, y32) / num8, Children[1]);
                    if (Children[1] is GameWidget gameWidget1) {
                        gameWidget1.LayoutTransform = Matrix.CreateScale(num8, num8, 1f);
                        ContainerWidget guiWidget = gameWidget1.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num8;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x29, y30), new Vector2(x29, y30) + new Vector2(x31, y32) / num8, Children[2]);
                    if (Children[2] is GameWidget gameWidget2) {
                        gameWidget2.LayoutTransform = Matrix.CreateScale(num8, num8, 1f);
                        ContainerWidget guiWidget = gameWidget2.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num8;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x30, y31), new Vector2(x30, y31) + new Vector2(x31, y32) / num8, Children[3]);
                    if (Children[3] is GameWidget gameWidget3) {
                        gameWidget3.LayoutTransform = Matrix.CreateScale(num8, num8, 1f);
                        ContainerWidget guiWidget = gameWidget3.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num8;
                        guiWidget.MarginBottom = 0f;
                    }
                }
                if (SettingsManager.ScreenLayout4 == ScreenLayout.QuadrupleOpposite) {
                    m_spacing = 12f;
                    m_bevel = 3f;
                    float x32 = 0f;
                    float y33 = 0f;
                    float x33 = ActualSize.X / 2f + m_spacing / 2f;
                    float y34 = 0f;
                    float x34 = 0f;
                    float y35 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x35 = ActualSize.X / 2f + m_spacing / 2f;
                    float y36 = ActualSize.Y / 2f + m_spacing / 2f;
                    float x36 = ActualSize.X / 2f - m_spacing / 2f;
                    float y37 = ActualSize.Y / 2f - m_spacing / 2f;
                    float num9 = 0.5f;
                    ArrangeChildWidgetInCell(new Vector2(x32, y33), new Vector2(x32, y33) + new Vector2(x36, y37) / num9, Children[0]);
                    if (Children[0] is GameWidget gameWidget0) {
                        gameWidget0.LayoutTransform = new Matrix(
                            0f - num9,
                            0f,
                            0f,
                            0f,
                            0f,
                            0f - num9,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f
                        );
                        ContainerWidget guiWidget = gameWidget0.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginLeft / num9;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x33, y34), new Vector2(x33, y34) + new Vector2(x36, y37) / num9, Children[1]);
                    if (Children[1] is GameWidget gameWidget1) {
                        gameWidget1.LayoutTransform = new Matrix(
                            0f - num9,
                            0f,
                            0f,
                            0f,
                            0f,
                            0f - num9,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f
                        );
                        ContainerWidget guiWidget = gameWidget1.GuiWidget;
                        guiWidget.MarginLeft = marginRight / num9;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x34, y35), new Vector2(x34, y35) + new Vector2(x36, y37) / num9, Children[2]);
                    if (Children[2] is GameWidget gameWidget2) {
                        gameWidget2.LayoutTransform = new Matrix(
                            num9,
                            0f,
                            0f,
                            0f,
                            0f,
                            num9,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f
                        );
                        ContainerWidget guiWidget = gameWidget2.GuiWidget;
                        guiWidget.MarginLeft = marginLeft / num9;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = 0f;
                        guiWidget.MarginBottom = 0f;
                    }
                    ArrangeChildWidgetInCell(new Vector2(x35, y36), new Vector2(x35, y36) + new Vector2(x36, y37) / num9, Children[3]);
                    if (Children[3] is GameWidget gameWidget3) {
                        gameWidget3.LayoutTransform = new Matrix(
                            num9,
                            0f,
                            0f,
                            0f,
                            0f,
                            num9,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f,
                            0f,
                            0f,
                            0f,
                            0f,
                            1f
                        );
                        ContainerWidget guiWidget = gameWidget3.GuiWidget;
                        guiWidget.MarginLeft = 0f;
                        guiWidget.MarginTop = 0f;
                        guiWidget.MarginRight = marginRight / num9;
                        guiWidget.MarginBottom = 0f;
                    }
                }
            }
            else if (Children.Count > SubsystemPlayers.MaxPlayers) {
                throw new InvalidOperationException("Too many GameWidgets.");
            }
        }

        public override void Overdraw(DrawContext dc) {
            Color color = new Color(181, 172, 154) * GlobalColorTransform;
            float num = 0.6f;
            float directionalLight = 0.4f;
            FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch();
            int count = flatBatch2D.TriangleVertices.Count;
            if (Children.Count == 2) {
                if (SettingsManager.ScreenLayout2 == ScreenLayout.DoubleVertical
                    || SettingsManager.ScreenLayout2 == ScreenLayout.DoubleOpposite) {
                    Vector2 c = new(ActualSize.X / 2f - m_spacing / 2f, -100f);
                    Vector2 c2 = new(ActualSize.X / 2f + m_spacing / 2f, ActualSize.Y + 100f);
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        c,
                        c2,
                        0f,
                        m_bevel,
                        color,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                }
                if (SettingsManager.ScreenLayout2 == ScreenLayout.DoubleHorizontal) {
                    Vector2 c3 = new(-100f, ActualSize.Y / 2f - m_spacing / 2f);
                    Vector2 c4 = new(ActualSize.X + 100f, ActualSize.Y / 2f + m_spacing / 2f);
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        c3,
                        c4,
                        0f,
                        m_bevel,
                        color,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                }
            }
            else if (Children.Count == 3) {
                if (SettingsManager.ScreenLayout3 == ScreenLayout.TripleVertical
                    || SettingsManager.ScreenLayout3 == ScreenLayout.TripleOpposite) {
                    float x = -100f;
                    float x2 = ActualSize.X / 2f - m_spacing / 2f + m_bevel;
                    float x3 = ActualSize.X / 2f + m_spacing / 2f - m_bevel;
                    float x4 = ActualSize.X + 100f;
                    float y = -100f;
                    float y2 = ActualSize.Y / 2f - m_spacing / 2f + m_bevel;
                    float y3 = ActualSize.Y / 2f + m_spacing / 2f - m_bevel;
                    float y4 = ActualSize.Y + 100f;
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        new Vector2(x, y),
                        new Vector2(x2, y4),
                        0f,
                        0f - m_bevel,
                        Color.Transparent,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        new Vector2(x3, y),
                        new Vector2(x4, y2),
                        0f,
                        0f - m_bevel,
                        Color.Transparent,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        new Vector2(x3, y3),
                        new Vector2(x4, y4),
                        0f,
                        0f - m_bevel,
                        Color.Transparent,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                    Color color2 = color * new Color(num, num, num, 1f);
                    flatBatch2D.QueueQuad(new Vector2(x2, y), new Vector2(x3, y4), 0f, color2);
                    flatBatch2D.QueueQuad(new Vector2(x3, y2), new Vector2(x4, y3), 0f, color2);
                }
                if (SettingsManager.ScreenLayout3 == ScreenLayout.TripleHorizontal) {
                    float x5 = -100f;
                    float x6 = ActualSize.X / 2f - m_spacing / 2f + m_bevel;
                    float x7 = ActualSize.X / 2f + m_spacing / 2f - m_bevel;
                    float x8 = ActualSize.X + 100f;
                    float y5 = -100f;
                    float y6 = ActualSize.Y / 2f - m_spacing / 2f + m_bevel;
                    float y7 = ActualSize.Y / 2f + m_spacing / 2f - m_bevel;
                    float y8 = ActualSize.Y + 100f;
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        new Vector2(x5, y5),
                        new Vector2(x8, y6),
                        0f,
                        0f - m_bevel,
                        Color.Transparent,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        new Vector2(x5, y7),
                        new Vector2(x6, y8),
                        0f,
                        0f - m_bevel,
                        Color.Transparent,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        new Vector2(x7, y7),
                        new Vector2(x8, y8),
                        0f,
                        0f - m_bevel,
                        Color.Transparent,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                    Color color3 = color * new Color(num, num, num, 1f);
                    flatBatch2D.QueueQuad(new Vector2(x5, y6), new Vector2(x8, y7), 0f, color3);
                    flatBatch2D.QueueQuad(new Vector2(x6, y7), new Vector2(x7, y8), 0f, color3);
                }
                if (SettingsManager.ScreenLayout3 == ScreenLayout.TripleEven) {
                    float x9 = -100f;
                    float x10 = ActualSize.X / 2f - m_spacing / 2f + m_bevel;
                    float x11 = ActualSize.X / 2f + m_spacing / 2f - m_bevel;
                    float x12 = ActualSize.X + 100f;
                    float x13 = ActualSize.X / 4f;
                    float x14 = ActualSize.X * 3f / 4f;
                    float y9 = -100f;
                    float y10 = ActualSize.Y / 2f - m_spacing / 2f + m_bevel;
                    float y11 = ActualSize.Y / 2f + m_spacing / 2f - m_bevel;
                    float y12 = ActualSize.Y + 100f;
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        new Vector2(x9, y9),
                        new Vector2(x10, y10),
                        0f,
                        0f - m_bevel,
                        Color.Transparent,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        new Vector2(x11, y9),
                        new Vector2(x12, y10),
                        0f,
                        0f - m_bevel,
                        Color.Transparent,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                    BevelledRectangleWidget.QueueBevelledRectangle(
                        null,
                        flatBatch2D,
                        new Vector2(x13, y11),
                        new Vector2(x14, y12),
                        0f,
                        0f - m_bevel,
                        Color.Transparent,
                        color,
                        Color.Transparent,
                        num,
                        directionalLight,
                        0f
                    );
                    Color color4 = color * new Color(num, num, num, 1f);
                    flatBatch2D.QueueQuad(new Vector2(x10, y9), new Vector2(x11, y10), 0f, color4);
                    flatBatch2D.QueueQuad(new Vector2(x9, y10), new Vector2(x12, y11), 0f, color4);
                    flatBatch2D.QueueQuad(new Vector2(x9, y11), new Vector2(x13, y12), 0f, color4);
                    flatBatch2D.QueueQuad(new Vector2(x14, y11), new Vector2(x12, y12), 0f, color4);
                }
            }
            else if (Children.Count == 4) {
                float x15 = -100f;
                float x16 = ActualSize.X / 2f - m_spacing / 2f + m_bevel;
                float x17 = ActualSize.X / 2f + m_spacing / 2f - m_bevel;
                float x18 = ActualSize.X + 100f;
                float y13 = -100f;
                float y14 = ActualSize.Y / 2f - m_spacing / 2f + m_bevel;
                float y15 = ActualSize.Y / 2f + m_spacing / 2f - m_bevel;
                float y16 = ActualSize.Y + 100f;
                BevelledRectangleWidget.QueueBevelledRectangle(
                    null,
                    flatBatch2D,
                    new Vector2(x15, y13),
                    new Vector2(x16, y14),
                    0f,
                    0f - m_bevel,
                    Color.Transparent,
                    color,
                    Color.Transparent,
                    num,
                    directionalLight,
                    0f
                );
                BevelledRectangleWidget.QueueBevelledRectangle(
                    null,
                    flatBatch2D,
                    new Vector2(x17, y13),
                    new Vector2(x18, y14),
                    0f,
                    0f - m_bevel,
                    Color.Transparent,
                    color,
                    Color.Transparent,
                    num,
                    directionalLight,
                    0f
                );
                BevelledRectangleWidget.QueueBevelledRectangle(
                    null,
                    flatBatch2D,
                    new Vector2(x15, y15),
                    new Vector2(x16, y16),
                    0f,
                    0f - m_bevel,
                    Color.Transparent,
                    color,
                    Color.Transparent,
                    num,
                    directionalLight,
                    0f
                );
                BevelledRectangleWidget.QueueBevelledRectangle(
                    null,
                    flatBatch2D,
                    new Vector2(x17, y15),
                    new Vector2(x18, y16),
                    0f,
                    0f - m_bevel,
                    Children.Count == 3 ? color : Color.Transparent,
                    color,
                    Color.Transparent,
                    num,
                    directionalLight,
                    0f
                );
                Color color5 = color * new Color(num, num, num, 1f);
                flatBatch2D.QueueQuad(new Vector2(x16, y13), new Vector2(x17, y16), 0f, color5);
                flatBatch2D.QueueQuad(new Vector2(x15, y14), new Vector2(x18, y15), 0f, color5);
            }
            else if (Children.Count > 4) {
                throw new InvalidOperationException("Too many GameWidgets.");
            }
            flatBatch2D.TransformTriangles(GlobalTransform, count);
        }
    }
}