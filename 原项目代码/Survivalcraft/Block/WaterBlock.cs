using Engine;
using Engine.Graphics;

namespace Game {
    public class WaterBlock : FluidBlock {
        public static int Index = 18;

        public new static int MaxLevel = 7;

        public WaterBlock() : base(MaxLevel) => CanBeBuiltIntoFurniture = true;

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            Color sideColor;
            Color color = sideColor = BlockColorsMap.Water.Lookup(generator.Terrain, x, y, z);
            sideColor.A = byte.MaxValue;
            Color topColor = color;
            topColor.A = 0;
            GenerateFluidTerrainVertices(
                generator,
                value,
                x,
                y,
                z,
                sideColor,
                topColor,
                texture == null ? geometry.TransparentSubsetsByFace : geometry.GetGeometry(texture).TransparentSubsetsByFace
            );
        }

        public override Color GetFurnitureColor(int value, SubsystemPalette subsystemPalette) => BlockColorsMap.Water.Lookup(12, 12);
    }
}