using Engine;
using Engine.Animation;
using Engine.Graphics;
using Engine.Media;
using System.Text.Json.Nodes;

namespace Game {
    public class ModelWidget : Widget {
        // Non-skinned shaders (LitShader for simple models - no INSTANCE attribute needed)
        public static LitShader m_shader = new(1, false, false, true, false, false);

        public static LitShader m_shaderAlpha = new(1, false, false, true, false, true);

        // Skinned shaders (lazy initialization to avoid static constructor issues)
        private static ModelShader s_shaderSkinnedOpaque;
        private static ModelShader s_shaderSkinnedAlphaTested;

        public static ModelShader m_shaderSkinnedOpaque {
            get {
                if (s_shaderSkinnedOpaque == null) {
                    string vsh = ShaderCodeManager.GetFast("Shaders/Model.vsh");
                    string psh = ShaderCodeManager.GetFast("Shaders/Model.psh");
                    if (string.IsNullOrEmpty(vsh) || string.IsNullOrEmpty(psh)) {
                        return null;
                    }
                    s_shaderSkinnedOpaque = new ModelShader(vsh, psh, false, 7, SubsystemModelsRenderer.MaxJointsCount);
                }
                return s_shaderSkinnedOpaque;
            }
            set => s_shaderSkinnedOpaque = value;
        }

        public static ModelShader m_shaderSkinnedAlphaTested {
            get {
                if (s_shaderSkinnedAlphaTested == null) {
                    string vsh = ShaderCodeManager.GetFast("Shaders/Model.vsh");
                    string psh = ShaderCodeManager.GetFast("Shaders/Model.psh");
                    if (string.IsNullOrEmpty(vsh) || string.IsNullOrEmpty(psh)) {
                        return null;
                    }
                    s_shaderSkinnedAlphaTested = new ModelShader(vsh, psh, true, 7, SubsystemModelsRenderer.MaxJointsCount);
                }
                return s_shaderSkinnedAlphaTested;
            }
            set => s_shaderSkinnedAlphaTested = value;
        }

        // Pre-allocated buffer for joint matrices (avoid GC pressure)
        private static Matrix[] s_jointMatricesBuffer;

        public List<Model> Models = new();

        public Dictionary<Model, Matrix?[]> m_boneTransforms = new();

        public Dictionary<Model, Matrix[]> m_absoluteBoneTransforms = new();

        // per-model 动画控制器（镜像 m_boneTransforms 结构）。AddModel 传入 config/template 时创建。
        public Dictionary<Model, AnimationController> m_animationControllers = new();

        // per-model 简单动画播放器（直接播模型内置动画，无需 json）。PlayAnimation 创建。
        public Dictionary<Model, AnimationPlayer> m_animationPlayers = new();

        /// <summary>动画总开关。false 时跳过 Update 驱动，保留手动 SetBoneTransform 摆姿态能力。</summary>
        public bool AnimationEnabled { get; set; } = true;

        /// <summary>取模型的动画控制器（无则 null）。调用方可借此 SetParameter 驱动状态规则。</summary>
        public AnimationController GetAnimationController(Model model) {
            m_animationControllers.TryGetValue(model, out AnimationController controller);
            return controller;
        }

        /// <summary>取模型的动画播放器（无则 null）。调用方可借此调 Speed/PhaseRange/SetNormalizedTime。</summary>
        public AnimationPlayer GetAnimationPlayer(Model model) {
            m_animationPlayers.TryGetValue(model, out AnimationPlayer player);
            return player;
        }

        /// <summary>
        /// Texture override dictionary. When a model is not found in this dictionary,
        /// the model's internal texture will be used.
        /// </summary>
        public Dictionary<Model, Texture2D> Textures = new();

        public Vector2 Size { get; set; }

        public Color Color { get; set; }

        public bool UseAlphaThreshold { get; set; }

        public bool IsPerspective { get; set; }

        public Vector3 OrthographicFrustumSize { get; set; }

        public Vector3 ViewPosition { get; set; }

        public Vector3 ViewTarget { get; set; }

        public float ViewFov { get; set; }

        public Matrix ModelMatrix { get; set; } = Matrix.Identity;

        public Vector3 AutoRotationVector { get; set; }

