using Engine.Core;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 独立贴图方块基类族（Cross/Cube/Flat）：把「每方块一张独立贴图」的渲染从旧插件的
    /// GetDefaultTexture 覆写迁移到新联机 API 的显式贴图接口 ——
    /// 地形顶点走 TerrainGeometry.GetGeometry(m_texture) 的独立几何通道，
    /// 独立即场景（手持/物品栏/掉落物）由各 Draw* 接口显式传入贴图。
    /// m_texture 为空时回退基类走原版图集路径，保证加载失败时视觉可降级。
    /// </summary>
    public abstract class PhytoCrossBlock : FlowerBlock {
        public Texture2D m_texture;

        /// <summary>独立贴图取用点：默认为 m_texture，按 data 变贴图的子类可覆写。</summary>
        public virtual Texture2D GetPhytoTexture(int value) => m_texture;

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetPhytoTexture(value);
            if (texture == null) {
                base.GenerateTerrainVertices(generator, geometry, value, x, y, z);
                return;
            }
            generator.GenerateCrossfaceVertices(
                this,
                value,
                x,
                y,
                z,
                Color.White,
                GetFaceTextureSlot(0, value),
                geometry.GetGeometry(texture).SubsetAlphaTest
            );
        }

        public override void DrawBlock(
            PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData
        ) {
            Texture2D texture = GetPhytoTexture(value);
            if (texture == null) {
                base.DrawBlock(primitivesRenderer, value, color, size, ref matrix, environmentData);
                return;
            }
            BlocksManager.DrawFlatOrImageExtrusionBlock(
                primitivesRenderer,
                value,
                size,
                ref matrix,
                texture,
                color,
                false,
                environmentData
            );
        }
    }

    public abstract class PhytoCubeBlock : CubeBlock {
        public Texture2D m_texture;

        /// <summary>独立贴图取用点：默认为 m_texture，按 data 变贴图的子类可覆写。</summary>
        public virtual Texture2D GetPhytoTexture(int value) => m_texture;

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetPhytoTexture(value);
            if (texture == null) {
                base.GenerateTerrainVertices(generator, geometry, value, x, y, z);
                return;
            }
            generator.GenerateCubeVertices(
                this,
                value,
                x,
                y,
                z,
                Color.White,
                geometry.GetGeometry(texture).OpaqueSubsetsByFace
            );
        }

        public override void DrawBlock(
            PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData
        ) {
            Texture2D texture = GetPhytoTexture(value);
            if (texture == null) {
                base.DrawBlock(primitivesRenderer, value, color, size, ref matrix, environmentData);
                return;
            }
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

    public abstract class PhytoFlatBlock : FlatBlock {
        public Texture2D m_texture;

        /// <summary>独立贴图取用点：默认为 m_texture，按 data 变贴图的子类可覆写。</summary>
        public virtual Texture2D GetPhytoTexture(int value) => m_texture;

        public override void DrawBlock(
            PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData
        ) {
            Texture2D texture = GetPhytoTexture(value);
            if (texture == null) {
                base.DrawBlock(primitivesRenderer, value, color, size, ref matrix, environmentData);
                return;
            }
            BlocksManager.DrawFlatOrImageExtrusionBlock(
                primitivesRenderer,
                value,
                size,
                ref matrix,
                texture,
                color,
                false,
                environmentData
            );
        }
    }
}
