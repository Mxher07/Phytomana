using System.Text.Json;
using Engine;
using Engine.Graphics;
using Engine.Media;
using TemplatesDatabase;

namespace Game {
    public static class CharacterSkinsManager {
        public static List<string> m_characterSkinNames = [];
        public static Dictionary<PlayerClass, Model> m_playerModels = [];
        public static Dictionary<PlayerClass, Model> m_playerModelsForClothing = [];
        public static Dictionary<PlayerClass, Model> m_outerClothingModels = [];
        public static bool AddEmptySkin;
        public static bool UseEmptySkinAsDefault;
        public const string fName = "CharacterSkinsManager";

        public static ReadOnlyList<string> CharacterSkinsNames => new(m_characterSkinNames);

        public static string CharacterSkinsDirectoryName => ModsManager.CharacterSkinsDirectoryName;

        public static event Action<string> CharacterSkinDeleted;

        public static void Initialize() {
            Storage.CreateDirectory(CharacterSkinsDirectoryName);
        }

        public static bool IsBuiltIn(string name) => name.StartsWith('$');

        public static PlayerClass? GetPlayerClass(string name) {
            name = name.ToLower();
            if (name.Contains("female")
                || name.Contains("girl")
                || name.Contains("woman")) {
                return PlayerClass.Female;
            }
            if (name.Contains("male")
                || name.Contains("boy")
                || name.Contains("man")) {
                return PlayerClass.Male;
            }
            return null;
        }

        public static string GetFileName(string name) {
            if (IsBuiltIn(name)) {
                return null;
            }
            return Storage.CombinePaths(CharacterSkinsDirectoryName, name);
        }

        public static string GetDisplayName(string name) {
            if (IsBuiltIn(name)) {
                if (name == "$Empty") {
                    return LanguageControl.Get(fName, "SkinName", "Empty");
                }
                if (name.StartsWith("$Female")) {
                    return LanguageControl.Get(fName, "SkinName", "Female", name.Substring(7));
                }
                if (name.StartsWith("$Male")) {
                    return LanguageControl.Get(fName, "SkinName", "Male", name.Substring(5));
                }
            }
            return Storage.GetFileNameWithoutExtension(name);
        }

        public static DateTime GetCreationDate(string name) {
            try {
                string fileName = GetFileName(name);
                if (!string.IsNullOrEmpty(fileName)) {
                    return Storage.GetFileLastWriteTime(fileName);
                }
            }
            catch {
                // ignored
            }
            return new DateTime(2000, 1, 1);
        }

        public static Texture2D LoadTexture(string name) {
            if (string.IsNullOrEmpty(name)) {
                return null;
            }
            Texture2D texture2D = null;
            try {
                string fileName = GetFileName(name);
                if (!string.IsNullOrEmpty(fileName)) {
                    using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                        ValidateCharacterSkin(stream);
                        stream.Position = 0L;
                        texture2D = Texture2D.Load(stream);
                    }
                }
                else if (name == "$Empty") {
                    return null;
                }
                else {
                    texture2D = ContentManager.Get<Texture2D>($"Textures/Creatures/Human{name.Substring(1).Replace(" ", "")}");
                }
            }
            catch (Exception ex) {
                Log.Warning(string.Format(LanguageControl.Get(fName, "1"), name, ex.Message));
            }
            if (texture2D == null) {
                texture2D = ContentManager.Get<Texture2D>("Textures/Creatures/HumanMale1");
            }
            return texture2D;
        }

        public static string ImportCharacterSkin(string name, Stream stream) {
            Exception ex = ExternalContentManager.VerifyExternalContentName(name);
            if (ex != null) {
                throw ex;
            }
            if (Storage.GetExtension(name) != ".scskin") {
                name += ".scskin";
            }
            ValidateCharacterSkin(stream);
            stream.Position = 0L;
            using (Stream destination = Storage.OpenFile(GetFileName(name), OpenFileMode.Create)) {
                stream.CopyTo(destination);
                return name;
            }
        }

        public static void DeleteCharacterSkin(string name) {
            try {
                string fileName = GetFileName(name);
                if (!string.IsNullOrEmpty(fileName)) {
                    Storage.DeleteFile(fileName);
                    CharacterSkinDeleted?.Invoke(name);
                }
            }
            catch (Exception e) {
                ExceptionManager.ReportExceptionToUser($"Unable to delete character skin \"{name}\"", e);
            }
        }

        public static void UpdateCharacterSkinsList() {
            m_characterSkinNames.Clear();
            m_characterSkinNames.Add("$Male1");
            m_characterSkinNames.Add("$Male2");
            m_characterSkinNames.Add("$Male3");
            m_characterSkinNames.Add("$Male4");
            m_characterSkinNames.Add("$Female1");
            m_characterSkinNames.Add("$Female2");
            m_characterSkinNames.Add("$Female3");
            m_characterSkinNames.Add("$Female4");
            if (AddEmptySkin) {
                m_characterSkinNames.Add("$Empty");
            }
            foreach (string item in Storage.ListFileNames(CharacterSkinsDirectoryName)) {
                if (Storage.GetExtension(item).ToLower() == ".scskin") {
                    m_characterSkinNames.Add(item);
                }
            }
        }