        [Obsolete(
            "A ModelWidget may contains multiple models, please use Models field instead. This field only represents the first model of Models field."
        )]
        public Model Model {
            get => Models?[0] ?? null;
            set {
                if (value != null) {
                    if (Models.Count == 0) {
                        Models.Add(value);
                    }
                    else {
                        Models[0] = value;
                    }
                    m_boneTransforms[value] = new Matrix?[value.Bones.Count];
                    m_absoluteBoneTransforms[value] = new Matrix[value.Bones.Count];
                }
                else {
                    Models.RemoveAt(0);
                }
            }
        }

        public Action<ModelWidget, Shader, Model, ModelMesh> OnSetupShaderParameters;

        /// <summary>
        ///     Custom shader. If null, default shader will be used for rendering.
        ///     When using custom shader, you may need to set parameters via <see cref="OnSetupShaderParameters" />, otherwise it may not work.
        /// </summary>
        public TransformedShader CustomShader { get; set; }

        public static ICustomModelWidgetRenderer CustomRenderer { get; set; }

        public void AddModel(Model value) => AddModel(value, null, null);

        public void AddModel(Model value, string animationConfigPath = null, string animationTemplateName = null) {
            if (value == null) {
                return;
            }
            Models.Add(value);
            m_boneTransforms[value] = new Matrix?[value.Bones.Count];
            m_absoluteBoneTransforms[value] = new Matrix[value.Bones.Count];

            // 指定 config 或 template 时建控制器（mirror ComponentModel.SetModel:381-394）
            if (!string.IsNullOrEmpty(animationConfigPath)
                || !string.IsNullOrEmpty(animationTemplateName)) {
                AnimationController controller = CreateController(value, animationConfigPath, animationTemplateName);
                if (controller != null) {
                    m_animationControllers[value] = controller;
                    // 双向互斥：controller 优先于 player（Update 内 ContainsKey 守卫）。建 controller 时移除 player，
                    // 避免调用方先 PlayAnimation 再 AddModel(config) 后两 dict 同时持有该 model（player 休眠残留）。
                    m_animationPlayers.Remove(value);
                }
            }
        }

        /// <summary>
        /// 按 config 路径或模板名建动画控制器。mirror ComponentModel.SetModel 的建控制器块。
        /// config 优先于 template。失败抛异常（与 ComponentModel 一致，content 错误应显式暴露）。
        /// </summary>
        AnimationController CreateController(Model model, string animationConfigPath, string animationTemplateName) {
            if (!string.IsNullOrEmpty(animationConfigPath)) {
                string json = ContentManager.Get<string>(animationConfigPath, ".json");
                var loader = new AnimationConfigLoader();
                AnimationConfig config = loader.LoadFromJsonNode(JsonNode.Parse(json));
                model.BoneAliases = config.BoneAliases; // 骨骼别名：旧硬编码骨骼名 → glb 真实名
                return loader.CreateController(config, model);
            }
            return new AnimationController(model, animationTemplateName);
        }

        /// <summary>
        /// 播放模型内置动画（按名称）。无需 json 配置——直接采样模型 Animations 列表。
        /// 适合“只想循环播放内置动画”的简单场景。与 AnimationController 互斥：调用即移除该模型的 controller。
        /// </summary>
        public AnimationPlayer PlayAnimation(Model model, string animationName, bool loop = true) {
            ModelAnimation anim = model?.Animations.Find(a => a.Name == animationName);
            return PlayAnimationInternal(model, anim, loop);
        }

        /// <summary>播放模型内置动画（按索引，默认第 0 个）。无需 json。</summary>
        public AnimationPlayer PlayAnimation(Model model, int animationIndex = 0, bool loop = true) {
            ModelAnimation anim = (model != null && animationIndex >= 0 && animationIndex < model.Animations.Count)
                ? model.Animations[animationIndex]
                : null;
            return PlayAnimationInternal(model, anim, loop);
        }

