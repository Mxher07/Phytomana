using Engine;
using Engine.Graphics;

namespace Game {
    public class MusketBlock : Block {
        public enum LoadState {
            Empty = 0,
            Gunpowder = 1,
            Wad = 2,
            Loaded = 3
        }

        public static int Index = 212;

        public BlockMesh m_standaloneBlockMeshUnloaded;

        public BlockMesh m_standaloneBlockMeshLoaded;

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Musket");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Musket").ParentBone);
            Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Hammer").ParentBone);
            m_standaloneBlockMeshUnloaded = new BlockMesh();
            m_standaloneBlockMeshUnloaded.AppendModelMeshPart(
                model.FindMesh("Musket").MeshParts[0],
                boneAbsoluteTransform,
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneBlockMeshUnloaded.AppendModelMeshPart(
                model.FindMesh("Hammer").MeshParts[0],
                boneAbsoluteTransform2,
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneBlockMeshLoaded = new BlockMesh();
            m_standaloneBlockMeshLoaded.AppendModelMeshPart(
                model.FindMesh("Musket").MeshParts[0],
                boneAbsoluteTransform,
                false,
                false,
                false,
                false,
                Color.White
            );
            m_standaloneBlockMeshLoaded.AppendModelMeshPart(
                model.FindMesh("Hammer").MeshParts[0],
                Matrix.CreateRotationX(0.7f) * boneAbsoluteTransform2,
                false,
                false,
                false,
                false,
                Color.White
            );
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            Texture2D texture = GetDefaultTexture(value);
            if (GetHammerState(Terrain.ExtractData(value))) {
                if (texture == null) {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshLoaded, color, 2f * size, ref matrix, environmentData);
                } else {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshLoaded, texture, color, 2f * size, ref matrix, environmentData);
                }
            }
            else {
                if (texture == null) {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshUnloaded, color, 2f * size, ref matrix, environmentData);
                } else {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshUnloaded, texture, color, 2f * size, ref matrix, environmentData);
                }
            }
        }

        public override bool IsSwapAnimationNeeded(int oldValue, int newValue) {
            if (Terrain.ExtractContents(oldValue) != BlockIndex) {
                return true;
            }
            int data = Terrain.ExtractData(oldValue);
            return SetHammerState(Terrain.ExtractData(newValue), true) != SetHammerState(data, true);
        }

        public override int GetDamage(int value) => (Terrain.ExtractData(value) >> 8) & 0xFF;

        public override int SetDamage(int value, int damage) {
            int num = Terrain.ExtractData(value);
            num &= -65281;
            num |= Math.Clamp(damage, 0, 255) << 8;
            return Terrain.ReplaceData(value, num);
        }

        public static LoadState GetLoadState(int data) => (LoadState)(data & 3);

        public static int SetLoadState(int data, LoadState loadState) => (data & -4) | ((int)loadState & 3);

        public static bool GetHammerState(int data) => (data & 4) != 0;

        public static int SetHammerState(int data, bool state) => (data & -5) | ((state ? 1 : 0) << 2);

        public static BulletBlock.BulletType? GetBulletType(int data) {
            int num = (data >> 4) & 0xF;
            if (num != 0) {
                return (BulletBlock.BulletType)(num - 1);
            }
            return null;
        }

        public static int SetBulletType(int data, BulletBlock.BulletType? bulletType) {
            int num = (int)(bulletType.HasValue ? bulletType.Value + 1 : BulletBlock.BulletType.MusketBall);
            return (data & -241) | ((num & 0xF) << 4);
        }
    }
}