using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentGlowingEyes : Component, IDrawable {
        public SubsystemGlow m_subsystemGlow;

        public SubsystemTerrain m_subsystemTerrain;

        public ComponentCreatureModel m_componentCreatureModel;

        public GlowPoint[] m_eyeGlowPoints = new GlowPoint[2];

        public static int[] m_drawOrders = new int[1];

        public Vector3 GlowingEyesOffset { get; set; }

        public Color GlowingEyesColor { get; set; }

        public int[] DrawOrders => m_drawOrders;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGlow = Project.FindSubsystem<SubsystemGlow>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_componentCreatureModel = Entity.FindComponent<ComponentCreatureModel>(true);
            GlowingEyesOffset = valuesDictionary.GetValue<Vector3>("GlowingEyesOffset");
            GlowingEyesColor = valuesDictionary.GetValue<Color>("GlowingEyesColor");
        }

        public override void OnEntityAdded() {
            for (int i = 0; i < m_eyeGlowPoints.Length; i++) {
                m_eyeGlowPoints[i] = m_subsystemGlow.AddGlowPoint();
            }
        }

        public override void OnEntityRemoved() {
            for (int i = 0; i < m_eyeGlowPoints.Length; i++) {
                m_subsystemGlow.RemoveGlowPoint(m_eyeGlowPoints[i]);
            }
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            if (m_eyeGlowPoints[0] == null
                || !m_componentCreatureModel.IsVisibleForCamera) {
                return;
            }
            m_eyeGlowPoints[0].Color = Color.Transparent;
            m_eyeGlowPoints[1].Color = Color.Transparent;
            ModelBone modelBone = m_componentCreatureModel.Model.FindBone("Head", false);
            if (modelBone == null) {
                return;
            }
            Matrix matrix = m_componentCreatureModel.AbsoluteBoneTransformsForCamera[modelBone.Index];
            matrix *= camera.InvertedViewMatrix;
            Vector3 vector = Vector3.Normalize(matrix.Up);
            float num = Vector3.Dot(matrix.Translation - camera.ViewPosition, camera.ViewDirection);
            if (num > 0f) {
                Vector3 translation = matrix.Translation;
                int cellLight = m_subsystemTerrain.Terrain.GetCellLight(
                    Terrain.ToCell(translation.X),
                    Terrain.ToCell(translation.Y),
                    Terrain.ToCell(translation.Z)
                );
                float num2 = LightingManager.LightIntensityByLightValue[cellLight];
                float num3 = 0f - Vector3.Dot(vector, Vector3.Normalize(matrix.Translation - camera.ViewPosition)) > 0.7f ? 1 : 0;
                num3 *= MathUtils.Saturate(1f * (num - 1f));
                num3 *= MathUtils.Saturate((1f - num2 - 0.5f) / 0.5f);
                if (num3 > 0.25f) {
                    Vector3 vector2 = Vector3.Normalize(matrix.Right);
                    Vector3 vector3 = -Vector3.Normalize(matrix.Forward);
                    Color color = GlowingEyesColor * num3;
                    m_eyeGlowPoints[0].Position = translation
                        + vector2 * GlowingEyesOffset.X
                        + vector3 * GlowingEyesOffset.Y
                        + vector * GlowingEyesOffset.Z;
                    m_eyeGlowPoints[0].Right = vector2;
                    m_eyeGlowPoints[0].Up = vector3;
                    m_eyeGlowPoints[0].Forward = vector;
                    m_eyeGlowPoints[0].Size = 0.01f;
                    m_eyeGlowPoints[0].FarSize = 0.06f;
                    m_eyeGlowPoints[0].FarDistance = 14f;
                    m_eyeGlowPoints[0].Color = color;
                    m_eyeGlowPoints[1].Position = translation
                        - vector2 * GlowingEyesOffset.X
                        + vector3 * GlowingEyesOffset.Y
                        + vector * GlowingEyesOffset.Z;
                    m_eyeGlowPoints[1].Right = vector2;
                    m_eyeGlowPoints[1].Up = vector3;
                    m_eyeGlowPoints[1].Forward = vector;
                    m_eyeGlowPoints[1].Size = 0.01f;
                    m_eyeGlowPoints[1].FarSize = 0.06f;
                    m_eyeGlowPoints[1].FarDistance = 14f;
                    m_eyeGlowPoints[1].Color = color;
                }
            }
        }
    }
}