        AnimationPlayer PlayAnimationInternal(Model model, ModelAnimation animation, bool loop) {
            if (model == null || animation == null) {
                return null;
            }
            // 确保模型已建骨骼缓冲（未 AddModel 则补一次）
            if (!m_boneTransforms.ContainsKey(model)) {
                AddModel(model);
            }
            var player = new AnimationPlayer();
            player.SetAnimation(model, animation);
            player.Play(loop);
            m_animationPlayers[model] = player;
            // controller 与 player 互斥：player 驱动时移除 controller（Update 中 controller 优先）
            m_animationControllers.Remove(model);
            return player;
        }

        public bool RemoveModel(Model value) {
            if (value != null) {
                Models.Remove(value);
                m_boneTransforms.Remove(value);
                m_absoluteBoneTransforms.Remove(value);
                Textures.Remove(value);
                m_animationControllers.Remove(value);
                m_animationPlayers.Remove(value);
                return true;
            }
            return false;
        }

        [Obsolete("A ModelWidget may contains multiple models. TextureOverride only represents the texture of the first model.")]
        public Texture2D TextureOverride {
            get => Textures[Models[0]];
            set => Textures[Models[0]] = value;
        }

        public ModelWidget() {
            Size = new Vector2(float.PositiveInfinity);
            IsHitTestVisible = false;
            Color = Color.White;
            UseAlphaThreshold = false;
            IsPerspective = true;
            ViewPosition = new Vector3(0f, 0f, -5f);
            ViewTarget = new Vector3(0f, 0f, 0f);
            ViewFov = 1f;
            OrthographicFrustumSize = new Vector3(0f, 10f, 10f);
        }

        public Matrix? GetBoneTransform(Model model, int boneIndex) => m_boneTransforms[model][boneIndex];

        public void SetBoneTransform(Model model, int boneIndex, Matrix? transformation) {
            m_boneTransforms[model][boneIndex] = transformation;
        }

        public override void Draw(DrawContext dc) {
            if (Models.Count == 0) {
                return;
            }

            // Setup view and projection matrices
            Matrix viewMatrix = Matrix.CreateLookAt(ViewPosition, ViewTarget, Vector3.UnitY);
            Viewport viewport = Display.Viewport;
            float aspectRatio = ActualSize.X / ActualSize.Y;

            Matrix projectionMatrix;
            if (IsPerspective) {
                projectionMatrix = Matrix.CreatePerspectiveFieldOfView(ViewFov, aspectRatio, 0.1f, 100f)
                    * MatrixUtils.CreateScaleTranslation(0.5f * ActualSize.X, -0.5f * ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f)
                    * GlobalTransform
                    * MatrixUtils.CreateScaleTranslation(2f / viewport.Width, -2f / viewport.Height, -1f, 1f);
            }
            else {
                Vector3 orthographicFrustumSize = OrthographicFrustumSize;
                if (orthographicFrustumSize.X < 0f) {
                    orthographicFrustumSize.X = orthographicFrustumSize.Y / aspectRatio;
                }
                else if (orthographicFrustumSize.Y < 0f) {
                    orthographicFrustumSize.Y = orthographicFrustumSize.X * aspectRatio;
                }
                projectionMatrix =
                    Matrix.CreateOrthographic(orthographicFrustumSize.X, orthographicFrustumSize.Y, 0f, OrthographicFrustumSize.Z)
                    * MatrixUtils.CreateScaleTranslation(0.5f * ActualSize.X, -0.5f * ActualSize.Y, ActualSize.X / 2f, ActualSize.Y / 2f)
                    * GlobalTransform
                    * MatrixUtils.CreateScaleTranslation(2f / viewport.Width, -2f / viewport.Height, -1f, 1f);
            }

            Display.DepthStencilState = DepthStencilState.Default;
            Display.BlendState = BlendState.AlphaBlend;
            Display.RasterizerState = RasterizerState.CullNoneScissor;

            // Process bone hierarchy for all models
            foreach (Model model in Models) {
                ProcessBoneHierarchy(model.RootBone, Matrix.Identity);
            }

            // Calculate auto rotation
            float time = (float)Time.RealTime + GetHashCode() % 1000 / 100f;
            Matrix autoRotation = AutoRotationVector.LengthSquared() > 0f
                ? Matrix.CreateFromAxisAngle(Vector3.Normalize(AutoRotationVector), AutoRotationVector.Length() * time)
                : Matrix.Identity;

            // Use custom shader if provided
            if (CustomShader != null) {
                CustomShader.Transforms.View = viewMatrix;
                CustomShader.Transforms.Projection = projectionMatrix;
                foreach (Model model in Models) {
                    foreach (ModelMesh mesh in model.Meshes) {
                        CustomShader.Transforms.World[0] = GetMeshTransform(model, mesh) * ModelMatrix * autoRotation;
                        OnSetupShaderParameters?.Invoke(this, CustomShader, model, mesh);
                        foreach (ModelMeshPart meshPart in mesh.MeshParts) {
                            if (meshPart.IndicesCount == 0) continue;
                            Texture2D texture = GetTexture(model, meshPart);
                            CustomShader.GetParameter("u_texture", true)?.SetValue(texture);
                            Display.DrawIndexed(
                                PrimitiveType.TriangleList,
                                CustomShader,
                                meshPart.VertexBuffer,
                                meshPart.IndexBuffer,
                                meshPart.StartIndex,
                                meshPart.IndicesCount
                            );
                        }
                    }
                }
                return;
            }

            if (CustomRenderer != null) {
                CustomRenderer.Render(new ModelWidgetRenderContext(
                    this,
                    viewMatrix,
                    projectionMatrix,
                    ModelMatrix * autoRotation,
                    Color * GlobalColorTransform,
                    UseAlphaThreshold));
                return;
            }

            // Separate skinned and non-skinned models
            List<Model> skinnedModels = Models.Where(m => m.HasSkin).ToList();
            List<Model> nonSkinnedModels = Models.Where(m => !m.HasSkin).ToList();

            // Draw non-skinned models with LitShader
            if (nonSkinnedModels.Count > 0) {
                DrawNonSkinnedModels(nonSkinnedModels, viewMatrix, projectionMatrix, autoRotation);
            }

            // Draw skinned models with ModelShader
            foreach (Model skinnedModel in skinnedModels) {
                DrawSkinnedModel(skinnedModel, viewMatrix, projectionMatrix, autoRotation);
            }
        }

