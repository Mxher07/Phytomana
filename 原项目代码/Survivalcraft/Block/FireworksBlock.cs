using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game {
    public class FireworksBlock : Block {
        public enum Shape {
            SmallBurst = 0,
            LargeBurst = 1,
            Circle = 2,
            Disc = 3,
            Ball = 4,
            ShortTrails = 5,
            LongTrails = 6,
            FlatTrails = 7
        }

        public static int Index = 215;

        public BlockMesh[] m_headBlockMeshes = new BlockMesh[64];

        public BlockMesh[] m_bodyBlockMeshes = new BlockMesh[2];

        public BlockMesh[] m_finsBlockMeshes = new BlockMesh[2];

        public static readonly string[] HeadNames = [
            "HeadConeSmall",
            "HeadConeLarge",
            "HeadCylinderSmall",
            "HeadCylinderLarge",
            "HeadSphere",
            "HeadDiamondSmall",
            "HeadDiamondLarge",
            "HeadCylinderFlat"
        ];

        public static readonly Color[] FireworksColors = [
            new(255, 255, 255),
            new(85, 255, 255),
            new(255, 85, 85),
            new(85, 85, 255),
            new(255, 255, 85),
            new(85, 255, 85),
            new(255, 170, 0),
            new(255, 85, 255)
        ];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Fireworks");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Body").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Fins").ParentBone);
            for (int i = 0; i < 64; i++) {
                int num = i / 8;
                int num2 = i % 8;
                Color color = FireworksColors[num2];
                color *= 0.75f;
                color.A = byte.MaxValue;
                Matrix boneAbsoluteTransform3 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(HeadNames[num]).ParentBone);
                m_headBlockMeshes[i] = new BlockMesh();
                m_headBlockMeshes[i]
                    .AppendModelMeshPart(
                        model.FindMesh(HeadNames[num]).MeshParts[0],
                        boneAbsoluteTransform3 * Matrix.CreateTranslation(0f, -0.25f, 0f),
                        false,
                        false,
                        false,
                        false,
                        color
                    );
            }
            for (int j = 0; j < 2; j++) {
                float num3 = 0.5f + j * 0.5f;
                Matrix m = Matrix.CreateScale(new Vector3(num3, 1f, num3));
                m_bodyBlockMeshes[j] = new BlockMesh();
                m_bodyBlockMeshes[j]
                    .AppendModelMeshPart(
                        model.FindMesh("Body").MeshParts[0],
                        boneAbsoluteTransform * m * Matrix.CreateTranslation(0f, -0.25f, 0f),
                        false,
                        false,
                        false,
                        false,
                        Color.White
                    );
            }
            for (int k = 0; k < 2; k++) {
                m_finsBlockMeshes[k] = new BlockMesh();
                m_finsBlockMeshes[k]
                    .AppendModelMeshPart(
                        model.FindMesh("Fins").MeshParts[0],
                        boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.25f, 0f),
                        false,
                        false,
                        false,
                        false,
                        k == 0 ? Color.White : new Color(224, 0, 0)
                    );
            }
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            int data = Terrain.ExtractData(value);
            int color2 = GetColor(data);
            Shape shape = GetShape(data);
            int altitude = GetAltitude(data);
            bool flickering = GetFlickering(data);
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(
                    primitivesRenderer,
                    m_headBlockMeshes[(int)shape * 8 + color2],
                    color,
                    2f * size,
                    ref matrix,
                    environmentData
                );
            } else {
                BlocksManager.DrawMeshBlock(
                    primitivesRenderer,
                    m_headBlockMeshes[(int)shape * 8 + color2],
                    texture,
                    color,
                    2f * size,
                    ref matrix,
                    environmentData
                );
            }
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_bodyBlockMeshes[altitude], color, 2f * size, ref matrix, environmentData);
            } else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_bodyBlockMeshes[altitude], texture, color, 2f * size, ref matrix, environmentData);
            }
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_finsBlockMeshes[flickering ? 1 : 0], color, 2f * size, ref matrix, environmentData);
            } else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_finsBlockMeshes[flickering ? 1 : 0], texture, color, 2f * size, ref matrix, environmentData);
            }
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            int data = Terrain.ExtractData(value);
            int color = GetColor(data);
            Shape shape = GetShape(data);
            int altitude = GetAltitude(data);
            bool flickering = GetFlickering(data);
            return string.Format(
                LanguageControl.GetFireworks("Other", "1"),
                LanguageControl.GetFireworks("FireworksColorDisplayNames", color.ToString()),
                flickering ? LanguageControl.GetFireworks("Other", "2") : null,
                LanguageControl.GetFireworks("ShapeDisplayNames", ((int)shape).ToString()),
                altitude == 0 ? LanguageControl.GetFireworks("Other", "3") : LanguageControl.GetFireworks("Other", "4")
            );
        }

        public override IEnumerable<int> GetCreativeValues() {
            int color = 0;
            while (color < 8) {
                int num;
                for (int altitude = 0; altitude < 2; altitude = num) {
                    for (int flickering = 0; flickering < 2; flickering = num) {
                        for (int shape = 0; shape < 8; shape = num) {
                            yield return Terrain.MakeBlockValue(
                                215,
                                0,
                                SetColor(SetAltitude(SetShape(SetFlickering(0, flickering != 0), (Shape)shape), altitude), color)
                            );
                            num = shape + 1;
                        }
                        num = flickering + 1;
                    }
                    num = altitude + 1;
                }
                num = color + 1;
                color = num;
            }
        }

        public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes() {
            int shape = 0;
            while (shape < 8) {
                int num;
                for (int altitude = 0; altitude < 2; altitude = num) {
                    for (int flickering = 0; flickering < 2; flickering = num) {
                        for (int color = 0; color < 8; color = num) {
                            CraftingRecipe craftingRecipe = new() {
                                ResultCount = 20,
                                ResultValue = Terrain.MakeBlockValue(
                                    215,
                                    0,
                                    SetColor(SetAltitude(SetShape(SetFlickering(0, flickering != 0), (Shape)shape), altitude), color)
                                ),
                                RemainsCount = 1,
                                RemainsValue = Terrain.MakeBlockValue(90),
                                RequiredHeatLevel = 0f,
                                Description = LanguageControl.GetFireworks("CRDescription", null)
                            };
                            if (shape == 0) {
                                craftingRecipe.Ingredients[0] = null;
                                craftingRecipe.Ingredients[1] = "sulphurchunk";
                                craftingRecipe.Ingredients[2] = null;
                            }
                            if (shape == 1) {
                                craftingRecipe.Ingredients[0] = "sulphurchunk";
                                craftingRecipe.Ingredients[1] = "coalchunk";
                                craftingRecipe.Ingredients[2] = "sulphurchunk";
                            }
                            if (shape == 2) {
                                craftingRecipe.Ingredients[0] = "sulphurchunk";
                                craftingRecipe.Ingredients[1] = null;
                                craftingRecipe.Ingredients[2] = "sulphurchunk";
                            }
                            if (shape == 3) {
                                craftingRecipe.Ingredients[0] = "sulphurchunk";
                                craftingRecipe.Ingredients[1] = "sulphurchunk";
                                craftingRecipe.Ingredients[2] = "sulphurchunk";
                            }
                            if (shape == 4) {
                                craftingRecipe.Ingredients[0] = "coalchunk";
                                craftingRecipe.Ingredients[1] = "coalchunk";
                                craftingRecipe.Ingredients[2] = "coalchunk";
                            }
                            if (shape == 5) {
                                craftingRecipe.Ingredients[0] = null;
                                craftingRecipe.Ingredients[1] = "saltpeterchunk";
                                craftingRecipe.Ingredients[2] = null;
                            }
                            if (shape == 6) {
                                craftingRecipe.Ingredients[0] = "sulphurchunk";
                                craftingRecipe.Ingredients[1] = "saltpeterchunk";
                                craftingRecipe.Ingredients[2] = "sulphurchunk";
                            }
                            if (shape == 7) {
                                craftingRecipe.Ingredients[0] = "coalchunk";
                                craftingRecipe.Ingredients[1] = "saltpeterchunk";
                                craftingRecipe.Ingredients[2] = "coalchunk";
                            }
                            if (flickering == 0) {
                                craftingRecipe.Ingredients[3] = "canvas";
                                craftingRecipe.Ingredients[5] = "canvas";
                            }
                            if (flickering == 1) {
                                craftingRecipe.Ingredients[3] = "gunpowder";
                                craftingRecipe.Ingredients[5] = "gunpowder";
                            }
                            if (altitude == 0) {
                                craftingRecipe.Ingredients[6] = "gunpowder";
                                craftingRecipe.Ingredients[7] = null;
                                craftingRecipe.Ingredients[8] = "gunpowder";
                            }
                            if (altitude == 1) {
                                craftingRecipe.Ingredients[6] = "gunpowder";
                                craftingRecipe.Ingredients[7] = "gunpowder";
                                craftingRecipe.Ingredients[8] = "gunpowder";
                            }
                            craftingRecipe.Ingredients[4] = $"paintbucket:{(color != 7 ? color : 10).ToString(CultureInfo.InvariantCulture)}";
                            yield return craftingRecipe;
                            num = color + 1;
                        }
                        num = flickering + 1;
                    }
                    num = altitude + 1;
                }
                num = shape + 1;
                shape = num;
            }
        }

        public static Shape GetShape(int data) => (Shape)(data & 7);

        // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
        public static int SetShape(int data, Shape shape) => (data & -8) | (int)(shape & Shape.FlatTrails);

        public static int GetAltitude(int data) => (data >> 3) & 1;

        public static int SetAltitude(int data, int altitude) => (data & -9) | ((altitude & 1) << 3);

        public static bool GetFlickering(int data) => ((data >> 4) & 1) != 0;

        public static int SetFlickering(int data, bool flickering) => (data & -17) | ((flickering ? 1 : 0) << 4);

        public static int GetColor(int data) => (data >> 5) & 7;

        public static int SetColor(int data, int color) => (data & -225) | ((color & 7) << 5);
    }
}