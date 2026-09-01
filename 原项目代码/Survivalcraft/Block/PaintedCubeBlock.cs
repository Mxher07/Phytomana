using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class PaintedCubeBlock : CubeBlock, IPaintableBlock {
        public int m_coloredTextureSlot;

        public PaintedCubeBlock(int coloredTextureSlot) => m_coloredTextureSlot = coloredTextureSlot;

        public override int GetFaceTextureSlot(int face, int value) {
            if (!IsColored(Terrain.ExtractData(value))) {
                return DefaultTextureSlot;
            }
            return m_coloredTextureSlot;
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            int data = Terrain.ExtractData(value);
            Color color = SubsystemPalette.GetColor(generator, GetColor(data));
            generator.GenerateCubeVertices(
                this,
                value,
                x,
                y,
                z,
                color,
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
            color *= SubsystemPalette.GetColor(environmentData, GetColor(data));
            Texture2D texture = GetDefaultTexture(value);
            if (texture == null) {
                BlocksManager.DrawCubeBlock(
                    primitivesRenderer,
                    value,
                    new Vector3(size),
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
                    new Vector3(size),
                    ref matrix,
                    color,
                    color,
                    environmentData,
                    texture
                );
            }
        }

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, null));
            int i = 0;
            while (i < 16) {
                yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, i));
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
            int data = Terrain.ExtractData(oldValue);
            if (GetColor(data).HasValue) {
                showDebris = true;
                if (toolLevel >= RequiredToolLevel) {
                    dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(DefaultDropContent, 0, data), Count = (int)DefaultDropCount });
                }
            }
            else {
                base.GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
            }
        }

        public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) {
            int data = Terrain.ExtractData(value);
            Color color = SubsystemPalette.GetColor(subsystemTerrain, GetColor(data));
            return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, GetFaceTextureSlot(0, value));
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            int data = Terrain.ExtractData(value);
            return SubsystemPalette.GetName(
                subsystemTerrain,
                GetColor(data),
                LanguageControl.GetBlock($"{GetType().Name}:{data.ToString()}", "DisplayName")
            );
        }

        public override string GetCategory(int value) {
            if (!GetColor(Terrain.ExtractData(value)).HasValue) {
                return base.GetCategory(value);
            }
            return "Painted";
        }

        public int? GetPaintColor(int value) => GetColor(Terrain.ExtractData(value));

        public int Paint(SubsystemTerrain terrain, int value, int? color) {
            int data = Terrain.ExtractData(value);
            return Terrain.ReplaceData(value, SetColor(data, color));
        }

        public static bool IsColored(int data) => (data & 1) != 0;

        public static int? GetColor(int data) {
            if ((data & 1) != 0) {
                return (data >> 1) & 0xF;
            }
            return null;
        }

        public static int SetColor(int data, int? color) {
            if (color.HasValue) {
                return (data & -32) | 1 | (color.Value << 1);
            }
            return data & -32;
        }
    }
}