        private void DrawNonSkinnedModels(List<Model> models, Matrix viewMatrix, Matrix projectionMatrix, Matrix autoRotation) {
            LitShader shader = UseAlphaThreshold ? m_shaderAlpha : m_shader;

            shader.Transforms.View = viewMatrix;
            shader.Transforms.Projection = projectionMatrix;
            shader.SamplerState = SamplerState.PointClamp;
            shader.MaterialColor = new Vector4(Color * GlobalColorTransform);
            shader.AmbientLightColor = new Vector3(0.66f, 0.66f, 0.66f);
            shader.DiffuseLightColor1 = new Vector3(1f, 1f, 1f);
            shader.LightDirection1 = Vector3.Normalize(new Vector3(1f, 1f, 1f));
            if (UseAlphaThreshold) {
                shader.AlphaThreshold = 0f;
            }

            foreach (Model model in models) {
                DrawModelMeshesLit(shader, model, autoRotation);
            }
        }

        private void DrawSkinnedModel(Model model, Matrix viewMatrix, Matrix projectionMatrix, Matrix autoRotation) {
            if (model?.Skin == null) return;

            ModelShader shader = UseAlphaThreshold ? m_shaderSkinnedAlphaTested : m_shaderSkinnedOpaque;
            if (shader == null) return;

            // Restore depth test
            Display.DepthStencilState = DepthStencilState.Default;

            // Calculate joint matrices with ModelMatrix and autoRotation applied
            if (s_jointMatricesBuffer == null || s_jointMatricesBuffer.Length < SubsystemModelsRenderer.MaxJointsCount) {
                s_jointMatricesBuffer = new Matrix[SubsystemModelsRenderer.MaxJointsCount];
            }
            int jointCount = CalculateJointMatrices(model, ModelMatrix * autoRotation, s_jointMatricesBuffer);
            for (int i = jointCount; i < s_jointMatricesBuffer.Length; i++) {
                s_jointMatricesBuffer[i] = Matrix.Identity;
            }

            // Use Widget's projection matrix with proper view
            // The key is to use view/projection matrices that match how non-skinned models work
            shader.JointMatrices = s_jointMatricesBuffer;
            shader.Transforms.View = viewMatrix;
            shader.Transforms.Projection = projectionMatrix;
            shader.Transforms.World[0] = Matrix.Identity;
            shader.InstancesCount = 1;

            shader.MaterialColor = new Vector4(Color * GlobalColorTransform);
            shader.EmissionColor = Vector4.Zero;
            shader.AmbientLightColor = new Vector3(0.66f, 0.66f, 0.66f);
            shader.DiffuseLightColor1 = new Vector3(1f, 1f, 1f);
            shader.DiffuseLightColor2 = new Vector3(0.4f, 0.4f, 0.4f);
            shader.LightDirection1 = Vector3.Normalize(new Vector3(1f, 1f, 1f));
            shader.LightDirection2 = Vector3.Normalize(new Vector3(-1f, -0.5f, -0.25f));
            // Disable fog
            shader.FogColor = Vector3.Zero;
            shader.FogBottomTopDensity = new Vector3(0f, 1f, 0f);
            shader.HazeStartDensity = Vector2.Zero;
            shader.FogYMultiplier = 1f;
            shader.WorldUp = Vector3.UnitY;
            if (UseAlphaThreshold) {
                shader.AlphaThreshold = 0f;
            }

            // Draw each mesh part with material
            foreach (ModelMesh mesh in model.Meshes) {
                OnSetupShaderParameters?.Invoke(this, shader, model, mesh);

                foreach (ModelMeshPart meshPart in mesh.MeshParts) {
                    if (meshPart.IndicesCount == 0) continue;

                    Texture2D texture = GetTexture(model, meshPart);
                    shader.Texture = texture;
                    shader.SamplerState = model.GetDefaultSamplerState() ?? SamplerState.LinearWrap;

                    Display.DrawIndexed(
                        PrimitiveType.TriangleList,
                        shader,
                        meshPart.VertexBuffer,
                        meshPart.IndexBuffer,
                        meshPart.StartIndex,
                        meshPart.IndicesCount
                    );
                }
            }
        }

