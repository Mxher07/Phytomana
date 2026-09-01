using Engine;
using Engine.Graphics;

namespace Game {
    public class FurnitureDesignWidget : Widget {
        public enum ViewMode {
            Side = 0,
            Top = 1,
            Front = 2,
            Perspective = 3
        }

        public static string fName = "FurnitureDesignWidget";

        public PrimitivesRenderer2D m_primitivesRenderer2d = new();

        public PrimitivesRenderer3D m_primitivesRenderer3d = new();

        public Vector2? m_dragStartPoint;

        public Vector3 m_direction;

        public Vector2 m_rotationSpeed;

        public static bool DrawDebugFurniture;

        public Vector2 Size { get; set; }

        public ViewMode Mode { get; set; }

        public FurnitureDesign Design { get; set; }

        public FurnitureDesignWidget() {
            ClampToBounds = true;
            Size = new Vector2(float.PositiveInfinity);
            Mode = ViewMode.Perspective;
            m_direction = Vector3.Normalize(new Vector3(1f, -0.5f, -1f));
            m_rotationSpeed = new Vector2(2f, 0.5f);
        }

        public override void Draw(DrawContext dc) {
            if (Design == null) {
                return;
            }
            Matrix matrix;
            if (Mode == ViewMode.Perspective) {
                Viewport viewport = Display.Viewport;
                Vector3 vector = new(0.5f, 0.5f, 0.5f);
                Matrix m = Matrix.CreateLookAt(2.65f * m_direction + vector, vector, Vector3.UnitY);
                Matrix m2 = Matrix.CreatePerspectiveFieldOfView(1.2f, ActualSize.X / ActualSize.Y, 0.4f, 4f);
                Matrix m3 = MatrixUtils.CreateScaleTranslation(ActualSize.X, 0f - ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f)
                    * GlobalTransform
                    * MatrixUtils.CreateScaleTranslation(2f / viewport.Width, -2f / viewport.Height, -1f, 1f);
                matrix = m * m2 * m3;
                FlatBatch3D flatBatch3D = m_primitivesRenderer3d.FlatBatch(1, DepthStencilState.DepthRead);
                for (int i = 0; i <= Design.Resolution; i++) {
                    float num = i / (float)Design.Resolution;
                    Color color = i % 2 == 0 ? new Color(56, 56, 56, 56) : new Color(28, 28, 28, 28);
                    color *= GlobalColorTransform;
                    flatBatch3D.QueueLine(new Vector3(num, 0f, 0f), new Vector3(num, 0f, 1f), color);
                    flatBatch3D.QueueLine(new Vector3(0f, 0f, num), new Vector3(1f, 0f, num), color);
                    flatBatch3D.QueueLine(new Vector3(0f, num, 0f), new Vector3(0f, num, 1f), color);
                    flatBatch3D.QueueLine(new Vector3(0f, 0f, num), new Vector3(0f, 1f, num), color);
                    flatBatch3D.QueueLine(new Vector3(0f, num, 1f), new Vector3(1f, num, 1f), color);
                    flatBatch3D.QueueLine(new Vector3(num, 0f, 1f), new Vector3(num, 1f, 1f), color);
                }
                Color color2 = new Color(64, 64, 64, 255) * GlobalColorTransform;
                FontBatch3D fontBatch3D = m_primitivesRenderer3d.FontBatch(LabelWidget.BitmapFont, 1);
                fontBatch3D.QueueText(
                    LanguageControl.Get(fName, 0),
                    new Vector3(0.5f, 0f, 0f),
                    0.004f * new Vector3(-1f, 0f, 0f),
                    0.004f * new Vector3(0f, 0f, -1f),
                    color2,
                    TextAnchor.HorizontalCenter
                );
                fontBatch3D.QueueText(
                    LanguageControl.Get(fName, 1),
                    new Vector3(1f, 0f, 0.5f),
                    0.004f * new Vector3(0f, 0f, -1f),
                    0.004f * new Vector3(1f, 0f, 0f),
                    color2,
                    TextAnchor.HorizontalCenter
                );
                if (DrawDebugFurniture) {
                    DebugDraw();
                }
            }
            else {
                Vector3 position;
                Vector3 up;
                if (Mode == ViewMode.Side) {
                    position = new Vector3(1f, 0f, 0f);
                    up = new Vector3(0f, 1f, 0f);
                }
                else if (Mode != ViewMode.Top) {
                    position = new Vector3(0f, 0f, -10f);
                    up = new Vector3(0f, 1f, 0f);
                }
                else {
                    position = new Vector3(0f, 1f, 0f);
                    up = new Vector3(0f, 0f, 1f);
                }
                Viewport viewport2 = Display.Viewport;
                float num2 = MathUtils.Min(ActualSize.X, ActualSize.Y);
                Matrix m4 = Matrix.CreateLookAt(position, new Vector3(0f, 0f, 0f), up);
                Matrix m5 = Matrix.CreateOrthographic(2f, 2f, -10f, 10f);
                Matrix m6 = MatrixUtils.CreateScaleTranslation(num2, 0f - num2, ActualSize.X / 2f, ActualSize.Y / 2f)
                    * GlobalTransform
                    * MatrixUtils.CreateScaleTranslation(2f / viewport2.Width, -2f / viewport2.Height, -1f, 1f);
                matrix = Matrix.CreateTranslation(-0.5f, -0.5f, -0.5f) * m4 * m5 * m6;
                FlatBatch2D flatBatch2D = m_primitivesRenderer2d.FlatBatch();
                Matrix m7 = GlobalTransform;
                for (int j = 1; j < Design.Resolution; j++) {
                    float num3 = j / (float)Design.Resolution;
                    Vector2 v = new(ActualSize.X * num3, 0f);
                    Vector2 v2 = new(ActualSize.X * num3, ActualSize.Y);
                    Vector2 v3 = new(0f, ActualSize.Y * num3);
                    Vector2 v4 = new(ActualSize.X, ActualSize.Y * num3);
                    Vector2.Transform(ref v, ref m7, out v);
                    Vector2.Transform(ref v2, ref m7, out v2);
                    Vector2.Transform(ref v3, ref m7, out v3);
                    Vector2.Transform(ref v4, ref m7, out v4);
                    Color color3 = j % 2 == 0 ? new Color(0, 0, 0, 56) : new Color(0, 0, 0, 28);
                    Color color4 = j % 2 == 0 ? new Color(56, 56, 56, 56) : new Color(28, 28, 28, 28);
                    color3 *= GlobalColorTransform;
                    color4 *= GlobalColorTransform;
                    flatBatch2D.QueueLine(v, v2, 0f, j % 2 == 0 ? color3 : color3 * 0.75f);
                    flatBatch2D.QueueLine(v + new Vector2(1f, 0f), v2 + new Vector2(1f, 0f), 0f, color4);
                    flatBatch2D.QueueLine(v3, v4, 0f, color3);
                    flatBatch2D.QueueLine(v3 + new Vector2(0f, 1f), v4 + new Vector2(0f, 1f), 0f, color4);
                }
            }
            Matrix matrix2 = Matrix.Identity;
            FurnitureGeometry geometry = Design.Geometry;
            for (int k = 0; k < 6; k++) {
                Color globalColorTransform = GlobalColorTransform;
                if (Mode == ViewMode.Perspective) {
                    float num4 = LightingManager.LightIntensityByLightValueAndFace[15 + 16 * CellFace.OppositeFace(k)];
                    globalColorTransform *= new Color(num4, num4, num4);
                }
                if (geometry.SubsetOpaqueByFace[k] != null) {
                    BlocksManager.DrawMeshBlock(m_primitivesRenderer3d, geometry.SubsetOpaqueByFace[k], globalColorTransform, 1f, ref matrix2, null);
                }
                if (geometry.SubsetAlphaTestByFace[k] != null) {
                    BlocksManager.DrawMeshBlock(
                        m_primitivesRenderer3d,
                        geometry.SubsetAlphaTestByFace[k],
                        globalColorTransform,
                        1f,
                        ref matrix2,
                        null
                    );
                }
                if (geometry.SubsetTransparentByFace[k] != null) {
                    BlocksManager.DrawMeshBlock(m_primitivesRenderer3d, geometry.SubsetTransparentByFace[k], globalColorTransform, 1f, ref matrix2, null, GeometrySubsetType.Transparent);
                }
            }
            // 渲染使用自定义纹理的家具几何体
            if (geometry.Draws != null) {
                foreach (var kv in geometry.Draws) {
                    FurnitureGeometry subGeo = kv.Value;
                    Texture2D tex = kv.Key;
                    for (int k = 0; k < 6; k++) {
                        Color globalColorTransform = GlobalColorTransform;
                        if (Mode == ViewMode.Perspective) {
                            float num4 = LightingManager.LightIntensityByLightValueAndFace[15 + 16 * CellFace.OppositeFace(k)];
                            globalColorTransform *= new Color(num4, num4, num4);
                        }
                        if (subGeo.SubsetOpaqueByFace[k] != null) {
                            BlocksManager.DrawMeshBlock(m_primitivesRenderer3d, subGeo.SubsetOpaqueByFace[k], tex, globalColorTransform, 1f, ref matrix2, null);
                        }
                        if (subGeo.SubsetAlphaTestByFace[k] != null) {
                            BlocksManager.DrawMeshBlock(m_primitivesRenderer3d, subGeo.SubsetAlphaTestByFace[k], tex, globalColorTransform, 1f, ref matrix2, null);
                        }
                        if (subGeo.SubsetTransparentByFace[k] != null) {
                            BlocksManager.DrawMeshBlock(m_primitivesRenderer3d, subGeo.SubsetTransparentByFace[k], tex, globalColorTransform, 1f, ref matrix2, null, GeometrySubsetType.Transparent);
                        }
                    }
                }
            }
            m_primitivesRenderer3d.Flush(matrix);
            m_primitivesRenderer2d.Flush();
        }

