using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemBottomSuckerBlockBehavior : SubsystemInWaterBlockBehavior {
        public override int[] HandledBlocks => [];

        public int m_seaUrchinBlockValue;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_seaUrchinBlockValue = BlocksManager.GetBlockIndex<SeaUrchinBlock>();
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int bottomSuckerBlockValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            base.OnNeighborBlockChanged(x, y, z, neighborX, neighborY, neighborZ);
            int face = BottomSuckerBlock.GetFace(Terrain.ExtractData(bottomSuckerBlockValue));
            Point3 point = CellFace.FaceToPoint3(CellFace.OppositeFace(face));
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
            if (!IsSupport(cellValue, face, bottomSuckerBlockValue)) {
                SubsystemTerrain.DestroyCell(
                    0,
                    x,
                    y,
                    z,
                    0,
                    false,
                    false
                );
            }
        }

        public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody) {
            if (Terrain.ExtractContents(SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z)) == m_seaUrchinBlockValue) {
                ComponentHealth componentHealth = componentBody.Entity.FindComponent<ComponentHealth>();
                if (componentHealth != null) {
                    componentHealth.OnSpiked(
                        this,
                        0.1f / componentHealth.SpikeResilience * MathF.Abs(velocity),
                        cellFace,
                        velocity,
                        componentBody,
                        LanguageControl.Get("OtherCauseOfDeath", "SeaCreature")
                    );
                }
            }
        }

        public virtual bool IsSupport(int value, int face, int bottomSuckerBlockValue = 226) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            if (block.IsCollidable_(value)) {
                return !block.IsFaceNonAttachable(SubsystemTerrain, CellFace.OppositeFace(face), value, bottomSuckerBlockValue);
            }
            return false;
        }
    }
}