        internal int CalculateJointMatrices(Model model, Matrix modelTransform, Matrix[] destination) {
            ModelSkin skin = model.Skin;
            if (skin == null) return 0;

            if (destination == null) {
                throw new ArgumentNullException(nameof(destination));
            }

            int jointCount = Math.Min(skin.JointCount, destination.Length);

            // Get root bone transform for coordinate conversion
            Matrix rootBoneTransform = model.RootBone.Transform;
            Matrix invRootBoneTransform = Matrix.Invert(rootBoneTransform);

            for (int i = 0; i < jointCount; i++) {
                if (i < skin.Joints.Count && skin.Joints[i] != null) {
                    ModelBone joint = skin.Joints[i];

                    // Get joint transform in model space (from ProcessBoneHierarchy)
                    Matrix jointLocal = m_absoluteBoneTransforms[model][joint.Index];

                    // Convert to glTF space
                    Matrix jointGlTF = jointLocal * invRootBoneTransform;

                    // Get inverse bind matrix
                    Matrix inverseBind = i < skin.InverseBindMatrices?.Length
                        ? skin.InverseBindMatrices[i]
                        : Matrix.Identity;

                    // Calculate joint matrix:
                    // jointMatrix = inverseBind * jointGlTF * rootBoneTransform * modelTransform
                    // This transforms: vertex(bind) -> glTF model space -> game world space
                    destination[i] = inverseBind * jointGlTF * rootBoneTransform * modelTransform;
                }
                else {
                    destination[i] = Matrix.Identity;
                }
            }

            return jointCount;
        }

        private void DrawModelMeshesLit(LitShader shader, Model model, Matrix autoRotation) {
            shader.InstancesCount = 1;

            foreach (ModelMesh mesh in model.Meshes) {
                shader.Transforms.World[0] = GetMeshTransform(model, mesh) * ModelMatrix * autoRotation;

                OnSetupShaderParameters?.Invoke(this, shader, model, mesh);

                foreach (ModelMeshPart meshPart in mesh.MeshParts) {
                    if (meshPart.IndicesCount == 0) continue;

                    // Set texture: use override texture or model's internal texture
                    Texture2D texture = GetTexture(model, meshPart);
                    shader.Texture = texture;

                    Display.DrawIndexed(
                        PrimitiveType.TriangleList,
                        shader,
                        meshPart.VertexBuffer,
                        meshPart.IndexBuffer,
                        meshPart.StartIndex,
                        meshPart.IndicesCount
                    );
                }
            }
        }

