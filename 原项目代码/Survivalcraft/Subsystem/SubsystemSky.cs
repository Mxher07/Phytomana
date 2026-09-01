using System.Globalization;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemSky : Subsystem, IDrawable, IUpdateable {
        public struct SkyVertex {
            public Vector3 Position;

            public Color Color;
        }

        public class SkyDome : IDisposable {
            public const int VerticesCountX = 16;

            public const int VerticesCountY = 8;

            public float? LastUpdateTimeOfDay;

            public float? LastUpdatePrecipitationIntensity;

            public int? LastUpdateTemperature;

            public float? LastUpdateFogDensity;

            public float LastUpdateLightningStrikeBrightness;

            public SkyVertex[] Vertices = new SkyVertex[128];

            public ushort[] Indices = new ushort[714];

            public VertexBuffer VertexBuffer;

            public IndexBuffer IndexBuffer;

            public virtual void Dispose() {
                Utilities.Dispose(ref VertexBuffer);
                Utilities.Dispose(ref IndexBuffer);
            }
        }

        public struct StarVertex {
            public Vector3 Position;

            public Vector2 TextureCoordinate;

            public Color Color;
        }

        public SubsystemTimeOfDay m_subsystemTimeOfDay;

        public SubsystemSeasons m_subsystemSeasons;

        public SubsystemTime m_subsystemTime;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemWeather m_subsystemWeather;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemBodies m_subsystemBodies;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

        public PrimitivesRenderer2D m_primitivesRenderer2d = new();

        public PrimitivesRenderer3D m_primitivesRenderer3d = new();

        public Random m_random = new();

        public Random m_fogSeedRandom = new();

        public Color m_viewFogColor;

        public float m_viewFogBottom;

        public float m_viewFogTop;

        public float m_viewHazeStart;

        public float m_viewHazeDensity;

        public float m_viewFogDensity;

        public bool m_viewIsSkyVisible;

        public Texture2D m_sunTexture;

        public Texture2D m_glowTexture;

        public Texture2D m_cloudsTexture;

        public Texture2D[] m_moonTextures = new Texture2D[8];

        public static UnlitShader m_shaderFlat = new(true, false, true, false);

        public static UnlitShader m_shaderTextured = new(true, true, false, false);

        public VertexDeclaration m_skyVertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position),
            new VertexElement(12, VertexElementFormat.NormalizedByte4, VertexElementSemantic.Color)
        );

        public Dictionary<GameWidget, SkyDome> m_skyDomes = [];

        public VertexBuffer m_starsVertexBuffer;

        public IndexBuffer m_starsIndexBuffer;

        public VertexDeclaration m_starsVertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate),
            new VertexElement(20, VertexElementFormat.NormalizedByte4, VertexElementSemantic.Color)
        );

        public const int m_starsCount = 250;

        public Vector3? m_lightningStrikePosition;

        public float m_lightningStrikeBrightness;

        public double m_lastLightningStrikeTime;

        public bool DrawSkyEnabled = true;

        // ReSharper disable UnassignedField.Global
        public bool DrawCloudsWireframe;
        // ReSharper restore UnassignedField.Global

        public bool FogEnabled = true;

        public int[] m_drawOrders = [-100, 5, 105];

        public float[] m_cloudsLayerRadii = [0f, 0.8f, 0.95f, 1f];

        public Color[] m_cloudsLayerColors = new Color[5];

        public static int[] m_lightValuesMoonless = [0, 3, 6, 9, 12, 15];

        public static int[] m_lightValuesNormal = [3, 5, 8, 10, 13, 15];

        public virtual float SkyLightIntensity { get; set; }

        public virtual int MoonPhase { get; set; }

        public virtual int SkyLightValue { get; set; }

        public virtual float VisibilityRange { get; set; }

        public virtual float VisibilityRangeYMultiplier { get; set; }

        public virtual float ViewUnderWaterDepth { get; set; }

        public virtual float ViewUnderMagmaDepth { get; set; }

        public virtual Color ViewFogColor => m_viewFogColor;

        public virtual float ViewFogBottom => m_viewFogBottom;

        public virtual float ViewFogTop => m_viewFogTop;

        public virtual float ViewHazeStart => m_viewHazeStart;

        public virtual float ViewHazeDensity => m_viewHazeDensity;

        public virtual float ViewFogDensity => m_viewFogDensity;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public int[] DrawOrders => m_drawOrders;

        public SkyPrimitiveRender m_primitiveRender;

        // ReSharper disable UnassignedField.Global
        public static SkyShader Shader;

        public static SkyShader ShaderAlphaTest;
        // ReSharper restore UnassignedField.Global

        public static bool DrawGalaxyEnabled = true;

        public virtual void MakeLightningStrike(Vector3 targetPosition, bool manual) {
            float explosionPressure = m_random.Float(0f, 1f) < 0.2f ? 39 : 19;
            bool strike = m_subsystemTime.GameTime - m_lastLightningStrikeTime > 1.0;
            bool setBodyOnFire = true;
            ModsManager.HookAction(
                "OnLightningStrike",
                loader => {
                    loader.OnLightningStrike(this, ref targetPosition, ref strike, ref explosionPressure, ref setBodyOnFire);
                    return false;
                }
            );
            if (m_lightningStrikePosition.HasValue
                || !strike) {
                return;
            }
            m_lastLightningStrikeTime = m_subsystemTime.GameTime;
            m_lightningStrikePosition = targetPosition;
            m_lightningStrikeBrightness = 1f;
            float num = float.MaxValue;
            foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions) {
                float num2 = Vector2.Distance(new Vector2(listenerPosition.X, listenerPosition.Z), new Vector2(targetPosition.X, targetPosition.Z));
                if (num2 < num) {
                    num = num2;
                }
            }
            float delay = m_subsystemAudio.CalculateDelay(num);
            if (num < 40f) {
                m_subsystemAudio.PlayRandomSound("Audio/ThunderNear", 1f, m_random.Float(-0.2f, 0.2f), 0f, delay);
            }
            else if (num < 200f) {
                m_subsystemAudio.PlayRandomSound("Audio/ThunderFar", 0.8f, m_random.Float(-0.2f, 0.2f), 0f, delay);
            }
            if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != 0) {
                return;
            }
            DynamicArray<ComponentBody> dynamicArray = [];
            m_subsystemBodies.FindBodiesAroundPoint(new Vector2(targetPosition.X, targetPosition.Z), 4f, dynamicArray);
            for (int i = 0; i < dynamicArray.Count; i++) {
                ComponentBody componentBody = dynamicArray.Array[i];
                if (setBodyOnFire
                    && componentBody.Position.Y > targetPosition.Y - 1.5f
                    && Vector2.Distance(
                        new Vector2(componentBody.Position.X, componentBody.Position.Z),
                        new Vector2(targetPosition.X, targetPosition.Z)
                    )
                    < 4f) {
                    componentBody.Entity.FindComponent<ComponentOnFire>()?.SetOnFire(null, m_random.Float(12f, 15f));
                }
                ComponentCreature componentCreature = componentBody.Entity.FindComponent<ComponentCreature>();
                if (componentCreature != null
                    && componentCreature.PlayerStats != null) {
                    componentCreature.PlayerStats.StruckByLightning++;
                }
            }
            bool flag = true;
            int num3 = Terrain.ToCell(targetPosition.X);
            int num4 = Terrain.ToCell(targetPosition.Y);
            int num5 = Terrain.ToCell(targetPosition.Z);
            if (!manual) {
                for (int j = -1; j <= 1; j++) {
                    for (int k = -1; k <= 1; k++) {
                        for (int l = -1; l <= 1; l++) {
                            if (BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num3 + j, num4 + k, num5 + l)] is LeavesBlock) {
                                flag = false;
                            }
                        }
                    }
                }
            }
            if (flag) {
                float pressure = m_random.Bool(0.2f) ? 39 : 19;
                Project.FindSubsystem<SubsystemExplosions>(true).AddExplosion(num3, num4 + 1, num5, pressure, false, true);
            }
            int cellValue = m_subsystemTerrain.Terrain.GetCellValue(num3, num4, num5);
            int num6 = Terrain.ExtractContents(cellValue);
            if (num6 != 0) {
                Block block = BlocksManager.Blocks[num6];
                m_subsystemParticles.AddParticleSystem(
                    block.CreateDebrisParticleSystem(m_subsystemTerrain, new Vector3(num3 + 0.5f, num4 + 1.5f, num5 + 0.5f), cellValue, 2.5f)
                );
            }
        }

        public delegate float CalculateFogDelegate(Vector3 viewPosition, Vector3 position);

        public CalculateFogDelegate CalculateFog { get; set; }

        public delegate float CalculateFogNoHazeDelegate(Vector3 viewPosition, Vector3 position);

        public CalculateFogNoHazeDelegate CalculateFogNoHaze { get; set; }

        public virtual void Update(float dt) {
            UpdateMoonPhase();
            UpdateLightAndViewParameters();
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            if (drawOrder == m_drawOrders[0]) {
                ViewUnderWaterDepth = 0f;
                ViewUnderMagmaDepth = 0f;
                Vector3 viewPosition = camera.ViewPosition;
                int x = Terrain.ToCell(viewPosition.X);
                int y = Terrain.ToCell(viewPosition.Y);
                int z = Terrain.ToCell(viewPosition.Z);
                float? surfaceHeight = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, y, z, out FluidBlock surfaceFluidBlock);
                if (surfaceHeight.HasValue) {
                    if (surfaceFluidBlock is WaterBlock) {
                        ViewUnderWaterDepth = surfaceHeight.Value + 0.1f - viewPosition.Y;
                    }
                    else if (surfaceFluidBlock is MagmaBlock) {
                        ViewUnderMagmaDepth = surfaceHeight.Value + 1f - viewPosition.Y;
                    }
                }
                if (ViewUnderWaterDepth > 0f) {
                    int seasonalHumidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
                    int temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
                    Color c = BlockColorsMap.Water.Lookup(temperature, seasonalHumidity);
                    float num = MathUtils.Lerp(1f, 0.5f, seasonalHumidity / 15f);
                    float num2 = MathUtils.Lerp(1f, 0.2f, MathUtils.Saturate(0.075f * (ViewUnderWaterDepth - 2f)));
                    float num3 = MathUtils.Lerp(0.33f, 1f, SkyLightIntensity);
                    m_viewHazeStart = 0f;
                    m_viewHazeDensity = MathUtils.Lerp(0.25f, 0.1f, num * num2 * num3);
                    m_viewFogDensity = 0f;
                    m_viewFogBottom = 0f;
                    m_viewFogTop = 1f;
                    m_viewFogColor = Color.MultiplyColorOnly(c, 0.66f * num2 * num3); //在水中的视图雾颜色
                    VisibilityRangeYMultiplier = 1f;
                    m_viewIsSkyVisible = false;
                }
                else if (ViewUnderMagmaDepth > 0f) {
                    m_viewHazeStart = 0f;
                    m_viewHazeDensity = 10f;
                    m_viewFogDensity = 0f;
                    m_viewFogBottom = 0f;
                    m_viewFogTop = 1f;
                    m_viewFogColor = new Color(255, 80, 0); //在岩浆中的视图雾颜色
                    VisibilityRangeYMultiplier = 1f;
                    m_viewIsSkyVisible = false;
                }
                else {
                    m_fogSeedRandom.Seed(m_subsystemWeather.FogSeed);
                    float num4 = m_fogSeedRandom.Bool(0.66f) ? m_fogSeedRandom.Float(62f, 82f) : m_fogSeedRandom.Float(62f, 180f);
                    float x2 = Math.Clamp(num4 + m_fogSeedRandom.Float(-20f, 20f), 62f, 180f);
                    float num5 = m_fogSeedRandom.Bool(0.66f) ? m_fogSeedRandom.Float(12f, 22f) : m_fogSeedRandom.Float(12f, 80f);
                    m_viewFogBottom = MathUtils.Lerp(num4, x2, m_subsystemWeather.FogProgress);
                    m_viewFogTop = m_viewFogBottom + num5;
                    m_viewFogDensity = MathF.Pow(m_subsystemWeather.FogIntensity, 2f) * m_fogSeedRandom.Float(0.04f, 0.1f);
                    float num6 = 256f;
                    float num7 = 128f;
                    int seasonalTemperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(
                        Terrain.ToCell(viewPosition.X),
                        Terrain.ToCell(viewPosition.Z)
                    );
                    float f = CalculateHazeFactor();
                    float num8 = MathUtils.Lerp(0.5f, 0f, f);
                    float num9 = MathUtils.Lerp(1f, 0.8f, f);
                    m_viewHazeStart = VisibilityRange * num8;
                    m_viewHazeDensity = 1f / ((num9 - num8) * VisibilityRange);
                    Color color = CalculateSkyColor(new Vector3(1f, 0f, 0f), seasonalTemperature);
                    Color color2 = CalculateSkyColor(new Vector3(0f, 0f, 1f), seasonalTemperature);
                    Color color3 = CalculateSkyColor(new Vector3(-1f, 0f, 0f), seasonalTemperature);
                    Color color4 = CalculateSkyColor(new Vector3(0f, 0f, -1f), seasonalTemperature);
                    Color c2 = 0.25f * color + 0.25f * color2 + 0.25f * color3 + 0.25f * color4;
                    Color c3 = CalculateSkyColor(new Vector3(camera.ViewDirection.X, 0f, camera.ViewDirection.Z), seasonalTemperature);
                    //在正常情况下（空气中）视图雾
                    m_viewFogColor = Color.Lerp(c3, c2, CalculateSkyFog(camera.ViewPosition));
                    VisibilityRangeYMultiplier = MathUtils.Lerp(
                        VisibilityRange / num6,
                        VisibilityRange / num7,
                        MathF.Pow(m_subsystemWeather.PrecipitationIntensity, 4f)
                    );
                    m_viewIsSkyVisible = true;
                }
                if (!FogEnabled) {
                    m_viewHazeDensity = 0f;
                    m_viewFogDensity = 0f;
                }
                if (!DrawSkyEnabled
                    || !m_viewIsSkyVisible
                    || SettingsManager.SkyRenderingMode == SkyRenderingMode.Disabled) {
                    FlatBatch2D flatBatch2D = m_primitivesRenderer2d.FlatBatch(
                        -1,
                        DepthStencilState.None,
                        RasterizerState.CullNoneScissor,
                        BlendState.Opaque
                    );
                    int count = flatBatch2D.TriangleVertices.Count;
                    ModsManager.HookAction(
                        "ViewFogColor",
                        modLoader => {
                            modLoader.ViewFogColor(ViewUnderWaterDepth, ViewUnderMagmaDepth, ref m_viewFogColor);
                            return false;
                        }
                    );
                    flatBatch2D.QueueQuad(Vector2.Zero, camera.ViewportSize, 0f, m_viewFogColor);
                    flatBatch2D.TransformTriangles(camera.ViewportMatrix, count);
                    m_primitivesRenderer2d.Flush();
                }
            }
            else if (drawOrder == m_drawOrders[1]) {
                if (DrawSkyEnabled
                    && m_viewIsSkyVisible
                    && SettingsManager.SkyRenderingMode != SkyRenderingMode.Disabled) {
                    DrawSkydome(camera);
                    if (DrawGalaxyEnabled) {
                        DrawStars(camera);
                        DrawSunAndMoon(camera);
                    }
                    DrawClouds(camera);
                    ModsManager.HookAction(
                        "SkyDrawExtra",
                        loader => {
                            loader.SkyDrawExtra(this, camera);
                            return false;
                        }
                    );
                    if (Shader != null
                        && ShaderAlphaTest != null) {
                        if (m_primitiveRender.Shader == null
                            && m_primitiveRender.ShaderAlphaTest == null) {
                            m_primitiveRender.Shader = Shader;
                            m_primitiveRender.ShaderAlphaTest = ShaderAlphaTest;
                            m_primitiveRender.Camera = camera;
                        }
                        m_primitiveRender.Flush(m_primitivesRenderer3d, camera.ViewProjectionMatrix);
                    }
                    else {
                        m_primitivesRenderer3d.Flush(camera.ViewProjectionMatrix);
                    }
                }
            }
            else {
                DrawLightning(camera);
                m_primitivesRenderer3d.Flush(camera.ViewProjectionMatrix);
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(true);
            m_subsystemSeasons = Project.FindSubsystem<SubsystemSeasons>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFluidBlockBehavior>(true);
            m_sunTexture = ContentManager.Get<Texture2D>("Textures/Sun");
            m_glowTexture = ContentManager.Get<Texture2D>("Textures/SkyGlow");
            m_cloudsTexture = ContentManager.Get<Texture2D>("Textures/Clouds");
            m_primitiveRender = new SkyPrimitiveRender();
            for (int i = 0; i < 8; i++) {
                m_moonTextures[i] = ContentManager.Get<Texture2D>($"Textures/Moon{(i + 1).ToString(CultureInfo.InvariantCulture)}");
            }
            InitializeCalculation();
            UpdateMoonPhase();
            UpdateLightAndViewParameters();
            Display.DeviceReset += Display_DeviceReset;
        }

        public virtual void InitializeCalculation() {
            CalculateFog = CalculateFogSurvivalcraft;
            CalculateFogNoHaze = CalculateFogNoHazeSurvivalcraft;
            CalculateLightIntensity = CalculateLightIntensitySurvivalcraft;
            CalculateSeasonAngle = CalculateSeasonAngleSurvivalcraft;
            CalculateHazeFactor = CalculateHazeFactorSurvivalcraft;
            CalculateSkyColor = CalculateSkyColorSurvivalcraft;
            CalculateSkyFog = CalculateSkyFogSurvivalcraft;
            CalculateDawnGlowIntensity = CalculateDawnGlowIntensitySurvivalcraft;
            CalculateDuskGlowIntensity = CalculateDuskGlowIntensitySurvivalcraft;
            CalculateWinterDistance = CalculateWinterDistanceSurvivalcraft;
        }

        public override void Dispose() {
            Display.DeviceReset -= Display_DeviceReset;
            Utilities.Dispose(ref m_starsVertexBuffer);
            Utilities.Dispose(ref m_starsIndexBuffer);
            foreach (SkyDome value in m_skyDomes.Values) {
                value.Dispose();
            }
            m_skyDomes.Clear();
        }

        public virtual void Display_DeviceReset() {
            Utilities.Dispose(ref m_starsVertexBuffer);
            Utilities.Dispose(ref m_starsIndexBuffer);
            foreach (SkyDome value in m_skyDomes.Values) {
                value.Dispose();
            }
            m_skyDomes.Clear();
        }

        public virtual void DrawSkydome(Camera camera) {
            if (!m_skyDomes.TryGetValue(camera.GameWidget, out SkyDome value)) {
                value = new SkyDome();
                m_skyDomes.Add(camera.GameWidget, value);
            }
            if (value.VertexBuffer == null
                || value.IndexBuffer == null) {
                Utilities.Dispose(ref value.VertexBuffer);
                Utilities.Dispose(ref value.IndexBuffer);
                value.VertexBuffer = new VertexBuffer(m_skyVertexDeclaration, value.Vertices.Length);
                value.IndexBuffer = new IndexBuffer(IndexFormat.SixteenBits, value.Indices.Length);
                FillSkyIndexBuffer(value);
                value.LastUpdateTimeOfDay = null;
            }
            int x = Terrain.ToCell(camera.ViewPosition.X);
            int z = Terrain.ToCell(camera.ViewPosition.Z);
            float precipitationIntensity = m_subsystemWeather.PrecipitationIntensity;
            float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
            int seasonalTemperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z);
            bool flag = true;
            if (value.LastUpdateTimeOfDay.HasValue
                && !(MathF.Abs(timeOfDay - value.LastUpdateTimeOfDay.Value) > 0.0005f)
                && value.LastUpdatePrecipitationIntensity.HasValue
                && !(MathF.Abs(precipitationIntensity - value.LastUpdatePrecipitationIntensity.Value) > 0.02f)
                && ((precipitationIntensity != 0f && precipitationIntensity != 1f)
                    || value.LastUpdatePrecipitationIntensity.Value == precipitationIntensity)
                && m_lightningStrikeBrightness == value.LastUpdateLightningStrikeBrightness
                && value.LastUpdateTemperature.HasValue) {
                int? lastUpdateTemperature = value.LastUpdateTemperature;
                if (seasonalTemperature == lastUpdateTemperature.GetValueOrDefault()
                    && lastUpdateTemperature.HasValue
                    && value.LastUpdateTemperature.HasValue
                    && !(MathF.Abs(m_viewFogDensity - (value.LastUpdateFogDensity ?? 0f)) > 0.002f)) {
                    flag = false;
                }
            }
            if (flag) {
                value.LastUpdateTimeOfDay = timeOfDay;
                value.LastUpdatePrecipitationIntensity = precipitationIntensity;
                value.LastUpdateLightningStrikeBrightness = m_lightningStrikeBrightness;
                value.LastUpdateTemperature = seasonalTemperature;
                value.LastUpdateFogDensity = m_viewFogDensity;
                FillSkyVertexBuffer(value, timeOfDay, precipitationIntensity, seasonalTemperature);
            }
            Display.DepthStencilState = DepthStencilState.DepthRead;
            Display.RasterizerState = RasterizerState.CullNoneScissor;
            float num = CalculateSkyFog(camera.ViewPosition);
            Display.BlendState = BlendState.Opaque;
            m_shaderFlat.Transforms.World[0] = Matrix.CreateTranslation(camera.ViewPosition) * camera.ViewProjectionMatrix;
            m_shaderFlat.Color = new Vector4(1f - num);
            m_shaderFlat.AdditiveColor = num * new Vector4(ViewFogColor);
            Display.DrawIndexed(PrimitiveType.TriangleList, m_shaderFlat, value.VertexBuffer, value.IndexBuffer, 0, value.IndexBuffer.IndicesCount);
        }

        public virtual void DrawStars(Camera camera) {
            float precipitationIntensity = m_subsystemWeather.PrecipitationIntensity;
            float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
            if (m_starsVertexBuffer == null
                || m_starsIndexBuffer == null) {
                Utilities.Dispose(ref m_starsVertexBuffer);
                Utilities.Dispose(ref m_starsIndexBuffer);
                m_starsVertexBuffer = new VertexBuffer(m_starsVertexDeclaration, m_starsCount * 4);
                m_starsIndexBuffer = new IndexBuffer(IndexFormat.SixteenBits, m_starsCount * 6);
                FillStarsBuffers();
            }
            Display.DepthStencilState = DepthStencilState.DepthRead;
            Display.RasterizerState = RasterizerState.CullNoneScissor;
            float num = MathUtils.Sqr((1f - CalculateLightIntensity(timeOfDay)) * (1f - precipitationIntensity));
            num *= 1f - CalculateSkyFog(camera.ViewPosition);
            if (num > 0.01f) {
                Display.BlendState = BlendState.Additive;
                m_shaderTextured.Transforms.World[0] = Matrix.CreateRotationZ(-2f * timeOfDay * (float)Math.PI)
                    * Matrix.CreateRotationX(CalculateSeasonAngle())
                    * Matrix.CreateTranslation(camera.ViewPosition)
                    * camera.ViewProjectionMatrix;
                m_shaderTextured.Color = new Vector4(1f, 1f, 1f, num);
                m_shaderTextured.Texture = ContentManager.Get<Texture2D>("Textures/Star");
                m_shaderTextured.SamplerState = SamplerState.LinearClamp;
                Display.DrawIndexed(
                    PrimitiveType.TriangleList,
                    m_shaderTextured,
                    m_starsVertexBuffer,
                    m_starsIndexBuffer,
                    0,
                    m_starsIndexBuffer.IndicesCount
                );
            }
        }

        public virtual void DrawSunAndMoon(Camera camera) {
            float precipitationIntensity = m_subsystemWeather.PrecipitationIntensity;
            float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
            float f = MathUtils.Max(CalculateDawnGlowIntensity(timeOfDay), CalculateDuskGlowIntensity(timeOfDay));
            float num = (float)Math.PI * 2f * (timeOfDay - m_subsystemTimeOfDay.Midday);
            float angle = num + (float)Math.PI;
            float num2 = MathUtils.Lerp(90f, 160f, f);
            float num3 = MathUtils.Lerp(60f, 80f, f);
            Color color = Color.Lerp(new Color(255, 255, 255), new Color(255, 255, 160), f);
            Color white = Color.White;
            white *= 1f - SkyLightIntensity;
            color *= MathUtils.Lerp(1f, 0f, precipitationIntensity);
            white *= MathUtils.Lerp(1f, 0f, precipitationIntensity);
            Color color2 = color * 0.6f * MathUtils.Lerp(1f, 0f, precipitationIntensity);
            Color color3 = color * 0.2f * MathUtils.Lerp(1f, 0f, precipitationIntensity);
            TexturedBatch3D batch = m_primitivesRenderer3d.TexturedBatch(
                m_glowTexture,
                false,
                0,
                DepthStencilState.DepthRead,
                null,
                BlendState.Additive
            );
            TexturedBatch3D batch2 = m_primitivesRenderer3d.TexturedBatch(
                m_sunTexture,
                false,
                1,
                DepthStencilState.DepthRead,
                null,
                BlendState.AlphaBlend
            );
            TexturedBatch3D batch3 = m_primitivesRenderer3d.TexturedBatch(
                m_moonTextures[MoonPhase],
                false,
                1,
                DepthStencilState.DepthRead,
                null,
                BlendState.AlphaBlend
            );
            QueueCelestialBody(batch, camera.ViewPosition, color2, 900f, 3.5f * num2, num);
            QueueCelestialBody(batch, camera.ViewPosition, color3, 900f, 3.5f * num3, angle);
            QueueCelestialBody(batch2, camera.ViewPosition, color, 900f, num2, num);
            QueueCelestialBody(batch3, camera.ViewPosition, white, 900f, num3, angle);
        }

        public virtual void DrawLightning(Camera camera) {
            if (!m_lightningStrikePosition.HasValue) {
                return;
            }
            FlatBatch3D flatBatch3D = m_primitivesRenderer3d.FlatBatch(0, DepthStencilState.DepthRead, null, BlendState.Additive);
            Color color0 = (1f - CalculateSkyFog(camera.ViewPosition)) * Color.White;
            Vector3 value = m_lightningStrikePosition.Value;
            Vector3 unitY = Vector3.UnitY;
            Vector3 v = Vector3.Normalize(Vector3.Cross(camera.ViewDirection, unitY));
            Viewport viewport = Display.Viewport;
            float num = Vector4.Transform(new Vector4(value, 1f), camera.ViewProjectionMatrix).W
                * 2f
                / (viewport.Width * camera.ProjectionMatrix.M11);
            for (int i = 0; i < (int)(m_lightningStrikeBrightness * 30f); i++) {
                float s = m_random.NormalFloat(0f, 1f * num);
                float s2 = m_random.NormalFloat(0f, 1f * num);
                Vector3 v2 = s * v + s2 * unitY;
                float num2 = 260f;
                while (num2 > value.Y) {
                    uint num3 = MathUtils.Hash((uint)(m_lightningStrikePosition.Value.X + 100f * m_lightningStrikePosition.Value.Z + 200f * num2));
                    float num4 = MathUtils.Lerp(4f, 10f, (float)(double)(num3 & 0xFF) / 255f);
                    float s3 = (num3 & 1) == 0 ? 1 : -1;
                    float s4 = MathUtils.Lerp(0.05f, 0.2f, (float)(double)((num3 >> 8) & 0xFF) / 255f);
                    float num5 = num2;
                    float num6 = num5 - num4 * MathUtils.Lerp(0.45f, 0.55f, ((num3 >> 16) & 0xFF) / 255f);
                    float num7 = num5 - num4 * MathUtils.Lerp(0.45f, 0.55f, ((num3 >> 24) & 0xFF) / 255f);
                    float num8 = num5 - num4;
                    Vector3 p = new Vector3(value.X, num5, value.Z) + v2;
                    Vector3 vector = new Vector3(value.X, num6, value.Z) + v2 - num4 * v * s3 * s4;
                    Vector3 vector2 = new Vector3(value.X, num7, value.Z) + v2 + num4 * v * s3 * s4;
                    Vector3 p2 = new Vector3(value.X, num8, value.Z) + v2;
                    Color color = color0 * 0.2f * MathUtils.Saturate((260f - num5) * 0.2f);
                    Color color2 = color0 * 0.2f * MathUtils.Saturate((260f - num6) * 0.2f);
                    Color color3 = color0 * 0.2f * MathUtils.Saturate((260f - num7) * 0.2f);
                    Color color4 = color0 * 0.2f * MathUtils.Saturate((260f - num8) * 0.2f);
                    flatBatch3D.QueueLine(p, vector, color, color2);
                    flatBatch3D.QueueLine(vector, vector2, color2, color3);
                    flatBatch3D.QueueLine(vector2, p2, color3, color4);
                    num2 -= num4;
                }
            }
            float num9 = MathUtils.Lerp(
                0.3f,
                0.75f,
                0.5f * (float)Math.Sin(MathUtils.Remainder(1.0 * m_subsystemTime.GameTime, 6.2831854820251465)) + 0.5f
            );
            m_lightningStrikeBrightness -= m_subsystemTime.GameTimeDelta / num9;
            if (m_lightningStrikeBrightness <= 0f) {
                m_lightningStrikePosition = null;
                m_lightningStrikeBrightness = 0f;
            }
        }

        public virtual void DrawClouds(Camera camera) {
            if (SettingsManager.SkyRenderingMode == SkyRenderingMode.NoClouds) {
                return;
            }
            float f = CalculateHazeFactor();
            float num = MathUtils.Lerp(0.03f, 1f, MathUtils.Sqr(SkyLightIntensity)) * MathUtils.Lerp(1f, 0.2f, f);
            float f2 = CalculateSkyFog(camera.ViewPosition);
            m_cloudsLayerColors[0] = Color.Lerp(Color.White * (num * 0.75f), ViewFogColor, f2);
            m_cloudsLayerColors[1] = Color.Lerp(Color.White * (num * 0.66f), ViewFogColor, f2);
            m_cloudsLayerColors[2] = ViewFogColor;
            m_cloudsLayerColors[3] = Color.Transparent;
            double gameTime = m_subsystemTime.GameTime;
            Vector3 viewPosition = camera.ViewPosition;
            Vector2 v = new(
                (float)MathUtils.Remainder(0.002 * gameTime - viewPosition.X / 1900f * 1.75f, 1.0) + viewPosition.X / 1900f * 1.75f,
                (float)MathUtils.Remainder(0.002 * gameTime - viewPosition.Z / 1900f * 1.75f, 1.0) + viewPosition.Z / 1900f * 1.75f
            );
            TexturedBatch3D texturedBatch3D = m_primitivesRenderer3d.TexturedBatch(
                m_cloudsTexture,
                false,
                2,
                DepthStencilState.DepthRead,
                null,
                BlendState.AlphaBlend,
                SamplerState.LinearWrap
            );
            DynamicArray<VertexPositionColorTexture> triangleVertices = texturedBatch3D.TriangleVertices;
            DynamicArray<int> triangleIndices = texturedBatch3D.TriangleIndices;
            int count = triangleVertices.Count;
            int count2 = triangleVertices.Count;
            int count3 = triangleIndices.Count;
            triangleVertices.Count += 49;
            triangleIndices.Count += 216;
            for (int i = 0; i < 7; i++) {
                for (int j = 0; j < 7; j++) {
                    int num2 = j - 3;
                    int num3 = i - 3;
                    int num4 = MathUtils.Max(Math.Abs(num2), Math.Abs(num3));
                    float num5 = m_cloudsLayerRadii[num4];
                    float num6 = num4 > 0 ? num5 / MathF.Sqrt(num2 * num2 + num3 * num3) : 0f;
                    float num7 = num2 * num6;
                    float num8 = num3 * num6;
                    float y = MathUtils.Lerp(600f, 60f, num5 * num5);
                    Vector3 position = new(viewPosition.X + num7 * 1900f, y, viewPosition.Z + num8 * 1900f);
                    Vector2 texCoord = new Vector2(position.X, position.Z) / 1900f * 1.75f - v;
                    Color color = m_cloudsLayerColors[num4];
                    texturedBatch3D.TriangleVertices.Array[count2++] = new VertexPositionColorTexture(position, color, texCoord);
                    if (j > 0
                        && i > 0) {
                        int num9 = count + j + i * 7;
                        int num10 = count + (j - 1) + i * 7;
                        int num11 = count + (j - 1) + (i - 1) * 7;
                        int num12 = count + j + (i - 1) * 7;
                        if ((num2 <= 0 && num3 <= 0)
                            || (num2 > 0 && num3 > 0)) {
                            texturedBatch3D.TriangleIndices.Array[count3++] = num9;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num10;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num11;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num11;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num12;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num9;
                        }
                        else {
                            texturedBatch3D.TriangleIndices.Array[count3++] = num9;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num10;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num12;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num10;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num11;
                            texturedBatch3D.TriangleIndices.Array[count3++] = num12;
                        }
                    }
                }
            }
            _ = DrawCloudsWireframe;
        }

        public virtual void QueueCelestialBody(TexturedBatch3D batch, Vector3 viewPosition, Color color, float distance, float radius, float angle) {
            color *= 1f - CalculateSkyFog(viewPosition);
            if (color.A > 0) {
                Matrix m = Matrix.Identity;
                m *= Matrix.CreateTranslation(0f, distance, 0f);
                m *= Matrix.CreateRotationZ(0f - angle);
                m *= Matrix.CreateRotationX(CalculateSeasonAngle());
                m *= Matrix.CreateTranslation(viewPosition);
                Vector3 v = new(0f - radius, 0f, 0f - radius);
                Vector3 v2 = new(radius, 0f, 0f - radius);
                Vector3 v3 = new(radius, 0f, radius);
                Vector3 v4 = new(0f - radius, 0f, radius);
                Vector3.Transform(ref v, ref m, out v);
                Vector3.Transform(ref v2, ref m, out v2);
                Vector3.Transform(ref v3, ref m, out v3);
                Vector3.Transform(ref v4, ref m, out v4);
                batch.QueueQuad(
                    v,
                    v2,
                    v3,
                    v4,
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0f),
                    color
                );
            }
        }

        public virtual void UpdateLightAndViewParameters() {
            VisibilityRange = SettingsManager.VisibilityRange;
            SkyLightIntensity = CalculateLightIntensity(m_subsystemTimeOfDay.TimeOfDay);
            SkyLightValue = MoonPhase == 4
                ? m_lightValuesMoonless[(int)MathF.Round(MathUtils.Lerp(0f, 5f, SkyLightIntensity))]
                : m_lightValuesNormal[(int)MathF.Round(MathUtils.Lerp(0f, 5f, SkyLightIntensity))];
        }

        public virtual void UpdateMoonPhase() {
            MoonPhase = ((int)Math.Floor(m_subsystemTimeOfDay.Day - 0.5 + 5.0) % 8 + 8) % 8;
        }

        public delegate float CalculateLightIntensityDelegate(float timeOfDay);

        public CalculateLightIntensityDelegate CalculateLightIntensity { get; set; }

        public delegate float CalculateSeasonAngleDelegate();

        public CalculateSeasonAngleDelegate CalculateSeasonAngle { get; set; }

        public delegate float CalculateHazeFactorDelegate();

        public CalculateHazeFactorDelegate CalculateHazeFactor { get; set; }

        public delegate Color CalculateSkyColorDelegate(Vector3 direction, int temperature);

        public CalculateSkyColorDelegate CalculateSkyColor { get; set; }

        public delegate float CalculateSkyFogDelegate(Vector3 viewPosition);

        public CalculateSkyFogDelegate CalculateSkyFog { get; set; }

        public virtual float FogIntegral(float y) => MathUtils.SmoothStep(ViewFogBottom, ViewFogTop, y) * (ViewFogTop - ViewFogBottom) + ViewFogBottom;

        public virtual void FillSkyVertexBuffer(SkyDome skyDome, float timeOfDay, float precipitationIntensity, int temperature) {
            for (int i = 0; i < 8; i++) {
                float x = (float)Math.PI / 2f * MathUtils.Sqr(i / 7f);
                for (int j = 0; j < 16; j++) {
                    int num = j + i * 16;
                    float x2 = (float)Math.PI * 2f * j / 16f;
                    float num2 = 1800f * MathF.Cos(x);
                    skyDome.Vertices[num].Position.X = num2 * MathF.Sin(x2);
                    skyDome.Vertices[num].Position.Z = num2 * MathF.Cos(x2);
                    skyDome.Vertices[num].Position.Y = 1800f * MathF.Sin(x) - (i == 0 ? 450f : 0f);
                    skyDome.Vertices[num].Color = CalculateSkyColor(skyDome.Vertices[num].Position, temperature);
                }
            }
            skyDome.VertexBuffer.SetData(skyDome.Vertices, 0, skyDome.Vertices.Length);
        }

        public virtual void FillSkyIndexBuffer(SkyDome skyDome) {
            int num = 0;
            for (int i = 0; i < 7; i++) {
                for (int j = 0; j < 16; j++) {
                    int num2 = j;
                    int num3 = (j + 1) % 16;
                    int num4 = i;
                    int num5 = i + 1;
                    skyDome.Indices[num++] = (ushort)(num2 + num4 * 16);
                    skyDome.Indices[num++] = (ushort)(num3 + num4 * 16);
                    skyDome.Indices[num++] = (ushort)(num3 + num5 * 16);
                    skyDome.Indices[num++] = (ushort)(num3 + num5 * 16);
                    skyDome.Indices[num++] = (ushort)(num2 + num5 * 16);
                    skyDome.Indices[num++] = (ushort)(num2 + num4 * 16);
                }
            }
            for (int k = 2; k < 16; k++) {
                skyDome.Indices[num++] = 0;
                skyDome.Indices[num++] = (ushort)(k - 1);
                skyDome.Indices[num++] = (ushort)k;
            }
            skyDome.IndexBuffer.SetData(skyDome.Indices, 0, skyDome.Indices.Length);
        }

        public virtual void FillStarsBuffers() {
            Random random = new(10);
            StarVertex[] array = new StarVertex[m_starsCount * 4];
            for (int i = 0; i < m_starsCount; i++) {
                float x;
                Color c;
                Vector3 v;
                switch (i) {
                    case 0:
                        x = 1.05f;
                        c = new Color(1f, 1f, 1f);
                        v = new Vector3(0f, 0f, 1f);
                        break;
                    case 1:
                        x = 0.91f;
                        c = new Color(1f, 0.8f, 0.6f);
                        v = new Vector3(-0.007f, -0.05f, 1f);
                        break;
                    case 2:
                        x = 0.94f;
                        c = new Color(1f, 0.8f, 0.7f);
                        v = new Vector3(0f, -0.11f, 1f);
                        break;
                    default:
                        x = random.Float(0.7f, 1f);
                        c = new Color(random.Float(0.8f, 1f), 0.8f, random.Float(0.8f, 1f));
                        do {
                            v = new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f));
                        }
                        while (v.LengthSquared() > 1f);
                        break;
                }
                float num = 7.65f * MathF.Pow(x, 3f);
                float s = MathF.Pow(x, 4f);
                c = Color.MultiplyAlphaOnly(c, s);
                v = Vector3.Normalize(v);
                Vector3 v2 = 900f * v;
                Vector3 vector = Vector3.Normalize(Vector3.Cross(v.X > v.Y ? Vector3.UnitY : Vector3.UnitX, v));
                Vector3 v3 = Vector3.Normalize(Vector3.Cross(vector, v));
                Vector3 position = v2 + num * (-vector - v3);
                Vector3 position2 = v2 + num * (vector - v3);
                Vector3 position3 = v2 + num * (vector + v3);
                Vector3 position4 = v2 + num * (-vector + v3);
                array[i * 4] = new StarVertex { Position = position, TextureCoordinate = new Vector2(0f, 0f), Color = c };
                array[i * 4 + 1] = new StarVertex { Position = position2, TextureCoordinate = new Vector2(1f, 0f), Color = c };
                array[i * 4 + 2] = new StarVertex { Position = position3, TextureCoordinate = new Vector2(1f, 1f), Color = c };
                array[i * 4 + 3] = new StarVertex { Position = position4, TextureCoordinate = new Vector2(0f, 1f), Color = c };
            }
            m_starsVertexBuffer.SetData(array, 0, array.Length);
            ushort[] array2 = new ushort[m_starsCount * 6];
            for (int j = 0; j < m_starsCount; j++) {
                array2[j * 6] = (ushort)(j * 4);
                array2[j * 6 + 1] = (ushort)(j * 4 + 1);
                array2[j * 6 + 2] = (ushort)(j * 4 + 2);
                array2[j * 6 + 3] = (ushort)(j * 4 + 2);
                array2[j * 6 + 4] = (ushort)(j * 4 + 3);
                array2[j * 6 + 5] = (ushort)(j * 4);
            }
            m_starsIndexBuffer.SetData(array2, 0, array2.Length);
        }

        public delegate float CalculateDawnGlowIntensityDelegate(float timeOfDay);

        public CalculateDawnGlowIntensityDelegate CalculateDawnGlowIntensity { get; set; }

        public delegate float CalculateDuskGlowIntensityDelegate(float timeOfDay);

        public CalculateDuskGlowIntensityDelegate CalculateDuskGlowIntensity;

        public delegate float CalculateWinterDistanceDelegate();

        public CalculateWinterDistanceDelegate CalculateWinterDistance;

        #region CalculationSurvivalcraft

        public virtual float CalculateFogSurvivalcraft(Vector3 viewPosition, Vector3 position) {
            Vector3 vector = viewPosition - position;
            vector.Y *= VisibilityRangeYMultiplier;
            float num = vector.Length();
            float num2 = (FogIntegral(viewPosition.Y) - FogIntegral(position.Y)) / (viewPosition.Y - position.Y);
            float num3 = MathUtils.Saturate(ViewHazeDensity * (num - ViewHazeStart));
            float num4 = num2 * ViewFogDensity * num;
            return MathUtils.Saturate(num3 + num4);
        }

        public virtual float CalculateFogNoHazeSurvivalcraft(Vector3 viewPosition, Vector3 position) {
            Vector3 vector = viewPosition - position;
            vector.Y *= VisibilityRangeYMultiplier;
            float num = vector.Length();
            return MathUtils.Saturate((FogIntegral(viewPosition.Y) - FogIntegral(position.Y)) / (viewPosition.Y - position.Y) * ViewFogDensity * num);
        }

        public virtual float CalculateLightIntensitySurvivalcraft(float timeOfDay) {
            if (IntervalUtils.IsBetween(timeOfDay, m_subsystemTimeOfDay.NightStart, m_subsystemTimeOfDay.DawnStart)) {
                return 0f;
            }
            if (IntervalUtils.IsBetween(timeOfDay, m_subsystemTimeOfDay.DawnStart, m_subsystemTimeOfDay.DayStart)) {
                return IntervalUtils.Interval(m_subsystemTimeOfDay.DawnStart, timeOfDay) / m_subsystemTimeOfDay.DawnInterval;
            }
            if (IntervalUtils.IsBetween(timeOfDay, m_subsystemTimeOfDay.DayStart, m_subsystemTimeOfDay.DuskStart)) {
                return 1f;
            }
            return 1f - IntervalUtils.Interval(m_subsystemTimeOfDay.DuskStart, timeOfDay) / m_subsystemTimeOfDay.DuskInterval;
        }

        public virtual float CalculateSeasonAngleSurvivalcraft() => -0.4f
            - 0.7f * (0.5f - 0.5f * MathF.Cos((m_subsystemGameInfo.WorldSettings.TimeOfYear - SubsystemSeasons.MidSummer) * 2f * MathF.PI));

        public virtual float CalculateHazeFactorSurvivalcraft() =>
            MathUtils.Saturate(m_subsystemWeather.PrecipitationIntensity + 30f * m_viewFogDensity);

        public virtual Color CalculateSkyColorSurvivalcraft(Vector3 direction, int temperature) {
            float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
            float f = CalculateHazeFactor();
            direction = Vector3.Normalize(direction);
            Vector2 vector = Vector2.Normalize(new Vector2(direction.X, direction.Z));
            float num = CalculateLightIntensity(timeOfDay);
            float f2 = MathUtils.Saturate(temperature / 15f);
            Vector3 v = new(0.65f, 0.68f, 0.7f);
            Vector3 v2 = Vector3.Lerp(new Vector3(0.33f, 0.39f, 0.46f), new Vector3(0.15f, 0.3f, 0.56f), f2);
            Vector3 v3 = Vector3.Lerp(new Vector3(0.79f, 0.83f, 0.88f), new Vector3(0.64f, 0.77f, 0.91f), f2);
            Vector3 v4 = Vector3.Lerp(v2, v, f) * num;
            Vector3 vector2 = Vector3.Lerp(v3, v, f) * num;
            Vector3 vector3 = new(1f, 0.3f, -0.2f);
            Vector3 vector4 = new(1f, 0.3f, -0.2f);
            if (m_lightningStrikePosition.HasValue) {
                v4 = Vector3.Max(new Vector3(m_lightningStrikeBrightness), v4);
            }
            float num2 = MathUtils.Lerp(CalculateDawnGlowIntensity(timeOfDay), 0f, f);
            float num3 = MathUtils.Lerp(CalculateDuskGlowIntensity(timeOfDay), 0f, f);
            float f3 = MathUtils.Saturate((direction.Y - 0.1f) / 0.4f);
            float num4 = num2 * MathUtils.Sqr(MathUtils.Saturate(0f - vector.X));
            float num5 = num3 * MathUtils.Sqr(MathUtils.Saturate(vector.X));
            Color color = new(Vector3.Lerp(vector2 + vector3 * num4 + vector4 * num5, v4, f3));
            ModsManager.HookAction(
                "ChangeSkyColor",
                loader => {
                    color = loader.ChangeSkyColor(color, direction, timeOfDay, temperature);
                    return true;
                }
            );
            return color;
        }

        public virtual float CalculateSkyFogSurvivalcraft(Vector3 viewPosition) =>
            CalculateFogNoHaze(viewPosition, viewPosition + new Vector3(1000f, 150f, 0f));

        public virtual float CalculateDawnGlowIntensitySurvivalcraft(float timeOfDay) {
            float num = MathUtils.Lerp(0.1f, 0.75f, MathUtils.LinearStep(-0.05f, 0.15f, CalculateWinterDistance()));
            float middawn = m_subsystemTimeOfDay.Middawn;
            float num2 = 1f * m_subsystemTimeOfDay.DawnInterval;
            return num * MathUtils.Max(1f - IntervalUtils.Distance(timeOfDay, middawn) / num2 * 2f, 0f);
        }

        public virtual float CalculateDuskGlowIntensitySurvivalcraft(float timeOfDay) {
            float num = MathUtils.Lerp(0.2f, 1f, MathUtils.LinearStep(-0.05f, 0.15f, CalculateWinterDistance()));
            float middusk = m_subsystemTimeOfDay.Middusk;
            float num2 = 1f * m_subsystemTimeOfDay.DuskInterval;
            return num * MathUtils.Max(1f - IntervalUtils.Distance(timeOfDay, middusk) / num2 * 2f, 0f);
        }

        public virtual float CalculateWinterDistanceSurvivalcraft() {
            float t = IntervalUtils.Midpoint(SubsystemSeasons.WinterStart, SubsystemSeasons.SpringStart);
            float num = IntervalUtils.Interval(SubsystemSeasons.WinterStart, SubsystemSeasons.SpringStart);
            return IntervalUtils.Distance(m_subsystemGameInfo.WorldSettings.TimeOfYear, t) - 0.5f * num;
        }

        #endregion
    }
}