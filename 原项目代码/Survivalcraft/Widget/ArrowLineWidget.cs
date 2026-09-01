using Engine;
using Engine.Graphics;
using Engine.Serialization;

namespace Game {
    public class ArrowLineWidget : Widget {
        public string m_pointsString;

        public float m_width;

        public float m_arrowWidth;

        public bool m_absoluteCoordinates;

        public List<Vector2> m_vertices = [];

        public bool m_parsingPending;

        public Vector2 m_startOffset;

        public float Width {
            get => m_width;
            set {
                m_width = value;
                m_parsingPending = true;
            }
        }

        public float ArrowWidth {
            get => m_arrowWidth;
            set {
                m_arrowWidth = value;
                m_parsingPending = true;
            }
        }

        public Color Color { get; set; }

        public string PointsString {
            get => m_pointsString;
            set {
                m_pointsString = value;
                m_parsingPending = true;
            }
        }

        public bool AbsoluteCoordinates {
            get => m_absoluteCoordinates;
            set {
                m_absoluteCoordinates = value;
                m_parsingPending = true;
            }
        }

        public ArrowLineWidget() {
            Width = 6f;
            ArrowWidth = 0f;
            Color = Color.White;
            IsHitTestVisible = false;
            PointsString = "0, 0; 50, 0";
        }

        public override void Draw(DrawContext dc) {
            if (m_parsingPending) {
                ParsePoints();
            }
            Color color = Color * GlobalColorTransform;
            FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.None);
            int count = flatBatch2D.TriangleVertices.Count;
            for (int i = 0; i < m_vertices.Count; i += 3) {
                Vector2 p = m_startOffset + m_vertices[i];
                Vector2 p2 = m_startOffset + m_vertices[i + 1];
                Vector2 p3 = m_startOffset + m_vertices[i + 2];
                flatBatch2D.QueueTriangle(p, p2, p3, 0f, color);
            }
            flatBatch2D.TransformTriangles(GlobalTransform, count);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            if (m_parsingPending) {
                ParsePoints();
            }
            IsDrawRequired = Color.A > 0 && Width > 0f;
        }

        public void ParsePoints() {
            m_parsingPending = false;
            List<Vector2> list = new();
            string[] array = m_pointsString.Split([";"], StringSplitOptions.None);
            foreach (string data in array) {
                list.Add(HumanReadableConverter.ConvertFromString<Vector2>(data));
            }
            m_vertices.Clear();
            for (int j = 0; j < list.Count; j++) {
                if (j >= 1) {
                    Vector2 vector = list[j - 1];
                    Vector2 vector2 = list[j];
                    Vector2 vector3 = Vector2.Normalize(vector2 - vector);
                    Vector2 vector4 = vector3;
                    Vector2 v = vector3;
                    if (j >= 2) {
                        vector4 = Vector2.Normalize(vector - list[j - 2]);
                    }
                    if (j <= list.Count - 2) {
                        v = Vector2.Normalize(list[j + 1] - vector2);
                    }
                    Vector2 v2 = Vector2.Perpendicular(vector4);
                    Vector2 v3 = Vector2.Perpendicular(vector3);
                    float num = (float)Math.PI - Vector2.Angle(vector4, vector3);
                    float s = 0.5f * Width / MathF.Tan(num / 2f);
                    Vector2 v4 = 0.5f * v2 * Width - vector4 * s;
                    float num2 = (float)Math.PI - Vector2.Angle(vector3, v);
                    float s2 = 0.5f * Width / MathF.Tan(num2 / 2f);
                    Vector2 v5 = 0.5f * v3 * Width - vector3 * s2;
                    m_vertices.Add(vector + v4);
                    m_vertices.Add(vector - v4);
                    m_vertices.Add(vector2 - v5);
                    m_vertices.Add(vector2 - v5);
                    m_vertices.Add(vector2 + v5);
                    m_vertices.Add(vector + v4);
                    if (j == list.Count - 1) {
                        m_vertices.Add(vector2 - 0.5f * ArrowWidth * v3);
                        m_vertices.Add(vector2 + 0.5f * ArrowWidth * v3);
                        m_vertices.Add(vector2 + 0.5f * ArrowWidth * vector3);
                    }
                }
            }
            if (m_vertices.Count > 0) {
                float? num3 = null;
                float? num4 = null;
                float? num5 = null;
                float? num6 = null;
                for (int k = 0; k < m_vertices.Count; k++) {
                    if (!num3.HasValue
                        || m_vertices[k].X < num3) {
                        num3 = m_vertices[k].X;
                    }
                    if (!num4.HasValue
                        || m_vertices[k].Y < num4) {
                        num4 = m_vertices[k].Y;
                    }
                    if (!num5.HasValue
                        || m_vertices[k].X > num5) {
                        num5 = m_vertices[k].X;
                    }
                    if (!num6.HasValue
                        || m_vertices[k].Y > num6) {
                        num6 = m_vertices[k].Y;
                    }
                }
                if (AbsoluteCoordinates) {
                    DesiredSize = new Vector2(num5.Value, num6.Value);
                    m_startOffset = Vector2.Zero;
                }
                else {
                    DesiredSize = new Vector2(num5.Value - num3.Value, num6.Value - num4.Value);
                    m_startOffset = -new Vector2(num3.Value, num4.Value);
                }
            }
            else {
                DesiredSize = Vector2.Zero;
                m_startOffset = Vector2.Zero;
            }
        }
    }
}