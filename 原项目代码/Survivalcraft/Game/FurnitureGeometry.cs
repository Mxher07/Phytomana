using Engine.Graphics;

namespace Game {
    public class FurnitureGeometry {
        public BlockMesh[] SubsetOpaqueByFace = new BlockMesh[6];

        public BlockMesh[] SubsetAlphaTestByFace = new BlockMesh[6];

        public BlockMesh[] SubsetTransparentByFace = new BlockMesh[6];

        /// <summary>
        /// 非 null 纹理的几何数据，按纹理分组。
        /// 嵌套 FurnitureGeometry 的 Draws 字段不会被使用（仅用于分组存储 mesh）。
        /// 延迟初始化，无自定义纹理时为 null。
        /// </summary>
        public Dictionary<Texture2D, FurnitureGeometry> Draws;
    }
}