        public static Model GetPlayerModel(PlayerClass playerClass) => GetPlayerModel(playerClass, false);

        public static Model GetPlayerModel(PlayerClass playerClass, bool forClothing) {
            ValuesDictionary humanModelValueDictionary = null;
            if (forClothing) {
                if (m_playerModelsForClothing.TryGetValue(playerClass, out Model modelForClothing)) {
                    return modelForClothing;
                }
                ValuesDictionary playerValuesDictionary;
                switch (playerClass) {
                    case PlayerClass.Male: playerValuesDictionary = DatabaseManager.FindEntityValuesDictionary("MalePlayer", true); break;
                    case PlayerClass.Female: playerValuesDictionary = DatabaseManager.FindEntityValuesDictionary("FemalePlayer", true); break;
                    default: throw new InvalidOperationException("Unknown player class.");
                }
                humanModelValueDictionary = playerValuesDictionary.GetValue<ValuesDictionary>("HumanModel");
                string modelNameForClothing = humanModelValueDictionary.GetValue<string>("ModelNameForClothing", null);
                if (!string.IsNullOrEmpty(modelNameForClothing)) {
                    modelForClothing = ContentManager.Get<Model>(modelNameForClothing);
                    m_playerModelsForClothing.Add(playerClass, modelForClothing);
                    return modelForClothing;
                }
            }
            if (!m_playerModels.TryGetValue(playerClass, out Model model)) {
                if (humanModelValueDictionary == null) {
                    ValuesDictionary playerValuesDictionary;
                    switch (playerClass) {
                        case PlayerClass.Male: playerValuesDictionary = DatabaseManager.FindEntityValuesDictionary("MalePlayer", true); break;
                        case PlayerClass.Female: playerValuesDictionary = DatabaseManager.FindEntityValuesDictionary("FemalePlayer", true); break;
                        default: throw new InvalidOperationException("Unknown player class.");
                    }
                    humanModelValueDictionary = playerValuesDictionary.GetValue<ValuesDictionary>("HumanModel");
                }
                model = ContentManager.Get<Model>(humanModelValueDictionary.GetValue<string>("ModelName"));
                // 应用骨骼别名表：此路径在 ClothingBlock.Initialize / PlayerModelWidget 等“方块/UI 上下文”中加载玩家模型，
                // 它们会用旧硬编码骨骼名（Hand1/Leg1/...）调用 throwing FindBone。glTF 玩家模型没有这些骨骼，
                // 必须在这里（模型加载并缓存时）把 AnimationConfig.boneAliases 写入 Model.BoneAliases，
                // 与 ComponentModel.SetModel 的运行时路径保持一致，否则这些 throwing 调用会崩溃。
                string animationConfigPath = humanModelValueDictionary.GetValue("AnimationConfigPath", "");
                if (!string.IsNullOrEmpty(animationConfigPath)) {
                    string configJson = ContentManager.Get<string>(animationConfigPath, ".json");
                    // 注意：这里只反序列化提取 boneAliases，不调用 LoadFromJsonNode/ValidateConfig。
                    // 原因：ClothingBlock.Initialize 在方块加载阶段运行（LoadingScreen 第 489 行），早于
                    // AnimationTemplateRegistration.Register（第 528 行），此时 Human 模板尚未注册，
                    // ValidateConfig 会因 "Unknown template" 抛异常。这里不需要控制器/状态规则，只需别名。
                    var config = System.Text.Json.Nodes.JsonNode.Parse(configJson)
                        .Deserialize<Engine.Animation.AnimationConfig>(Engine.Animation.AnimationConfigLoader.s_jsonOptions);
                    model.BoneAliases = config?.BoneAliases;
                }
                m_playerModels.Add(playerClass, model);
            }
            return model;
        }

        public static Model GetOuterClothingModel(PlayerClass playerClass) {
            if (!m_outerClothingModels.TryGetValue(playerClass, out Model value)) {
                ValuesDictionary valuesDictionary;
                switch (playerClass) {
                    case PlayerClass.Male: valuesDictionary = DatabaseManager.FindEntityValuesDictionary("MalePlayer", true); break;
                    case PlayerClass.Female: valuesDictionary = DatabaseManager.FindEntityValuesDictionary("FemalePlayer", true); break;
                    default: throw new InvalidOperationException("Unknown player class.");
                }
                value = ContentManager.Get<Model>(valuesDictionary.GetValue<ValuesDictionary>("OuterClothingModel").GetValue<string>("ModelName"));
                m_outerClothingModels.Add(playerClass, value);
            }
            return value;
        }

        public static void ValidateCharacterSkin(Stream stream) {
            Image image = Image.Load(stream);
            if (image.Width > 65536
                || image.Height > 65536) {
                throw new InvalidOperationException($"Character skin is larger than 65536x65536 pixels (size={image.Width}x{image.Height})");
            }
            if (!MathUtils.IsPowerOf2(image.Width)
                || !MathUtils.IsPowerOf2(image.Height)) {
                throw new InvalidOperationException($"Character skin does not have power-of-two size (size={image.Width}x{image.Height})");
            }
        }
    }
}