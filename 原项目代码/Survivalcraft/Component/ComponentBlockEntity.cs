using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentBlockEntity : Component {
        public SubsystemMovingBlocks m_subsystemMovingBlocks;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemAudio m_subsystemAudio;
        public MovingBlock MovingBlock { get; set; }

        public IInventory m_inventoryToGatherPickable;

        public int m_blockValue;

        public virtual int BlockValue {
            get {
                if (!MovingBlock.IsNullOrStopped(MovingBlock)) {
                    m_blockValue = MovingBlock.Value;
                    return m_blockValue;
                }
                TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Coordinates.X, Coordinates.Z);
                if (chunkAtCell != null
                    && chunkAtCell.State == TerrainChunkState.Valid) {
                    int value = m_subsystemTerrain.Terrain.GetCellValue(Coordinates.X, Coordinates.Y, Coordinates.Z);
                    if (value != 0) {
                        m_blockValue = value;
                    }
                }
                return m_blockValue;
            }
            set {
                m_blockValue = value;
                if (!MovingBlock.IsNullOrStopped(MovingBlock)) {
                    MovingBlock.Value = value;
                    return;
                }
                TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Coordinates.X, Coordinates.Z);
                if (chunkAtCell != null
                    && chunkAtCell.State == TerrainChunkState.Valid) {
                    m_subsystemTerrain.ChangeCell(Coordinates.X, Coordinates.Y, Coordinates.Z, value);
                }
            }
        }

        public virtual Vector3 Position {
            get {
                if (!MovingBlock.IsNullOrStopped(MovingBlock)) {
                    return MovingBlock.Position;
                }
                return new Vector3(Coordinates);
            }
        }

        public Point3 Coordinates { get; set; }

        public virtual void GatherPickable(WorldItem worldItem) {
            if (m_inventoryToGatherPickable == null) {
                return;
            }
            Pickable pickable = worldItem as Pickable;
            int num = pickable?.Count ?? 1;
            int num2 = ComponentInventoryBase.AcquireItems(m_inventoryToGatherPickable, worldItem.Value, num);
            if (num2 < num) {
                m_subsystemAudio.PlaySound("Audio/PickableCollected", 1f, 0f, worldItem.Position, 3f, true);
            }
            if (num2 <= 0) {
                worldItem.ToRemove = true;
            }
            else if (pickable != null) {
                pickable.Count = num2;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            Coordinates = valuesDictionary.GetValue<Point3>("Coordinates");
            MovingBlock = MovingBlock.LoadFromValuesDictionary(Project, valuesDictionary);
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("Coordinates", Coordinates);
            MovingBlock?.SetValuesDicionary(valuesDictionary);
        }
    }
}