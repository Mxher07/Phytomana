using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentDiggingCracks : Component, IDrawable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemSky m_subsystemSky;

        public ComponentMiner m_componentMiner;

        public Texture2D[] m_textures;

        public Shader m_shader;

        public Geometry m_geometry;

        DynamicArray<TerrainVertex> m_vertices = [];
        DynamicArray<int> m_indices = [];

        public Point3 m_point;

        public int m_value;

        public static int[] m_drawOrders = [1];

        public int[] DrawOrders => m_drawOrders;

        public virtual void Draw(Camera camera, int drawOrder) {
            if (!m_componentMiner.DigCellFace.HasValue
                || !(m_componentMiner.DigProgress > 0f)
                || !(m_componentMiner.DigTime > 0.2f)) {
                return;
            }
            Point3 point = m_componentMiner.DigCellFace.Value.Point;
            int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
            if (m_geometry == null
                || cellValue != m_value
                || point != m_point) {
                m_geometry = new Geometry(m_textures[0]); //这里随便默认一个纹理就行
                block.GenerateTerrainVertices(m_subsystemTerrain.BlockGeometryGenerator, m_geometry, cellValue, point.X, point.Y, point.Z);
                m_point = point;
                m_value = cellValue;
                m_vertices.Count = 0;
                m_indices.Count = 0;
                foreach (KeyValuePair<Texture2D, TerrainGeometry> drawGroup in m_geometry.Draws) {
                    foreach (TerrainGeometrySubset geometry in drawGroup.Value.Subsets) {
                        foreach (int index in geometry.Indices) {
                            m_indices.Add(index + m_vertices.Count);
                        }
                        foreach (TerrainVertex vertex in geometry.Vertices) {
                            TerrainVertex terrainVertex = block.SetDiggingCrackingTextureTransform(vertex);
                            m_vertices.Add(terrainVertex);
                        }
                    }
                }
            }
            Vector3 viewPosition = camera.InvertedViewMatrix.Translation;
            Vector3 v = new(MathF.Floor(viewPosition.X), 0f, MathF.Floor(viewPosition.Z));
            Matrix value = Matrix.CreateTranslation(v - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
            try {
                Display.BlendState = BlendState.NonPremultiplied;
                Display.DepthStencilState = DepthStencilState.Default;
                Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
                m_shader.GetParameter("u_origin").SetValue(v.XZ);
                m_shader.GetParameter("u_viewProjectionMatrix").SetValue(value);
                m_shader.GetParameter("u_viewPosition").SetValue(camera.ViewPosition);
                m_shader.GetParameter("u_samplerState").SetValue(SamplerState.PointWrap);
                m_shader.GetParameter("u_fogYMultiplier").SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
                m_shader.GetParameter("u_fogColor").SetValue(new Vector3(m_subsystemSky.ViewFogColor));
                m_shader.GetParameter("u_fogBottomTopDensity")
                    .SetValue(new Vector3(m_subsystemSky.ViewFogBottom, m_subsystemSky.ViewFogTop, m_subsystemSky.ViewFogDensity));
                m_shader.GetParameter("u_hazeStartDensity").SetValue(new Vector2(m_subsystemSky.ViewHazeStart, m_subsystemSky.ViewHazeDensity));
                m_shader.GetParameter("u_alphaThreshold").SetValue(0.5f);
                m_shader.GetParameter("u_texture")
                    .SetValue(block.GetDiggingCrackingTexture(m_componentMiner, m_componentMiner.m_digProgress, cellValue, m_textures));
                Display.DrawUserIndexed(
                    PrimitiveType.TriangleList,
                    m_shader,
                    TerrainVertex.VertexDeclaration,
                    m_vertices.Array,
                    0,
                    m_vertices.Count,
                    m_indices.Array,
                    0,
                    m_indices.Count
                );
            }
            catch {
                // ignored
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_componentMiner = Entity.FindComponent<ComponentMiner>(true);
            m_shader = ContentManager.Get<Shader>("Shaders/AlphaTested");
            m_textures = new Texture2D[8];
            for (int i = 0; i < 8; i++) {
                m_textures[i] = ContentManager.Get<Texture2D>($"Textures/Cracks{i + 1}");
            }
        }

        public class Geometry(Texture2D texture2D) : TerrainGeometry(texture2D);
    }
}