using Engine;
using Engine.Graphics;
using Engine.Input;

namespace Game {
    public class MoveRoseWidget : Widget {
        public Vector3 m_direction;

        public bool m_jump;

        public int? m_jumpTouchId;

        public Vector3 Direction {
            get {
                if (IsEnabledGlobal && IsVisibleGlobal) {
                    return m_direction;
                }
                return Vector3.Zero;
            }
        }

        public bool Jump {
            get {
                if (IsEnabledGlobal && IsVisibleGlobal) {
                    return m_jump;
                }
                return false;
            }
        }

        public override void Update() {
            m_direction = Vector3.Zero;
            m_jump = false;
            Vector2 v = ActualSize / 2f;
            float num = ActualSize.X / 2f;
            float num2 = num / 3.5f;
            float num3 = MathUtils.DegToRad(35f);
            foreach (TouchLocation touchLocation in Input.TouchLocations) {
                if (HitTestGlobal(touchLocation.Position) == this) {
                    if (touchLocation.State == TouchLocationState.Pressed
                        && Vector2.Distance(ScreenToWidget(touchLocation.Position), v) <= num2) {
                        m_jump = true;
                        m_jumpTouchId = touchLocation.Id;
                    }
                    if (touchLocation.State == TouchLocationState.Released
                        && m_jumpTouchId.HasValue
                        && touchLocation.Id == m_jumpTouchId.Value) {
                        m_jumpTouchId = null;
                    }
                    if (touchLocation.State == TouchLocationState.Moved
                        || touchLocation.State == TouchLocationState.Pressed) {
                        Vector2 v2 = ScreenToWidget(touchLocation.Position);
                        float num4 = Vector2.Distance(v2, v);
                        if (num4 > num2
                            && num4 <= num) {
                            float num5 = Vector2.Angle(v2 - v, -Vector2.UnitY);
                            if (MathF.Abs(MathUtils.NormalizeAngle(num5 - 0f)) < num3) {
                                m_direction = m_jumpTouchId.HasValue ? new Vector3(0f, 1f, 0f) : new Vector3(0f, 0f, 1f);
                            }
                            else if (MathF.Abs(MathUtils.NormalizeAngle(num5 - (float)Math.PI / 2f)) < num3) {
                                m_direction = new Vector3(-1f, 0f, 0f);
                            }
                            else if (MathF.Abs(MathUtils.NormalizeAngle(num5 - (float)Math.PI)) < num3) {
                                m_direction = m_jumpTouchId.HasValue ? new Vector3(0f, -1f, 0f) : new Vector3(0f, 0f, -1f);
                            }
                            else if (MathF.Abs(MathUtils.NormalizeAngle(num5 - 4.712389f)) < num3) {
                                m_direction = new Vector3(1f, 0f, 0f);
                            }
                        }
                    }
                }
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
        }

        public override void Draw(DrawContext dc) {
            Subtexture subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/MoveRose");
            Subtexture subtexture2 = ContentManager.Get<Subtexture>("Textures/Atlas/MoveRose_Pressed");
            TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(subtexture.Texture);
            TexturedBatch2D texturedBatch2D2 = dc.PrimitivesRenderer2D.TexturedBatch(subtexture2.Texture);
            int count = texturedBatch2D.TriangleVertices.Count;
            int count2 = texturedBatch2D2.TriangleVertices.Count;
            Vector2 p = ActualSize / 2f;
            Vector2 vector = new(0f, 0f);
            Vector2 vector2 = new(ActualSize.X, 0f);
            Vector2 vector3 = new(ActualSize.X, ActualSize.Y);
            Vector2 vector4 = new(0f, ActualSize.Y);
            if (m_direction.Z > 0f) {
                Vector2 subtextureCoords = GetSubtextureCoords(subtexture2, new Vector2(0f, 0f));
                Vector2 subtextureCoords2 = GetSubtextureCoords(subtexture2, new Vector2(1f, 0f));
                Vector2 subtextureCoords3 = GetSubtextureCoords(subtexture2, new Vector2(0.5f, 0.5f));
                texturedBatch2D2.QueueTriangle(
                    vector,
                    vector2,
                    p,
                    0f,
                    subtextureCoords,
                    subtextureCoords2,
                    subtextureCoords3,
                    GlobalColorTransform
                );
            }
            else {
                Vector2 subtextureCoords4 = GetSubtextureCoords(subtexture, new Vector2(0f, 0f));
                Vector2 subtextureCoords5 = GetSubtextureCoords(subtexture, new Vector2(1f, 0f));
                Vector2 subtextureCoords6 = GetSubtextureCoords(subtexture, new Vector2(0.5f, 0.5f));
                texturedBatch2D.QueueTriangle(
                    vector,
                    vector2,
                    p,
                    0f,
                    subtextureCoords4,
                    subtextureCoords5,
                    subtextureCoords6,
                    GlobalColorTransform
                );
            }
            if (m_direction.X > 0f) {
                Vector2 subtextureCoords7 = GetSubtextureCoords(subtexture2, new Vector2(1f, 0f));
                Vector2 subtextureCoords8 = GetSubtextureCoords(subtexture2, new Vector2(1f, 1f));
                Vector2 subtextureCoords9 = GetSubtextureCoords(subtexture2, new Vector2(0.5f, 0.5f));
                texturedBatch2D2.QueueTriangle(
                    vector2,
                    vector3,
                    p,
                    0f,
                    subtextureCoords7,
                    subtextureCoords8,
                    subtextureCoords9,
                    GlobalColorTransform
                );
            }
            else {
                Vector2 subtextureCoords10 = GetSubtextureCoords(subtexture, new Vector2(1f, 0f));
                Vector2 subtextureCoords11 = GetSubtextureCoords(subtexture, new Vector2(1f, 1f));
                Vector2 subtextureCoords12 = GetSubtextureCoords(subtexture, new Vector2(0.5f, 0.5f));
                texturedBatch2D.QueueTriangle(
                    vector2,
                    vector3,
                    p,
                    0f,
                    subtextureCoords10,
                    subtextureCoords11,
                    subtextureCoords12,
                    GlobalColorTransform
                );
            }
            if (m_direction.Z < 0f) {
                Vector2 subtextureCoords13 = GetSubtextureCoords(subtexture2, new Vector2(1f, 1f));
                Vector2 subtextureCoords14 = GetSubtextureCoords(subtexture2, new Vector2(0f, 1f));
                Vector2 subtextureCoords15 = GetSubtextureCoords(subtexture2, new Vector2(0.5f, 0.5f));
                texturedBatch2D2.QueueTriangle(
                    vector3,
                    vector4,
                    p,
                    0f,
                    subtextureCoords13,
                    subtextureCoords14,
                    subtextureCoords15,
                    GlobalColorTransform
                );
            }
            else {
                Vector2 subtextureCoords16 = GetSubtextureCoords(subtexture, new Vector2(1f, 1f));
                Vector2 subtextureCoords17 = GetSubtextureCoords(subtexture, new Vector2(0f, 1f));
                Vector2 subtextureCoords18 = GetSubtextureCoords(subtexture, new Vector2(0.5f, 0.5f));
                texturedBatch2D.QueueTriangle(
                    vector3,
                    vector4,
                    p,
                    0f,
                    subtextureCoords16,
                    subtextureCoords17,
                    subtextureCoords18,
                    GlobalColorTransform
                );
            }
            if (m_direction.X < 0f) {
                Vector2 subtextureCoords19 = GetSubtextureCoords(subtexture2, new Vector2(0f, 1f));
                Vector2 subtextureCoords20 = GetSubtextureCoords(subtexture2, new Vector2(0f, 0f));
                Vector2 subtextureCoords21 = GetSubtextureCoords(subtexture2, new Vector2(0.5f, 0.5f));
                texturedBatch2D2.QueueTriangle(
                    vector4,
                    vector,
                    p,
                    0f,
                    subtextureCoords19,
                    subtextureCoords20,
                    subtextureCoords21,
                    GlobalColorTransform
                );
            }
            else {
                Vector2 subtextureCoords22 = GetSubtextureCoords(subtexture, new Vector2(0f, 1f));
                Vector2 subtextureCoords23 = GetSubtextureCoords(subtexture, new Vector2(0f, 0f));
                Vector2 subtextureCoords24 = GetSubtextureCoords(subtexture, new Vector2(0.5f, 0.5f));
                texturedBatch2D.QueueTriangle(
                    vector4,
                    vector,
                    p,
                    0f,
                    subtextureCoords22,
                    subtextureCoords23,
                    subtextureCoords24,
                    GlobalColorTransform
                );
            }
            if (texturedBatch2D == texturedBatch2D2) {
                texturedBatch2D.TransformTriangles(GlobalTransform, count);
                return;
            }
            texturedBatch2D.TransformTriangles(GlobalTransform, count);
            texturedBatch2D2.TransformTriangles(GlobalTransform, count2);
        }

        public static Vector2 GetSubtextureCoords(Subtexture subtexture, Vector2 texCoords) => new(
            MathUtils.Lerp(subtexture.TopLeft.X, subtexture.BottomRight.X, texCoords.X),
            MathUtils.Lerp(subtexture.TopLeft.Y, subtexture.BottomRight.Y, texCoords.Y)
        );
    }
}