        public override void Update() {
            if (Mode != ViewMode.Perspective) {
                return;
            }
            if (Input.Tap.HasValue
                && HitTestGlobal(Input.Tap.Value) == this) {
                m_dragStartPoint = Input.Tap;
            }
            if (Input.Press.HasValue) {
                if (m_dragStartPoint.HasValue) {
                    Vector2 vector = ScreenToWidget(Input.Press.Value) - ScreenToWidget(m_dragStartPoint.Value);
                    Vector2 vector2 = default;
                    vector2.Y = -0.01f * vector.X;
                    vector2.X = 0.01f * vector.Y;
                    if (Time.FrameDuration > 0f) {
                        m_rotationSpeed = vector2 / Time.FrameDuration;
                    }
                    Rotate(vector2);
                    m_dragStartPoint = Input.Press;
                }
            }
            else {
                m_dragStartPoint = null;
                Rotate(m_rotationSpeed * Time.FrameDuration);
                m_rotationSpeed *= MathF.Pow(0.1f, Time.FrameDuration);
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = Design != null;
            DesiredSize = Size;
        }

        public void Rotate(Vector2 angles) {
            float num = MathUtils.DegToRad(1f);
            Vector3 axis = Vector3.Normalize(Vector3.Cross(m_direction, Vector3.UnitY));
            m_direction = Vector3.TransformNormal(m_direction, Matrix.CreateRotationY(angles.Y));
            float num2 = MathF.Acos(Vector3.Dot(m_direction, Vector3.UnitY));
            float num3 = MathF.Acos(Vector3.Dot(m_direction, -Vector3.UnitY));
            angles.X = MathF.Min(angles.X, num2 - num);
            angles.X = MathF.Max(angles.X, 0f - (num3 - num));
            m_direction = Vector3.TransformNormal(m_direction, Matrix.CreateFromAxisAngle(axis, angles.X));
            m_direction = Vector3.Normalize(m_direction);
        }

        public void DebugDraw() { }
    }
}