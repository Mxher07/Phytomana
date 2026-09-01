using Engine;
using Engine.Graphics;

namespace Game {
    public class PlayerModelWidget : CanvasWidget {
        public enum Shot {
            Body = 0,
            Bust = 1
        }

        public ModelWidget m_modelWidget;

        public CharacterSkinsCache m_publicCharacterSkinsCache;

        public CharacterSkinsCache m_characterSkinsCache;

        public Vector2? m_lastDrag;

        public float m_rotation;

        public bool m_modelsDirty = true; // 首帧需加载；PlayerClass 变化时 setter 置 true

        public CharacterSkinsCache CharacterSkinsCache {
            get => m_characterSkinsCache;
            set {
                if (value != null) {
                    m_publicCharacterSkinsCache.Clear();
                    m_characterSkinsCache = value;
                }
                else {
                    m_characterSkinsCache = m_publicCharacterSkinsCache;
                }
            }
        }

        public Shot CameraShot { get; set; }

        public int AnimateHeadSeed { get; set; }

        public int AnimateHandsSeed { get; set; }

        public bool OuterClothing { get; set; }

        public PlayerClass m_playerClass;
        public PlayerClass PlayerClass {
            get => m_playerClass;
            set {
                if (m_playerClass == value) {
                    return;
                }
                m_playerClass = value;
                m_modelsDirty = true; // 值真变才 swap（多处每帧赋同值，如 PlayerScreen.Update）
            }
        }

        public PlayerData PlayerData { //目前仅赋值，游戏里没有使用该属性，但勿删，模组可能会用到
            get;
            set;
        }

        public string CharacterSkinName { get; set; }

        public Texture2D CharacterSkinTexture { get; set; }

        public Texture2D OuterClothingTexture { get; set; }

        public Model OuterClothingModel { get; set; }
        public Model PlayerModel { get; set; }

        public PlayerModelWidget() {
            m_modelWidget = new ModelWidget { UseAlphaThreshold = true, IsPerspective = true };
            Children.Add(m_modelWidget);
            IsHitTestVisible = false;
            m_publicCharacterSkinsCache = new CharacterSkinsCache();
            m_characterSkinsCache = m_publicCharacterSkinsCache;
        }

