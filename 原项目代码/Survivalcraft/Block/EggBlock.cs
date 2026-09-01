using System.Globalization;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game {
    public class EggBlock : Block {
        public class EggType {
            public int EggTypeIndex;

            public bool ShowEgg;

            public string DisplayName;

            public string TemplateName;

            public float NutritionalValue;

            public int TextureSlot;

            public Color Color;

            public Vector2 ScaleUV;

            public bool SwapUV;

            public float Scale;

            public BlockMesh BlockMesh;
        }

        public static string fName = "EggBlock";
        public static int Index = 118;
        public Dictionary<int, EggType> m_eggTypes = [];
        public ReadOnlyList<EggType> EggTypes => new(m_eggTypes.Values.ToList());

        public override void Initialize() {
            m_eggTypes.Clear();
            DatabaseObjectType parameterSetType = DatabaseManager.GameDatabase.ParameterSetType;
            Guid eggParameterSetGuid = new("300ff557-775f-4c7c-a88a-26655369f00b");
            foreach (DatabaseObject item in from o in DatabaseManager.GameDatabase.Database.Root.GetExplicitNestingChildren(parameterSetType, false)
                where o.EffectiveInheritanceRoot.Guid == eggParameterSetGuid
                select o) {
                int nestedValue = item.GetNestedValue<int>("EggTypeIndex");
                if (nestedValue >= 0) {
                    nestedValue &= 0xFFF;
                    string value = item.GetNestedValue<string>("DisplayName");
                    if (value.StartsWith('[')
                        && value.EndsWith(']')) {
                        string[] lp = value.Substring(1, value.Length - 2).Split([":"], StringSplitOptions.RemoveEmptyEntries);
                        value = LanguageControl.GetDatabase("DisplayName", lp[1]);
                    }
                    m_eggTypes[nestedValue] = new EggType {
                        EggTypeIndex = nestedValue,
                        ShowEgg = item.GetNestedValue<bool>("ShowEgg"),
                        DisplayName = value,
                        TemplateName = item.NestingParent.Name,
                        NutritionalValue = item.GetNestedValue<float>("NutritionalValue"),
                        Color = item.GetNestedValue<Color>("Color"),
                        ScaleUV = item.GetNestedValue<Vector2>("ScaleUV"),
                        SwapUV = item.GetNestedValue<bool>("SwapUV"),
                        Scale = item.GetNestedValue<float>("Scale"),
                        TextureSlot = item.GetNestedValue<int>("TextureSlot")
                    };
                }
            }
            Model model = ContentManager.Get<Model>("Models/Egg");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Egg").ParentBone);
            foreach (EggType eggType in m_eggTypes.Values) {
                if (eggType == null) {
                    continue;
                }
                eggType.BlockMesh = new BlockMesh();
                eggType.BlockMesh.AppendModelMeshPart(
                    model.FindMesh("Egg").MeshParts[0],
                    boneAbsoluteTransform,
                    false,
                    false,
                    false,
                    false,
                    eggType.Color
                );
                Matrix identity = Matrix.Identity;
                if (eggType.SwapUV) {
                    identity.M11 = 0f;
                    identity.M12 = 1f;
                    identity.M21 = 1f;
                    identity.M22 = 0f;
                }
                identity *= Matrix.CreateScale(0.0625f * eggType.ScaleUV.X, 0.0625f * eggType.ScaleUV.Y, 1f);
                identity *= Matrix.CreateTranslation(eggType.TextureSlot % 16 / 16f, eggType.TextureSlot / 16 / 16f, 0f);
                eggType.BlockMesh.TransformTextureCoordinates(identity);
            }
            base.Initialize();
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            EggType eggType = GetEggType(Terrain.ExtractData(value));
            int data = Terrain.ExtractData(value);
            bool isCooked = GetIsCooked(data);
            bool isLaid = GetIsLaid(data);
            if (isCooked) {
                return string.Format(LanguageControl.Get(fName, 5), eggType.DisplayName);
            }
            if (!isLaid) {
                return eggType.DisplayName;
            }
            return string.Format(LanguageControl.Get(fName, 6), eggType.DisplayName);
        }

        public override string GetCategory(int value) => "Spawner Eggs";

        public override float GetNutritionalValue(int value) {
            EggType eggType = GetEggType(Terrain.ExtractData(value));
            if (!GetIsCooked(Terrain.ExtractData(value))) {
                return eggType.NutritionalValue;
            }
            return 1.5f * eggType.NutritionalValue;
        }

        public override float GetSicknessProbability(int value) {
            if (!GetIsCooked(Terrain.ExtractData(value))) {
                return DefaultSicknessProbability;
            }
            return 0f;
        }

        public override int GetRotPeriod(int value) {
            if (GetNutritionalValue(value) > 0f) {
                return base.GetRotPeriod(value);
            }
            return 0;
        }

        public override int GetDamage(int value) => (Terrain.ExtractData(value) >> 16) & 1;

        public override int SetDamage(int value, int damage) {
            int num = Terrain.ExtractData(value);
            num = (num & -65537) | ((damage & 1) << 16);
            return Terrain.ReplaceData(value, num);
        }

        public override int GetDamageDestructionValue(int value) => 246;

        public override IEnumerable<int> GetCreativeValues() {
            EggType[] eggs = m_eggTypes.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
            foreach (EggType eggType in eggs) {
                if (eggType == null) {
                    continue;
                }
                if (eggType.ShowEgg) {
                    yield return Terrain.MakeBlockValue(118, 0, SetEggType(0, eggType.EggTypeIndex));
                    if (eggType.NutritionalValue > 0f) {
                        yield return Terrain.MakeBlockValue(118, 0, SetIsCooked(SetEggType(0, eggType.EggTypeIndex), true));
                    }
                }
            }
        }

        public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes() {
            string description = LanguageControl.Get(fName, 4);
            foreach (EggType eggType in EggTypes) {
                if (eggType == null) {
                    continue;
                }
                if (eggType.NutritionalValue > 0f) {
                    int rot = 0;
                    while (rot <= 1) {
                        CraftingRecipe craftingRecipe = new() {
                            ResultCount = 1,
                            ResultValue = Terrain.MakeBlockValue(118, 0, SetEggType(SetIsCooked(0, true), eggType.EggTypeIndex)),
                            RemainsCount = 1,
                            RemainsValue = Terrain.MakeBlockValue(91),
                            RequiredHeatLevel = 1f,
                            Description = description
                        };
                        int data = SetEggType(SetIsLaid(0, true), eggType.EggTypeIndex);
                        int value = SetDamage(Terrain.MakeBlockValue(118, 0, data), rot);
                        craftingRecipe.Ingredients[0] = $"egg:{Terrain.ExtractData(value).ToString(CultureInfo.InvariantCulture)}";
                        craftingRecipe.Ingredients[1] = "waterbucket";
                        yield return craftingRecipe;
                        rot++;
                    }
                }
            }
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            int data = Terrain.ExtractData(value);
            EggType eggType = GetEggType(data);
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, eggType.BlockMesh, color, eggType.Scale * size, ref matrix, environmentData);
            } else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, eggType.BlockMesh, texture, color, eggType.Scale * size, ref matrix, environmentData);
            }
        }

        public EggType GetEggType(int data) {
            int index = (data >> 4) & 0xFFF;
            bool found = m_eggTypes.TryGetValue(index, out EggType eggType);
            if (found) {
                return eggType;
            }
            return m_eggTypes[0];
        }

        public EggType GetEggTypeByCreatureTemplateName(string templateName) {
            return m_eggTypes.FirstOrDefault(pair => pair.Value.TemplateName == templateName).Value;
        }

        public static bool GetIsCooked(int data) => (data & 1) != 0;

        public static int SetIsCooked(int data, bool isCooked) {
            if (!isCooked) {
                return data & -2;
            }
            return data | 1;
        }

        public static bool GetIsLaid(int data) => (data & 2) != 0;

        public static int SetIsLaid(int data, bool isLaid) {
            if (!isLaid) {
                return data & -3;
            }
            return data | 2;
        }

        public static int SetEggType(int data, int eggTypeIndex) {
            data &= -65521;
            data |= (eggTypeIndex & 0xFFF) << 4;
            return data;
        }
    }
}