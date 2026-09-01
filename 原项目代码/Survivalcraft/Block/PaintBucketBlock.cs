using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game {
    public class PaintBucketBlock : BucketBlock {
        public static int Index = 129;

        public BlockMesh m_standaloneBucketBlockMesh = new();

        public BlockMesh m_standalonePaintBlockMesh = new();

        public const string fName = "PaintBucketBlock";

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/FullBucket");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Bucket").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Contents").ParentBone);
            m_standaloneBucketBlockMesh.AppendModelMeshPart(
                model.FindMesh("Bucket").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standalonePaintBlockMesh.AppendModelMeshPart(
                model.FindMesh("Contents").MeshParts[0],
                boneAbsoluteTransform2 * Matrix.CreateRotationY(MathUtils.DegToRad(180f)) * Matrix.CreateTranslation(0f, -0.3f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standalonePaintBlockMesh.TransformTextureCoordinates(Matrix.CreateTranslation(0.9375f, 0f, 0f));
            base.Initialize();
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            int color2 = GetColor(Terrain.ExtractData(value));
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBucketBlockMesh, color, 2f * size, ref matrix, environmentData);
            } else {
                BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBucketBlockMesh, texture, color, 2f * size, ref matrix, environmentData);
            }
            if (texture == null) {
                BlocksManager.DrawMeshBlock(
                    primitivesRenderer,
                    m_standalonePaintBlockMesh,
                    color * SubsystemPalette.GetColor(environmentData, color2),
                    2f * size,
                    ref matrix,
                    environmentData
                );
            } else {
                BlocksManager.DrawMeshBlock(
                    primitivesRenderer,
                    m_standalonePaintBlockMesh,
                    texture,
                    color * SubsystemPalette.GetColor(environmentData, color2),
                    2f * size,
                    ref matrix,
                    environmentData
                );
            }
        }

        public override IEnumerable<int> GetCreativeValues() {
            int i = 0;
            while (i < 16) {
                yield return Terrain.MakeBlockValue(129, 0, SetColor(0, i));
                int num = i + 1;
                i = num;
            }
        }

        public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes() {
            string[] additives = [
                BlocksManager.Blocks[43].CraftingId,
                BlocksManager.Blocks[24].CraftingId,
                BlocksManager.Blocks[103].CraftingId,
                BlocksManager.Blocks[22].CraftingId
            ];
            int color = 0;
            while (color < 16) {
                int num2;
                for (int additive = 0; additive < 4; additive = num2) {
                    int num = CombineColors(color, 1 << additive);
                    if (num != color) {
                        CraftingRecipe craftingRecipe = new() {
                            Description = string.Format(LanguageControl.Get(fName, "1"), SubsystemPalette.GetName(null, num, null)),
                            ResultValue = Terrain.MakeBlockValue(129, 0, num),
                            ResultCount = 1,
                            RequiredHeatLevel = 1f,
                            Ingredients = {
                                [0] = $"{BlocksManager.Blocks[129].CraftingId}:{color.ToString(CultureInfo.InvariantCulture)}",
                                [1] = additives[additive]
                            }
                        };
                        yield return craftingRecipe;
                    }
                    num2 = additive + 1;
                }
                num2 = color + 1;
                color = num2;
            }
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            int color = GetColor(Terrain.ExtractData(value));
            return SubsystemPalette.GetName(subsystemTerrain, color, LanguageControl.GetBlock("PaintBucketBlock", "DisplayName"));
        }

        public override int GetDamageDestructionValue(int value) => Terrain.MakeBlockValue(90);

        public static int GetColor(int data) => data & 0xF;

        public static int SetColor(int data, int color) => (data & -16) | (color & 0xF);

        public static Vector4 ColorToCmyk(int color) {
            int num = color & 1;
            int num2 = (color >> 1) & 1;
            int num3 = (color >> 2) & 1;
            int num4 = (color >> 3) & 1;
            return new Vector4(num, num2, num3, num4);
        }

        public static int CmykToColor(Vector4 cmyk) {
            if (cmyk.W <= 1f) {
                int num = (int)MathF.Round(MathUtils.Saturate(cmyk.X));
                int num2 = (int)MathF.Round(MathUtils.Saturate(cmyk.Y));
                int num3 = (int)MathF.Round(MathUtils.Saturate(cmyk.Z));
                int num4 = (int)MathF.Round(MathUtils.Saturate(cmyk.W));
                return num | (num2 << 1) | (num3 << 2) | (num4 << 3);
            }
            return 15;
        }

        public static int CombineColors(int color1, int color2) => CmykToColor(ColorToCmyk(color1) + ColorToCmyk(color2));
    }
}