        public override void Update() {
            if (Input.Press.HasValue) {
                if (m_lastDrag.HasValue) {
                    m_rotation += 0.01f * (Input.Press.Value.X - m_lastDrag.Value.X);
                    m_lastDrag = Input.Press.Value;
                    Input.Clear();
                }
                else if (HitTestGlobal(Input.Press.Value) == this) {
                    m_lastDrag = Input.Press.Value;
                }
            }
            else {
                m_lastDrag = null;
                m_rotation = MathUtils.NormalizeAngle(m_rotation);
                if (MathF.Abs(m_rotation) > 0.01f) {
                    m_rotation *= MathUtils.PowSign(0.1f, Time.FrameDuration);
                }
                else {
                    m_rotation = 0f;
                }
            }
            m_modelWidget.ModelMatrix = m_rotation != 0f ? Matrix.CreateRotationY(m_rotation) : Matrix.Identity;
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            if (OuterClothing) {
                return;
            }
            if (m_modelsDirty) {
                m_modelWidget.RemoveModel(PlayerModel);
                m_modelWidget.RemoveModel(OuterClothingModel);
                OuterClothingModel = CharacterSkinsManager.GetOuterClothingModel(PlayerClass);
                PlayerModel = CharacterSkinsManager.GetPlayerModel(PlayerClass);
                m_modelWidget.AddModel(PlayerModel);
                m_modelWidget.AddModel(OuterClothingModel);
                m_modelsDirty = false;
            }
            if (CameraShot == Shot.Body) {
                m_modelWidget.ViewPosition = PlayerClass == PlayerClass.Male ? new Vector3(0f, 1.46f, -3.2f) : new Vector3(0f, 1.39f, -3.04f);
                m_modelWidget.ViewTarget = PlayerClass == PlayerClass.Male ? new Vector3(0f, 0.9f, 0f) : new Vector3(0f, 0.86f, 0f);
                m_modelWidget.ViewFov = 0.57f;
            }
            else {
                if (CameraShot != Shot.Bust) {
                    throw new InvalidOperationException("Unknown shot.");
                }
                m_modelWidget.ViewPosition = PlayerClass == PlayerClass.Male ? new Vector3(0f, 1.5f, -1.05f) : new Vector3(0f, 1.43f, -1f);
                m_modelWidget.ViewTarget = PlayerClass == PlayerClass.Male ? new Vector3(0f, 1.5f, 0f) : new Vector3(0f, 1.43f, 0f);
                m_modelWidget.ViewFov = 0.57f;
            }
            m_modelWidget.Textures[PlayerModel] = CharacterSkinName != null
                ? CharacterSkinsCache.GetTexture(CharacterSkinName)
                : CharacterSkinTexture;
            //OuterClothingTexture ??= new RenderTarget2D(m_modelWidget.Textures[PlayerModel].Width, m_modelWidget.Textures[PlayerModel].Height, 1, ColorFormat.Rgba8888, DepthFormat.None);
            m_modelWidget.Textures[OuterClothingModel] = OuterClothingTexture;
            if (AnimateHeadSeed != 0) {
                int num = AnimateHeadSeed < 0 ? GetHashCode() : AnimateHeadSeed;
                float num2 = (float)MathUtils.Remainder(Time.FrameStartTime + 1000.0 * num, 10000.0);
                float rotationZ = MathUtils.Lerp(-0.75f, 0.75f, SimplexNoise.OctavedNoise(num2 + 100f, 0.2f, 1, 2f, 0.5f));
                float rotationX = MathUtils.Lerp(-0.5f, 0.5f, SimplexNoise.OctavedNoise(num2 + 200f, 0.17f, 1, 2f, 0.5f));
                Matrix value = Matrix.CreateRotationX(rotationX) * Matrix.CreateRotationZ(rotationZ);
                m_modelWidget.SetBoneTransform(OuterClothingModel, OuterClothingModel.FindBone("Head").Index, value);
                m_modelWidget.SetBoneTransform(PlayerModel, PlayerModel.FindBone("Head").Index, value);
            }
            if (AnimateHandsSeed != 0) {
                int num3 = AnimateHandsSeed < 0 ? GetHashCode() : AnimateHandsSeed;
                float num4 = (float)MathUtils.Remainder(Time.FrameStartTime + 1000.0 * num3, 10000.0);
                Vector2 vector2 = default;
                vector2.X = MathUtils.Lerp(0.2f, 0f, SimplexNoise.OctavedNoise(num4 + 100f, 0.7f, 1, 2f, 0.5f));
                vector2.Y = MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.OctavedNoise(num4 + 200f, 0.7f, 1, 2f, 0.5f));
                Vector2 vector3 = default;
                vector3.X = MathUtils.Lerp(-0.2f, 0f, SimplexNoise.OctavedNoise(num4 + 300f, 0.7f, 1, 2f, 0.5f));
                vector3.Y = MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.OctavedNoise(num4 + 400f, 0.7f, 1, 2f, 0.5f));
                Matrix value2 = Matrix.CreateRotationX(vector2.Y) * Matrix.CreateRotationY(vector2.X);
                Matrix value3 = Matrix.CreateRotationX(vector3.Y) * Matrix.CreateRotationY(vector3.X);
                m_modelWidget.SetBoneTransform(PlayerModel, PlayerModel.FindBone("Hand1").Index, value2);
                m_modelWidget.SetBoneTransform(PlayerModel, PlayerModel.FindBone("Hand2").Index, value3);
            }
            ModsManager.HookAction(
                "OnPlayerModelWidgetMeasureOverride",
                loader => {
                    loader.OnPlayerModelWidgetMeasureOverride(this);
                    return false;
                }
            );
            base.MeasureOverride(parentAvailableSize);
        }

        public override void UpdateCeases() {
            if (RootWidget == null) {
                if (m_publicCharacterSkinsCache.ContainsTexture(m_modelWidget.Textures[PlayerModel])) {
                    m_modelWidget.Textures[PlayerModel] = null;
                }
                m_publicCharacterSkinsCache.Clear();
            }
            base.UpdateCeases();
        }
    }
}