using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentVrHandsModel : Component, IDrawable, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public ComponentPlayer m_componentPlayer;

        public ComponentMiner m_componentMiner;

        public Model m_vrHandModel;

        public int m_value;

        public float m_swapAnimationTime;

        public float m_pokeAnimationTime;

        public Vector3 m_itemOffset;

        public Vector3 m_itemRotation;

        public double m_nextHandLightTime;

        public float m_handLight;

        public int m_itemLight;

        public DrawBlockEnvironmentData m_drawBlockEnvironmentData = new();

        public PrimitivesRenderer3D m_primitivesRenderer = new();

        public static LitShader m_shader = new(2, false, false, true, false, false);

        public static int[] m_drawOrders = [1];

        public Vector3 ItemOffsetOrder { get; set; }

        public Vector3 ItemRotationOrder { get; set; }

        /// <summary>
        /// 强制只绘制手部模型（即使有物品也不绘制）
        /// </summary>
        public bool ForceDrawHandOnly { get; set; }

        public int[] DrawOrders => m_drawOrders;

        public UpdateOrder UpdateOrder => UpdateOrder.FirstPersonModels;

        public virtual void Draw(Camera camera, int drawOrder) {
            if (!(m_componentPlayer.ComponentHealth.Health > 0f)
                || !camera.GameWidget.IsEntityFirstPersonTarget(Entity)
                || !m_componentPlayer.ComponentInput.IsControlledByVr) {
                return;
            }
            if (!VrManager.IsControllerPresent(VrController.Right)) {
                return;
            }
            Vector3 eyePosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
            int x = Terrain.ToCell(eyePosition.X);
            int num = Terrain.ToCell(eyePosition.Y);
            int z = Terrain.ToCell(eyePosition.Z);

            // 根据绘制内容分别计算光照
            if (m_value != 0 && !ForceDrawHandOnly) {
                if (num >= 0
                    && num <= 255) {
                    TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
                    if (chunkAtCell != null
                        && chunkAtCell.State >= TerrainChunkState.InvalidVertices1) {
                        m_itemLight = m_subsystemTerrain.Terrain.GetCellLightFast(x, num, z);
                    }
                }
            }
            else {
                if (Time.FrameStartTime >= m_nextHandLightTime) {
                    float? num2 = LightingManager.CalculateSmoothLight(m_subsystemTerrain, eyePosition);
                    if (num2.HasValue) {
                        m_nextHandLightTime = Time.FrameStartTime + 0.1;
                        m_handLight = num2.Value;
                    }
                }
            }

            Matrix identity = Matrix.Identity;
            // 切换动画
            if (m_swapAnimationTime > 0f) {
                float num3 = MathF.Pow(MathF.Sin(m_swapAnimationTime * (float)Math.PI), 3f);
                identity *= Matrix.CreateTranslation(0f, -0.8f * num3, 0.2f * num3);
            }
            // 戳击动画
            if (m_pokeAnimationTime > 0f) {
                float num4 = MathF.Sin(MathF.Sqrt(m_pokeAnimationTime) * (float)Math.PI);
                if (m_value != 0 && !ForceDrawHandOnly) {
                    identity *= Matrix.CreateRotationX((0f - MathUtils.DegToRad(90f)) * num4);
                }
                else {
                    identity *= Matrix.CreateRotationX((0f - MathUtils.DegToRad(45f)) * num4);
                }
            }
            Matrix matrix = VrManager.HmdMatrixInverted
                * Matrix.CreateWorld(camera.ViewPosition, camera.ViewDirection, camera.ViewUp)
                * camera.ViewMatrix;
            Matrix controllerMatrix = VrManager.GetControllerMatrix(VrController.Right);
            if (m_value == 0 || ForceDrawHandOnly) {
                // 空手时绘制手部模型
                Display.DepthStencilState = DepthStencilState.Default;
                Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
                m_shader.Texture = m_componentPlayer.ComponentCreatureModel.TextureOverride;
                m_shader.SamplerState = SamplerState.PointClamp;
                m_shader.MaterialColor = Vector4.One;
                m_shader.AmbientLightColor = new Vector3(m_handLight * LightingManager.LightAmbient);
                m_shader.DiffuseLightColor1 = new Vector3(m_handLight);
                m_shader.DiffuseLightColor2 = new Vector3(m_handLight);
                m_shader.LightDirection1 = -Vector3.TransformNormal(LightingManager.DirectionToLight1, camera.ViewMatrix);
                m_shader.LightDirection2 = -Vector3.TransformNormal(LightingManager.DirectionToLight2, camera.ViewMatrix);
                m_shader.Transforms.View = Matrix.Identity;
                m_shader.Transforms.Projection = camera.ProjectionMatrix;
                m_shader.Transforms.World[0] = Matrix.CreateScale(0.01f) * identity * controllerMatrix * matrix;
                foreach (ModelMesh mesh in m_vrHandModel.Meshes) {
                    foreach (ModelMeshPart meshPart in mesh.MeshParts) {
                        Display.DrawIndexed(
                            PrimitiveType.TriangleList,
                            m_shader,
                            meshPart.VertexBuffer,
                            meshPart.IndexBuffer,
                            meshPart.StartIndex,
                            meshPart.IndicesCount
                        );
                    }
                }
            }
            else {
                // 手持物品时绘制方块图标
                int num5 = Terrain.ExtractContents(m_value);
                Block block = BlocksManager.Blocks[num5];
                Vector3 vector = block.GetInHandRotation(m_value) * ((float)Math.PI / 180f) + m_itemRotation;
                Matrix matrix2 = Matrix.CreateFromYawPitchRoll(vector.Y, vector.X, vector.Z)
                    * Matrix.CreateTranslation(block.GetInHandOffset(m_value))
                    * identity
                    * Matrix.CreateTranslation(m_itemOffset)
                    * controllerMatrix
                    * matrix;
                m_drawBlockEnvironmentData.DrawBlockMode = DrawBlockMode.FirstPerson;
                m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
                m_drawBlockEnvironmentData.InWorldMatrix = matrix2;
                m_drawBlockEnvironmentData.Light = m_itemLight;
                m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
                m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z)
                    + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num);
                m_drawBlockEnvironmentData.EnvironmentTemperature = m_componentPlayer.ComponentVitalStats.EnvironmentTemperature;
                m_drawBlockEnvironmentData.Owner = m_entity;
                block.DrawBlock(m_primitivesRenderer, m_value, Color.White, block.GetInHandScale(m_value), ref matrix2, m_drawBlockEnvironmentData);
            }
            m_primitivesRenderer.Flush(camera.ProjectionMatrix);
        }

        public virtual void Update(float dt) {
            int activeBlockValue = m_componentMiner.ActiveBlockValue;
            if (m_swapAnimationTime == 0f
                && activeBlockValue != m_value) {
                if (BlocksManager.Blocks[Terrain.ExtractContents(activeBlockValue)].IsSwapAnimationNeeded(m_value, activeBlockValue)) {
                    m_swapAnimationTime = 0.0001f;
                }
                else {
                    m_value = activeBlockValue;
                }
            }
            if (m_swapAnimationTime > 0f) {
                float swapAnimationTime = m_swapAnimationTime;
                m_swapAnimationTime += 2f * dt;
                if (swapAnimationTime < 0.5f
                    && m_swapAnimationTime >= 0.5f) {
                    m_value = activeBlockValue;
                }
                if (m_swapAnimationTime > 1f) {
                    m_swapAnimationTime = 0f;
                }
            }
            m_pokeAnimationTime = m_componentMiner.PokingPhase;
            m_itemOffset = Vector3.Lerp(m_itemOffset, ItemOffsetOrder, MathUtils.Saturate(10f * dt));
            m_itemRotation = Vector3.Lerp(m_itemRotation, ItemRotationOrder, MathUtils.Saturate(10f * dt));
            ItemOffsetOrder = Vector3.Zero;
            ItemRotationOrder = Vector3.Zero;
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_componentMiner = Entity.FindComponent<ComponentMiner>(true);
            m_vrHandModel = ContentManager.Get<Model>(valuesDictionary.GetValue<string>("VrHandModelName"));
        }
    }
}