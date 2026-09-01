using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemCollapsingBlockBehavior : SubsystemBlockBehavior {
        public const string IdString = "CollapsingBlock";

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemSoundMaterials m_subsystemSoundMaterials;

        public SubsystemMovingBlocks m_subsystemMovingBlocks;

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living) {
                TryCollapseColumn(new Point3(x, y, z));
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(true);
            m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(true);
            m_subsystemMovingBlocks.Stopped += MovingBlocksStopped;
            m_subsystemMovingBlocks.CollidedWithTerrain += MovingBlocksCollidedWithTerrain;
        }

        public void MovingBlocksCollidedWithTerrain(IMovingBlockSet movingBlockSet, Point3 p) {
            if (movingBlockSet.Id == "CollapsingBlock") {
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z);
                if (IsCollapseSupportBlock(cellValue)) {
                    movingBlockSet.Stop();
                }
                else if (IsCollapseDestructibleBlock(cellValue)) {
                    SubsystemTerrain.DestroyCell(
                        0,
                        p.X,
                        p.Y,
                        p.Z,
                        0,
                        false,
                        false
                    );
                }
            }
        }

        public void MovingBlocksStopped(IMovingBlockSet movingBlockSet) {
            if (movingBlockSet.Id == "CollapsingBlock") {
                Point3 p = Terrain.ToCell(
                    MathF.Round(movingBlockSet.Position.X),
                    MathF.Round(movingBlockSet.Position.Y),
                    MathF.Round(movingBlockSet.Position.Z)
                );
                foreach (MovingBlock block in movingBlockSet.Blocks) {
                    Point3 point = p + block.Offset;
                    m_subsystemMovingBlocks.AddTerrainBlock(point.X, point.Y, point.Z, block.Value, block);
                }
                m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet);
                if (movingBlockSet.Blocks.Count > 0) {
                    m_subsystemSoundMaterials.PlayImpactSound(movingBlockSet.Blocks[0].Value, movingBlockSet.Position, 1f);
                }
            }
        }

        public void TryCollapseColumn(Point3 p) {
            if (p.Y <= 0) {
                return;
            }
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(p.X, p.Y - 1, p.Z);
            if (IsCollapseSupportBlock(cellValue)) {
                return;
            }
            List<MovingBlock> list = new();
            for (int i = p.Y; i < 256; i++) {
                int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(p.X, i, p.Z);
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)];
                if (!block.GetIsCollapsable(cellValue2)) {
                    break;
                }
                list.Add(new MovingBlock { Value = cellValue2, Offset = new Point3(0, i - p.Y, 0) });
            }
            if (list.Count != 0) {
                IMovingBlockSet movingBlockSet = m_subsystemMovingBlocks.AddMovingBlockSet(
                    new Vector3(p),
                    new Vector3(p.X, -list.Count - 1, p.Z),
                    0f,
                    10f,
                    0.7f,
                    new Vector2(0f),
                    list,
                    "CollapsingBlock",
                    null,
                    true
                );
                if (movingBlockSet != null) {
                    foreach (MovingBlock item in list) {
                        Point3 point = p + item.Offset;
                        SubsystemTerrain.ChangeCell(point.X, point.Y, point.Z, 0, true, item);
                    }
                }
            }
        }

        public bool IsCollapseSupportBlock(int value) {
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            return block.IsCollapseSupportBlock(SubsystemTerrain, value);
        }

        public static bool IsCollapseDestructibleBlock(int value) {
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            return block.IsCollapseDestructibleBlock(value);
        }
    }
}