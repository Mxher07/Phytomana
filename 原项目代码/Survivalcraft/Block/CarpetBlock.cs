using Engine;
using Engine.Graphics;

namespace Game {
    public class CarpetBlock : CubeBlock {
        public static int Index = 208;

        public BoundingBox[] m_collisionBoxes = [new(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.0625f, 1f))];

        public CarpetBlock() => CanBeBuiltIntoFurniture = true;

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) => face != 5;

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            int data = Terrain.ExtractData(value);
            Color fabricColor = SubsystemPalette.GetFabricColor(generator, GetColor(data));
            generator.GenerateCubeVertices(
                this,
                value,
                x,
                y,
                z,
                0.0625f,
                0.0625f,
                0.0625f,
                0.0625f,
                fabricColor,
                fabricColor,
                fabricColor,
                fabricColor,
                fabricColor,
                -1,
                texture == null ? geometry.OpaqueSubsetsByFace : geometry.GetGeometry(texture).OpaqueSubsetsByFace
            );
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            int data = Terrain.ExtractData(value);
            color *= SubsystemPalette.GetFabricColor(environmentData, GetColor(data));
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawCubeBlock(
                    primitivesRenderer,
                    value,
                    new Vector3(size, 0.0625f * size, size),
                    ref matrix,
                    color,
                    color,
                    environmentData
                );
            }
            else {
                BlocksManager.DrawCubeBlock(
                    primitivesRenderer,
                    value,
                    new Vector3(size, 0.0625f * size, size),
                    ref matrix,
                    color,
                    color,
                    environmentData,
                    texture
                );
            }
        }

        public override IEnumerable<int> GetCreativeValues() {
            int i = 0;
            while (i < 16) {
                yield return Terrain.MakeBlockValue(208, 0, i);
                int num = i + 1;
                i = num;
            }
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            showDebris = true;
            if (toolLevel >= RequiredToolLevel) {
                int data = Terrain.ExtractData(oldValue);
                dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(208, 0, data), Count = 1 });
            }
        }

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) {
            int data = Terrain.ExtractData(value);
            Color fabricColor = SubsystemPalette.GetFabricColor(subsystemTerrain, GetColor(data));
            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, fabricColor, DefaultTextureSlot);
        }

        public override CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain subsystemTerrain,
            string[] ingredients,
            float heatLevel,
            float playerLevel) {
            if (heatLevel < 1f) {
                return null;
            }
            List<string> list = ingredients.Where(i => !string.IsNullOrEmpty(i)).ToList();
            if (list.Count == 2) {
                int num = 0;
                int num2 = 0;
                int num3 = 0;
                foreach (string item in list) {
                    CraftingRecipesManager.DecodeIngredient(item, out string craftingId, out int? data);
                    if (craftingId == BlocksManager.Blocks[208].CraftingId) {
                        num3 = Terrain.MakeBlockValue(208, 0, data ?? 0);
                    }
                    else if (craftingId == BlocksManager.Blocks[129].CraftingId) {
                        num = Terrain.MakeBlockValue(129, 0, data ?? 0);
                    }
                    else if (craftingId == BlocksManager.Blocks[128].CraftingId) {
                        num2 = Terrain.MakeBlockValue(128, 0, data ?? 0);
                    }
                }
                if (num != 0
                    && num3 != 0) {
                    int num4 = Terrain.ExtractData(num3);
                    int color = PaintBucketBlock.GetColor(Terrain.ExtractData(num));
                    int damage = BlocksManager.Blocks[129].GetDamage(num);
                    Block block = BlocksManager.Blocks[129];
                    int num5 = PaintBucketBlock.CombineColors(num4, color);
                    if (num5 != num4) {
                        return new CraftingRecipe {
                            ResultCount = 1,
                            ResultValue = Terrain.MakeBlockValue(208, 0, num5),
                            RemainsCount = 1,
                            RemainsValue =
                                BlocksManager.DamageItem(Terrain.MakeBlockValue(129, 0, color), damage + MathUtils.Max(block.Durability / 4, 1)),
                            RequiredHeatLevel = 1f,
                            Description = $"Dye carpet {SubsystemPalette.GetName(subsystemTerrain, color, null)}",
                            Ingredients = (string[])ingredients.Clone()
                        };
                    }
                }
                if (num2 != 0
                    && num3 != 0) {
                    int num6 = Terrain.ExtractData(num3);
                    int damage2 = BlocksManager.Blocks[128].GetDamage(num2);
                    Block block2 = BlocksManager.Blocks[128];
                    if (num6 != 0) {
                        return new CraftingRecipe {
                            ResultCount = 1,
                            ResultValue = Terrain.MakeBlockValue(208, 0, 0),
                            RemainsCount = 1,
                            RemainsValue = BlocksManager.DamageItem(
                                Terrain.MakeBlockValue(128, 0, 0),
                                damage2 + MathUtils.Max(block2.Durability / 4, 1)
                            ),
                            RequiredHeatLevel = 1f,
                            Description = "Undye carpet",
                            Ingredients = (string[])ingredients.Clone()
                        };
                    }
                }
            }
            return null;
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_collisionBoxes;

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            int value2 = Terrain.ExtractData(value);
            return SubsystemPalette.GetName(subsystemTerrain, value2, LanguageControl.GetBlock("CarpetBlock", "DisplayName"));
        }

        public static int GetColor(int data) => data & 0xF;

        public static int SetColor(int data, int color) => (data & -16) | (color & 0xF);

        public override Color GetFurnitureColor(int value, SubsystemPalette subsystemPalette) => subsystemPalette?.GetFabricColor(GetColor(Terrain.ExtractData(value))) ?? Color.White;
    }
}