        internal Matrix GetMeshTransform(Model model, ModelMesh mesh) {
            return m_absoluteBoneTransforms[model][mesh.ParentBone.Index];
        }

        internal Texture2D GetTexture(Model model, ModelMeshPart meshPart) {
            // First check if there's an override texture
            Texture2D overrideTexture = GetTextureOverride(model);
            if (overrideTexture != null) {
                return overrideTexture;
            }

            // Then check material texture
            int materialIndex = meshPart.MaterialIndex;
            if (materialIndex >= 0) {
                ModelMaterial material = model.GetMaterial(materialIndex);
                int texIndex = material?.BaseColorTexture?.TextureIndex ?? -1;
                if (texIndex >= 0) {
                    Texture2D materialTexture = model.GetTexture(texIndex);
                    if (materialTexture != null) {
                        return materialTexture;
                    }
                }
            }

            // Fallback to default white texture
            return Model.DefaultTransparentTexture;
        }

        internal Texture2D GetTextureOverride(Model model) {
            return Textures.TryGetValue(model, out Texture2D overrideTexture) ? overrideTexture : null;
        }

        public override void Update() {
            // Widget.Update 每帧由 UpdateWidgetsHierarchy 调用（Widget.cs:680 base + 层级遍历），暂停时仍触发（UI 常驻）。
            // Time.FrameDuration 本身是实时帧增量（Time.cs:80），故暂停时动画继续——满足 UI 预览需求。
            if (!AnimationEnabled
                || (m_animationControllers.Count == 0 && m_animationPlayers.Count == 0)) {
                return;
            }
            float dt = Time.FrameDuration;
            // 1. controller 驱动（mirror ComponentModel.Animate:198-214）
            foreach ((Model model, AnimationController controller) in m_animationControllers) {
                if (!m_boneTransforms.TryGetValue(model, out Matrix?[] bt)) {
                    continue;
                }
                // 清上一帧（控制器只写它驱动的骨骼，null → ProcessBoneHierarchy 回退 rest pose）
                for (int i = 0; i < bt.Length; i++) {
                    bt[i] = null;
                }
                controller.Update(dt);
                controller.ComputeBoneTransforms(bt);
            }
            // 2. player 驱动（mirror ComponentModel.Animate:230-241）。有 controller 的模型跳过（互斥）。
            foreach ((Model model, AnimationPlayer player) in m_animationPlayers) {
                if (m_animationControllers.ContainsKey(model)) {
                    continue;
                }
                if (!player.IsPlaying) {
                    continue;
                }
                if (!m_boneTransforms.TryGetValue(model, out Matrix?[] bt)) {
                    continue;
                }
                for (int i = 0; i < bt.Length; i++) {
                    bt[i] = null;
                }
                player.Update(dt);
                player.SampleBoneTransforms(bt);
                player.SamplePointerTargets(model);
                player.SampleMorphWeights(model);
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = Models.Count > 0;
            DesiredSize = Size;
        }

        public void ProcessBoneHierarchy(ModelBone modelBone, Matrix currentTransform) {
            Matrix[] transforms = m_absoluteBoneTransforms[modelBone.Model];
            Matrix m = modelBone.Transform;

            if (m_boneTransforms[modelBone.Model][modelBone.Index].HasValue) {
                if (modelBone.Model.HasSkin) {
                    // glTF 蒙皮模型：动画采样的是完整的局部变换，直接替换骨骼变换
                    m = m_boneTransforms[modelBone.Model][modelBone.Index].Value;
                }
                else {
                    // DAE 模型：保留骨骼的原始平移，只替换旋转/缩放
                    Vector3 translation = m.Translation;
                    m.Translation = Vector3.Zero;
                    m *= m_boneTransforms[modelBone.Model][modelBone.Index].Value;
                    m.Translation += translation;
                }
            }

            Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);

            foreach (ModelBone childBone in modelBone.ChildBones) {
                ProcessBoneHierarchy(childBone, transforms[modelBone.Index]);
            }
        }
    }
}
