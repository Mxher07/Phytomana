using Engine;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemModelsRenderer : Subsystem, IDrawable {
        public class ModelData : IComparable<ModelData> {
            public ComponentModel ComponentModel;

            public ComponentBody ComponentBody;

            public float Light;

            public double NextLightTime;

            public int LastAnimateFrame;

            public int CompareTo(ModelData other) {
                int num = ComponentModel?.PrepareOrder ?? 0;
                int num2 = other.ComponentModel?.PrepareOrder ?? 0;
                return num - num2;
            }
        }

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemSky m_subsystemSky;

        public SubsystemShadows m_subsystemShadows;

        public SubsystemTimeOfDay m_subsystemTimeOfDay;

        public PrimitivesRenderer3D m_primitivesRenderer = new();

        public static ModelShader ShaderOpaque;

        public static ModelShader ShaderAlphaTested;

        // Skinned shaders for skeletal animation
        public static ModelShader ShaderSkinnedOpaque;

        public static ModelShader ShaderSkinnedAlphaTested;

        public ModelShader m_shaderOpaque;

        public ModelShader m_shaderAlphaTested;

        public ModelShader m_shaderSkinnedOpaque;

        public ModelShader m_shaderSkinnedAlphaTested;

        /// <summary>
        /// 自定义渲染器实例（由模组设置）
        /// </summary>
        public ICustomModelRenderer CustomRenderer;

        /// <summary>
        /// 是否使用自定义渲染（由 Mod 设置）
        /// </summary>
        public bool UseCustomRendering;

        /// <summary>
        /// Maximum number of joints per model for GPU skinning (Uniform-based).
        /// Determined by GL_MAX_VERTEX_UNIFORM_VECTORS at runtime.
        /// </summary>
        public static int MaxJointsCount { get; private set; } = 64;

        public int MaxInstancesCount = 32;

        public Dictionary<ComponentModel, ModelData> m_componentModels = [];

        public List<ModelData> m_modelsToPrepare = [];

        public List<ModelData>[] m_modelsToDraw = [[], [], [], []];

        // Pre-allocated buffers for skinning (avoid GC pressure)
        public Matrix[] m_jointMatricesBuffer;
        public readonly List<ModelData> m_nonSkinnedModelsBuffer = [];
        public readonly List<ModelData> m_skinnedModelsBuffer = [];

        public static bool DisableDrawingModels = false;

        public int ModelsDrawn;

        public int[] m_drawOrders = [-10000, 1, 99, 201];

        public PrimitivesRenderer3D PrimitivesRenderer => m_primitivesRenderer;

        public int[] DrawOrders => m_drawOrders;

        public virtual void Draw(Camera camera, int drawOrder) {
            //准备模型
            if (drawOrder == m_drawOrders[0]) {
                bool skipped = false;
                ModsManager.HookAction(
                    "PrepareModels",
                    loader => {
                        loader.PrepareModels(this, camera, skipped, out bool skip);
                        skipped |= skip;
                        return false;
                    }
                );
                if (!skipped) {
                    ModelsDrawn = 0;
                    List<ModelData>[] modelsToDraw = m_modelsToDraw;
                    for (int i = 0; i < modelsToDraw.Length; i++) {
                        modelsToDraw[i].Clear();
                    }
                    m_modelsToPrepare.Clear();
                    foreach (ModelData value in m_componentModels.Values) {
                        if (value.ComponentModel.Model != null) {
                            value.ComponentModel.CalculateIsVisible(camera);
                            if (value.ComponentModel.IsVisibleForCamera) {
                                m_modelsToPrepare.Add(value);
                            }
                        }
                    }
                    m_modelsToPrepare.Sort();
                    if (UseCustomRendering && CustomRenderer != null) {
                        // 自定义渲染：不走 m_modelsToDraw，自定义渲染器管理自己的队列
                        foreach (ModelData item in m_modelsToPrepare) {
                            PrepareModel(item, camera);
                        }
                        CustomRenderer.BeginFrame(camera, m_modelsToPrepare);
                        ModelsDrawn += m_modelsToPrepare.Count;
                    }
                    else {
                        // 标准路径
                        foreach (ModelData item in m_modelsToPrepare) {
                            PrepareModel(item, camera);
                            m_modelsToDraw[(int)item.ComponentModel.RenderingMode].Add(item);
                        }
                    }
                }
            }
            if (!DisableDrawingModels) {
                bool skipped = false;
                ModsManager.HookAction(
                    "RenderModels",
                    loader => {
                        loader.RenderModels(this, camera, drawOrder, skipped, out bool skip);
                        skipped |= skip;
                        return false;
                    }
                );
                if (!skipped) {
                    if (drawOrder == m_drawOrders[1]) //绘制类型为AlphaThreshold的Model
                    {
                        if (UseCustomRendering && CustomRenderer != null) {
                            CustomRenderer.RenderOpaquePass();
                        }
                        else {
                            Display.DepthStencilState = DepthStencilState.Default;
                            Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
                            Display.BlendState = BlendState.Opaque;
                            DrawModels(camera, m_modelsToDraw[0], null);
                            Display.RasterizerState = RasterizerState.CullNoneScissor;
                            DrawModels(camera, m_modelsToDraw[1], 0f);
                            Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
                            m_primitivesRenderer.Flush(camera.ProjectionMatrix, true, 0);
                        }
                    }
                    else if (drawOrder == m_drawOrders[2]) //绘制TransparentBeforeWater的Model
                    {
                        if (UseCustomRendering && CustomRenderer != null) {
                            // 自定义渲染在 drawOrders[3](201) 统一处理，此处跳过
                        }
                        else {
                            Display.DepthStencilState = DepthStencilState.Default;
                            Display.RasterizerState = RasterizerState.CullNoneScissor;
                            Display.BlendState = BlendState.AlphaBlend;
                            DrawModels(camera, m_modelsToDraw[2], null);
                        }
                    }
                    else if (drawOrder == m_drawOrders[3]) //绘制TransparentAfterWater的Model
                    {
                        if (UseCustomRendering && CustomRenderer != null) {
                            CustomRenderer.RenderTransparentPass(underwater: false);
                            CustomRenderer.RenderTransparentPass(underwater: true);
                            m_primitivesRenderer.Flush(camera.ProjectionMatrix);
                        }
                        else {
                            Display.DepthStencilState = DepthStencilState.Default;
                            Display.RasterizerState = RasterizerState.CullNoneScissor;
                            Display.BlendState = BlendState.AlphaBlend;
                            DrawModels(camera, m_modelsToDraw[3], null);
                            if (ShaderOpaque != null
                                && ShaderAlphaTested != null) {
                                m_primitivesRenderer.Flush(camera.ProjectionMatrix);
                            }
                            else {
                                m_primitivesRenderer.Flush(camera.ProjectionMatrix);
                            }
                        }
                    }
                }
            }
            else {
                m_primitivesRenderer.Clear();
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemShadows = Project.FindSubsystem<SubsystemShadows>(true);
            // Dynamic uniform budget calculation
            // Non-array vertex shader uniforms: 12 vec4 (material, lights, fog, glymul)
            // Skinned shader has 8 extra vec4 (2 fixed mat4: world[1] + wvp[1])
            // Safety margin: 4 vec4 for driver overhead
            const int NonArrayOverhead = 12;
            const int SkinnedExtraOverhead = 8;
            const int SafetyMargin = 4;
            int available = GLWrapper.GL_MAX_VERTEX_UNIFORM_VECTORS - NonArrayOverhead - SafetyMargin;
            MaxInstancesCount = Math.Clamp(available / 8, 1, 64);
            MaxJointsCount = Math.Clamp((available - SkinnedExtraOverhead) / 4, 1, 128);
            m_jointMatricesBuffer = new Matrix[MaxJointsCount];
            ModsManager.HookAction(
                "GetMaxInstancesCount",
                modLoader => {
                    MaxInstancesCount = Math.Max(modLoader.GetMaxInstancesCount(), MaxInstancesCount);
                    return false;
                }
            );
            // Non-skinned shaders
            m_shaderOpaque = new ModelShader(
                ShaderCodeManager.GetFast("Shaders/Model.vsh"),
                ShaderCodeManager.GetFast("Shaders/Model.psh"),
                false,
                MaxInstancesCount
            );
            m_shaderAlphaTested = new ModelShader(
                ShaderCodeManager.GetFast("Shaders/Model.vsh"),
                ShaderCodeManager.GetFast("Shaders/Model.psh"),
                true,
                MaxInstancesCount
            );
            // Skinned shaders: maxInstancesCount=1 because skinned models never batch
            // This frees uniform space for more joints
            m_shaderSkinnedOpaque = new ModelShader(
                ShaderCodeManager.GetFast("Shaders/Model.vsh"),
                ShaderCodeManager.GetFast("Shaders/Model.psh"),
                false,
                1,
                MaxJointsCount
            );
            m_shaderSkinnedAlphaTested = new ModelShader(
                ShaderCodeManager.GetFast("Shaders/Model.vsh"),
                ShaderCodeManager.GetFast("Shaders/Model.psh"),
                true,
                1,
                MaxJointsCount
            );
        }

        public override void OnEntityAdded(Entity entity) {
            foreach (ComponentModel item in entity.FindComponents<ComponentModel>()) {
                ModelData value = new() {
                    ComponentModel = item, ComponentBody = item.Entity.FindComponent<ComponentBody>(), Light = m_subsystemSky.SkyLightIntensity
                };
                m_componentModels.Add(item, value);
            }
        }

        public override void OnEntityRemoved(Entity entity) {
            foreach (ComponentModel item in entity.FindComponents<ComponentModel>()) {
                m_componentModels.Remove(item);
            }
        }

        public virtual void PrepareModel(ModelData modelData, Camera camera) {
            if (Time.FrameIndex > modelData.LastAnimateFrame) {
                modelData.ComponentModel.Animate();
                modelData.LastAnimateFrame = Time.FrameIndex;
            }
            if (Time.FrameStartTime >= modelData.NextLightTime) {
                float? num = CalculateModelLight(modelData);
                if (num.HasValue) {
                    modelData.Light = num.Value;
                }
                modelData.NextLightTime = Time.FrameStartTime + 0.1;
            }
            modelData.ComponentModel.CalculateAbsoluteBonesTransforms(camera);
        }

        public virtual void DrawModels(Camera camera, List<ModelData> modelsData, float? alphaThreshold) {
            // Separate skinned and non-skinned models (use pre-allocated buffers)
            m_nonSkinnedModelsBuffer.Clear();
            m_skinnedModelsBuffer.Clear();

            foreach (var modelData in modelsData) {
                if (modelData.ComponentModel.Model?.HasSkin == true) {
                    m_skinnedModelsBuffer.Add(modelData);
                } else {
                    m_nonSkinnedModelsBuffer.Add(modelData);
                }
            }

            if (m_nonSkinnedModelsBuffer.Count > 0) {
                DrawInstancedModels(camera, m_nonSkinnedModelsBuffer, alphaThreshold);
            }
            foreach (var skinnedModel in m_skinnedModelsBuffer) {
                DrawSkinnedModel(camera, skinnedModel, alphaThreshold);
            }


            // Draw extras (shadows, etc.)
            DrawModelsExtras(camera, modelsData);
        }

        public virtual void DrawInstancedModels(Camera camera, List<ModelData> modelsData, float? alphaThreshold) {
            ModelShader modelShader = ShaderOpaque != null && ShaderAlphaTested != null ? alphaThreshold.HasValue ? ShaderAlphaTested : ShaderOpaque :
                alphaThreshold.HasValue ? m_shaderAlphaTested : m_shaderOpaque;
            modelShader.LightDirection1 = -Vector3.TransformNormal(LightingManager.DirectionToLight1, camera.ViewMatrix);
            modelShader.LightDirection2 = -Vector3.TransformNormal(LightingManager.DirectionToLight2, camera.ViewMatrix);
            modelShader.FogColor = new Vector3(m_subsystemSky.ViewFogColor);
            modelShader.FogBottomTopDensity = new Vector3(
                m_subsystemSky.ViewFogBottom - camera.ViewPosition.Y,
                m_subsystemSky.ViewFogTop - camera.ViewPosition.Y,
                m_subsystemSky.ViewFogDensity
            );
            modelShader.HazeStartDensity = new Vector2(m_subsystemSky.ViewHazeStart, m_subsystemSky.ViewHazeDensity);
            modelShader.FogYMultiplier = m_subsystemSky.VisibilityRangeYMultiplier;
            modelShader.WorldUp = Vector3.TransformNormal(Vector3.UnitY, camera.ViewMatrix);
            modelShader.Transforms.View = Matrix.Identity;
            modelShader.Transforms.Projection = camera.ProjectionMatrix;
            // SamplerState 会在每个模型绘制时根据模型类型设置
            if (alphaThreshold.HasValue) {
                modelShader.AlphaThreshold = alphaThreshold.Value;
            }
            ModsManager.HookAction(
                "ModelShaderParameter",
                modLoader => {
                    modLoader.ModelShaderParameter(modelShader, camera, modelsData, alphaThreshold);
                    return true;
                }
            );
            ModsManager.HookAction(
                "SetShaderParameter",
                modLoader => {
                    modLoader.SetShaderParameter(modelShader, camera);
                    return true;
                }
            );
            foreach (ModelData modelsDatum in modelsData) {
                bool skipDrawing = false;
                ModsManager.HookAction(
                    "OnModelDataDrawing",
                    modLoader => {
                        modLoader.OnModelDataDrawing(modelsDatum, modelShader, camera, this, out bool skip);
                        skipDrawing |= skip;
                        return false;
                    }
                );
                if (!skipDrawing) {
                    ComponentModel componentModel = modelsDatum.ComponentModel;
                    Model model = componentModel.Model;

                    // 设置通用着色器参数
                    modelShader.InstancesCount = componentModel.AbsoluteBoneTransformsForCamera.Length;
                    modelShader.EmissionColor = componentModel.EmissionColor ?? Vector4.Zero;
                    modelShader.AmbientLightColor = new Vector3(LightingManager.LightAmbient * modelsDatum.Light);
                    modelShader.DiffuseLightColor1 = new Vector3(modelsDatum.Light);
                    modelShader.DiffuseLightColor2 = new Vector3(modelsDatum.Light);

                    Array.Copy(
                        componentModel.AbsoluteBoneTransformsForCamera,
                        modelShader.Transforms.World,
                        componentModel.AbsoluteBoneTransformsForCamera.Length
                    );

                    // 获取按材质分组的实例化数据
                    Dictionary<int, InstancedModelData> dataByMaterial = InstancedModelsManager.GetInstancedModelDataByMaterial(
                        model,
                        componentModel.MeshDrawOrders
                    );

                    // 按材质分别绘制
                    foreach (var kvp in dataByMaterial) {
                        int materialIndex = kvp.Key;
                        InstancedModelData instancedData = kvp.Value;
                        ModelMaterial material = materialIndex >= 0 ? model.GetMaterial(materialIndex) : null;

                        // 设置材质颜色
                        Vector4 baseColor;
                        if (componentModel.DiffuseColor.HasValue) {
                            baseColor = new Vector4(componentModel.DiffuseColor.Value, 1f);
                        } else if (material != null) {
                            baseColor = material.BaseColorFactor;
                        } else {
                            baseColor = model?.GetDefaultBaseColorFactor() ?? Vector4.One;
                        }
                        float opacity = componentModel.Opacity ?? baseColor.W;
                        modelShader.MaterialColor = new Vector4(new Vector3(baseColor.X, baseColor.Y, baseColor.Z) * opacity, opacity);

                        // 设置纹理
                        if (componentModel.TextureOverride != null) {
                            modelShader.Texture = componentModel.TextureOverride;
                            modelShader.SamplerState = SamplerState.PointClamp;
                        } else {
                            int texIndex = material?.BaseColorTexture?.TextureIndex ?? -1;
                            if (texIndex >= 0) {
                                Texture2D texture = model.GetTexture(texIndex);
                                modelShader.Texture = texture;
                                modelShader.SamplerState = texture.SamplerState;
                            } else {
                                modelShader.Texture = Model.DefaultWhiteTexture;
                                modelShader.SamplerState = SamplerState.PointClamp;
                            }
                        }

                        Display.DrawIndexed(
                            PrimitiveType.TriangleList,
                            modelShader,
                            instancedData.VertexBuffer,
                            instancedData.IndexBuffer,
                            0,
                            instancedData.IndexBuffer.IndicesCount
                        );
                    }
                    ModelsDrawn++;
                }
                //画名称
                ModsManager.HookAction(
                    "OnModelRendererDrawExtra",
                    modLoader => {
                        modLoader.OnModelRendererDrawExtra(this, modelsDatum, camera, alphaThreshold);
                        return false;
                    }
                );
            }
        }

        /// <summary>
        /// Draw a single skinned model with GPU skinning
        /// </summary>
        public virtual void DrawSkinnedModel(Camera camera, ModelData modelData, float? alphaThreshold) {
            ComponentModel componentModel = modelData.ComponentModel;
            Model model = componentModel.Model;

            if (model?.Skin == null) return;

            // Select skinned shader
            ModelShader skinnedShader = ShaderSkinnedOpaque != null && ShaderSkinnedAlphaTested != null
                ? alphaThreshold.HasValue ? ShaderSkinnedAlphaTested : ShaderSkinnedOpaque
                : alphaThreshold.HasValue ? m_shaderSkinnedAlphaTested : m_shaderSkinnedOpaque;

            // Set shader parameters
            skinnedShader.LightDirection1 = -Vector3.TransformNormal(LightingManager.DirectionToLight1, camera.ViewMatrix);
            skinnedShader.LightDirection2 = -Vector3.TransformNormal(LightingManager.DirectionToLight2, camera.ViewMatrix);
            skinnedShader.FogColor = new Vector3(m_subsystemSky.ViewFogColor);
            skinnedShader.FogBottomTopDensity = new Vector3(
                m_subsystemSky.ViewFogBottom - camera.ViewPosition.Y,
                m_subsystemSky.ViewFogTop - camera.ViewPosition.Y,
                m_subsystemSky.ViewFogDensity
            );
            skinnedShader.HazeStartDensity = new Vector2(m_subsystemSky.ViewHazeStart, m_subsystemSky.ViewHazeDensity);
            skinnedShader.FogYMultiplier = m_subsystemSky.VisibilityRangeYMultiplier;
            skinnedShader.WorldUp = Vector3.TransformNormal(Vector3.UnitY, camera.ViewMatrix);
            // 蒙皮模型：World[0] 设置为 ViewMatrix，View 设置为 Identity
            // 这样 u_worldMatrix[0] 能将世界空间坐标转换到视图空间（用于雾效计算）
            // 同时 WorldViewProjection = ViewMatrix * Projection 是正确的
            skinnedShader.Transforms.World[0] = camera.ViewMatrix;
            skinnedShader.Transforms.View = Matrix.Identity;
            skinnedShader.Transforms.Projection = camera.ProjectionMatrix;

            if (alphaThreshold.HasValue) {
                skinnedShader.AlphaThreshold = alphaThreshold.Value;
            }

            skinnedShader.InstancesCount = 1; // Skinned models use single instance
            skinnedShader.EmissionColor = componentModel.EmissionColor ?? Vector4.Zero;
            skinnedShader.AmbientLightColor = new Vector3(LightingManager.LightAmbient * modelData.Light);
            skinnedShader.DiffuseLightColor1 = new Vector3(modelData.Light);
            skinnedShader.DiffuseLightColor2 = new Vector3(modelData.Light);

            // Calculate joint matrices for GPU skinning
            // Reference: Plan/GPUSkinningPitfalls.md
            Matrix invertedView = camera.InvertedViewMatrix;
            int jointCount = CalculateJointMatrices(componentModel, model, invertedView, m_jointMatricesBuffer);
            skinnedShader.JointMatrices = m_jointMatricesBuffer;

            // Draw model meshes directly (not using InstancedModelsManager which doesn't support skinned vertices)
            foreach (int meshIndex in componentModel.MeshDrawOrders) {
                ModelMesh mesh = model.Meshes[meshIndex];
                if (!mesh.IsVisible) {
                    continue;
                }
                foreach (ModelMeshPart meshPart in mesh.MeshParts) {
                    if (meshPart.IndicesCount == 0) continue;

                    // 获取该 mesh part 的材质
                    int materialIndex = meshPart.MaterialIndex;
                    ModelMaterial material = materialIndex >= 0 ? model.GetMaterial(materialIndex) : null;

                    // 设置材质颜色
                    Vector4 baseColor;
                    if (componentModel.DiffuseColor.HasValue) {
                        baseColor = new Vector4(componentModel.DiffuseColor.Value, 1f);
                    } else if (material != null) {
                        baseColor = material.BaseColorFactor;
                    } else {
                        baseColor = model.GetDefaultBaseColorFactor() ?? Vector4.One;
                    }
                    float opacity = componentModel.Opacity ?? baseColor.W;
                    skinnedShader.MaterialColor = new Vector4(new Vector3(baseColor.X, baseColor.Y, baseColor.Z) * opacity, opacity);

                    // 设置纹理
                    if (componentModel.TextureOverride != null) {
                        skinnedShader.Texture = componentModel.TextureOverride;
                    } else {
                        int texIndex = material?.BaseColorTexture?.TextureIndex ?? -1;
                        if (texIndex >= 0) {
                            skinnedShader.Texture = model.GetTexture(texIndex);
                        } else {
                            skinnedShader.Texture = Model.DefaultWhiteTexture;
                        }
                    }

                    // 设置采样器
                    skinnedShader.SamplerState = model.GetDefaultSamplerState() ?? SamplerState.LinearWrap;

                    Display.DrawIndexed(
                        PrimitiveType.TriangleList,
                        skinnedShader,
                        meshPart.VertexBuffer,
                        meshPart.IndexBuffer,
                        meshPart.StartIndex,
                        meshPart.IndicesCount
                    );
                }
            }
            ModelsDrawn++;
        }

        public virtual void DrawModelsExtras(Camera camera, List<ModelData> modelsData) {
            foreach (ModelData modelData in modelsData) {
                if (modelData.ComponentBody != null
                    && modelData.ComponentModel.CastsShadow) {
                    Vector3 shadowPosition = modelData.ComponentBody.Position + new Vector3(0f, 0.02f, 0f);
                    BoundingBox boundingBox = modelData.ComponentBody.BoundingBox;
                    float shadowDiameter = 2.25f * (boundingBox.Max.X - boundingBox.Min.X);
                    m_subsystemShadows.QueueShadow(camera, shadowPosition, shadowDiameter, modelData.ComponentModel.Opacity ?? 1f);
                }
                modelData.ComponentModel.DrawExtras(camera);
            }
        }

        /// <summary>
        /// 计算骨骼矩阵用于 GPU skinning
        /// </summary>
        /// <param name="componentModel">模型组件</param>
        /// <param name="model">模型对象</param>
        /// <param name="invertedView">反转的视图矩阵</param>
        /// <param name="output">输出缓冲区</param>
        /// <returns>实际计算的骨骼数量</returns>
        public static int CalculateJointMatrices(ComponentModel componentModel, Model model, Matrix invertedView, Span<Matrix> output) {
            ModelSkin skin = model.Skin;
            int jointCount = Math.Min(skin.JointCount, output.Length);

            if (skin.JointCount > output.Length) {
                Log.Warning($"Model has {skin.JointCount} joints, but output buffer only fits {output.Length}. Visual artifacts may occur.");
            }

            // Get root bone transform (coordinate conversion) and its inverse
            Matrix rootBoneTransform = model.RootBone.Transform;
            Matrix invRootBoneTransform = Matrix.Invert(rootBoneTransform);

            for (int i = 0; i < jointCount; i++) {
                if (i < skin.Joints.Count && skin.Joints[i] != null) {
                    ModelBone joint = skin.Joints[i];

                    // Step 1: Get joint world transform (in game space, includes entity position)
                    Matrix jointWorld = componentModel.AbsoluteBoneTransformsForCamera[joint.Index] * invertedView;

                    // Step 2: Convert to glTF space (remove root bone's coordinate conversion)
                    Matrix jointWorldGlTF = jointWorld * invRootBoneTransform;

                    // Step 3: Get inverse bind matrix (in glTF space)
                    Matrix inverseBind = i < skin.InverseBindMatrices?.Length
                        ? skin.InverseBindMatrices[i]
                        : Matrix.Identity;

                    // Step 4: Calculate joint matrix
                    // jointMatrix = inverseBind * jointWorldGlTF * rootBoneTransform
                    output[i] = inverseBind * jointWorldGlTF * rootBoneTransform;
                } else {
                    output[i] = Matrix.Identity;
                }
            }

            return jointCount;
        }

        public virtual float? CalculateModelLight(ModelData modelData) {
            Vector3 p;
            if (modelData.ComponentBody != null) {
                p = modelData.ComponentBody.Position;
                p.Y += 0.95f * (modelData.ComponentBody.BoundingBox.Max.Y - modelData.ComponentBody.BoundingBox.Min.Y);
            }
            else {
                Matrix? boneTransform = modelData.ComponentModel.GetBoneTransform(modelData.ComponentModel.Model.RootBone.Index);
                p = !boneTransform.HasValue ? Vector3.Zero : boneTransform.Value.Translation + new Vector3(0f, 0.9f, 0f);
            }
            return LightingManager.CalculateSmoothLight(m_subsystemTerrain, p);
        }

        //阴影绘制
        public virtual void ShadowDraw(SubsystemShadows subsystemShadows, Camera camera, Vector3 shadowPosition, float shadowDiameter, float alpha) {
            if (!SettingsManager.ObjectsShadowsEnabled) {
                return;
            }
            float num = Vector3.DistanceSquared(camera.ViewPosition, shadowPosition);
            if (!(num <= 1024f)) {
                return;
            }
            float num2 = MathF.Sqrt(num);
            float num3 = MathUtils.Saturate(4f * (1f - num2 / 32f));
            float num4 = shadowDiameter / 2f; //阴影直径/2
            int num5 = Terrain.ToCell(shadowPosition.X - num4);
            int num6 = Terrain.ToCell(shadowPosition.Z - num4);
            int num7 = Terrain.ToCell(shadowPosition.X + num4);
            int num8 = Terrain.ToCell(shadowPosition.Z + num4);
            for (int i = num5; i <= num7; i++) {
                for (int j = num6; j <= num8; j++) {
                    int num9 = MathUtils.Min(Terrain.ToCell(shadowPosition.Y), 255);
                    int num10 = MathUtils.Max(num9 - 2, 0);
                    for (int num11 = num9; num11 >= num10; num11--) {
                        int cellValueFast = subsystemShadows.m_subsystemTerrain.Terrain.GetCellValueFast(i, num11, j);
                        int num12 = Terrain.ExtractContents(cellValueFast);
                        Block block = BlocksManager.Blocks[num12];
                        if (block.ObjectShadowStrength > 0f) {
                            BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(subsystemShadows.m_subsystemTerrain, cellValueFast);
                            for (int k = 0; k < customCollisionBoxes.Length; k++) {
                                BoundingBox boundingBox = customCollisionBoxes[k];
                                float num13 = boundingBox.Max.Y + num11;
                                if (shadowPosition.Y - num13 > -0.5f) {
                                    float num14 = camera.ViewPosition.Y - num13;
                                    if (num14 > 0f) {
                                        float num15 = MathUtils.Max(num14 * 0.01f, 0.005f);
                                        float num16 = MathUtils.Saturate(1f - (shadowPosition.Y - num13) / 2f);
                                        Vector3 p = new(boundingBox.Min.X + i, num13 + num15, boundingBox.Min.Z + j);
                                        Vector3 p2 = new(boundingBox.Max.X + i, num13 + num15, boundingBox.Min.Z + j);
                                        Vector3 p3 = new(boundingBox.Max.X + i, num13 + num15, boundingBox.Max.Z + j);
                                        Vector3 p4 = new(boundingBox.Min.X + i, num13 + num15, boundingBox.Max.Z + j);
                                        subsystemShadows.DrawShadowOverQuad(
                                            p,
                                            p2,
                                            p3,
                                            p4,
                                            shadowPosition,
                                            shadowDiameter,
                                            0.45f * block.ObjectShadowStrength * alpha * num3 * num16
                                        );
                                    }
                                }
                            }
                            break;
                        }
                        if (num12 == 18) {
                            break;
                        }
                    }
                }
            }
        }
    }
}