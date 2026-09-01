using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentAimingSights : Component, IUpdateable, IDrawable {
        public ComponentPlayer m_componentPlayer;

        public readonly PrimitivesRenderer2D m_primitivesRenderer2D = new();

        public readonly PrimitivesRenderer3D m_primitivesRenderer3D = new();

        public Vector3 m_sightsPosition;

        public Vector3 m_sightsDirection;

        public static int[] m_drawOrders = [2000];

        public bool IsSightsVisible { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Reset;

        public int[] DrawOrders => m_drawOrders;

        public virtual void ShowAimingSights(Vector3 position, Vector3 direction) {
            IsSightsVisible = true;
            m_sightsPosition = position;
            m_sightsDirection = direction;
        }

        public virtual void Update(float dt) {
            IsSightsVisible = false;
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            if (camera.GameWidget != m_componentPlayer.GameWidget) {
                return;
            }
            if (m_componentPlayer.ComponentHealth.Health > 0f
                && m_componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible) {
                if (IsSightsVisible) {
                    Texture2D texture = ContentManager.Get<Texture2D>("Textures/Gui/Sights");
                    float s = camera.Eye == null ? 8f : 2.5f;
                    Vector3 v = m_sightsPosition + m_sightsDirection * 50f;
                    Vector3 vector = Vector3.Normalize(Vector3.Cross(m_sightsDirection, Vector3.UnitY));
                    Vector3 v2 = Vector3.Normalize(Vector3.Cross(m_sightsDirection, vector));
                    Vector3 p = v + s * (-vector - v2);
                    Vector3 p2 = v + s * (vector - v2);
                    Vector3 p3 = v + s * (vector + v2);
                    Vector3 p4 = v + s * (-vector + v2);
                    TexturedBatch3D texturedBatch3D = m_primitivesRenderer3D.TexturedBatch(texture, false, 0, DepthStencilState.None);
                    int count = texturedBatch3D.TriangleVertices.Count;
                    texturedBatch3D.QueueQuad(
                        p,
                        p2,
                        p3,
                        p4,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0f),
                        new Vector2(1f, 1f),
                        new Vector2(0f, 1f),
                        Color.White
                    );
                    texturedBatch3D.TransformTriangles(camera.ViewMatrix, count);
                }
                bool isCrosshairVisible = !SettingsManager.HideCrosshair;
                ModsManager.HookAction(
                    "IsCrosshairVisible",
                    loader => {
                        loader.IsCrosshairVisible(this, ref isCrosshairVisible);
                        return false;
                    }
                );
                if (isCrosshairVisible
                    && camera.Eye == null
                    && !camera.UsesMovementControls
                    && !IsSightsVisible
                    && (SettingsManager.LookControlMode == LookControlMode.SplitTouch || !m_componentPlayer.ComponentInput.IsControlledByTouch)) {
                    Subtexture subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Crosshair");
                    float s2 = 1.25f;
                    Vector3 v3 = camera.ViewPosition + camera.ViewDirection * 50f;
                    Vector3 vector2 = Vector3.Normalize(Vector3.Cross(camera.ViewDirection, Vector3.UnitY));
                    Vector3 v4 = Vector3.Normalize(Vector3.Cross(camera.ViewDirection, vector2));
                    Vector3 p5 = v3 + s2 * (-vector2 - v4);
                    Vector3 p6 = v3 + s2 * (vector2 - v4);
                    Vector3 p7 = v3 + s2 * (vector2 + v4);
                    Vector3 p8 = v3 + s2 * (-vector2 + v4);
                    TexturedBatch3D texturedBatch3D2 = m_primitivesRenderer3D.TexturedBatch(subtexture.Texture, false, 0, DepthStencilState.None);
                    int count2 = texturedBatch3D2.TriangleVertices.Count;
                    texturedBatch3D2.QueueQuad(
                        p5,
                        p6,
                        p7,
                        p8,
                        new Vector2(subtexture.TopLeft.X, subtexture.TopLeft.Y),
                        new Vector2(subtexture.BottomRight.X, subtexture.TopLeft.Y),
                        new Vector2(subtexture.BottomRight.X, subtexture.BottomRight.Y),
                        new Vector2(subtexture.TopLeft.X, subtexture.BottomRight.Y),
                        Color.White
                    );
                    texturedBatch3D2.TransformTriangles(camera.ViewMatrix, count2);
                }
            }
            m_primitivesRenderer2D.Flush();
            m_primitivesRenderer3D.Flush(camera.ProjectionMatrix);
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
        }
    }
}