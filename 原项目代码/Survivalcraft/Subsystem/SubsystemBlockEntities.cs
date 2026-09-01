using Engine;
using GameEntitySystem;

namespace Game {
    public class SubsystemBlockEntities : Subsystem {
        public Dictionary<Point3, ComponentBlockEntity> m_blockEntities = [];

        public Dictionary<MovingBlock, ComponentBlockEntity> m_movingBlockEntities = [];

        public ComponentBlockEntity GetBlockEntity(int x, int y, int z) {
            m_blockEntities.TryGetValue(new Point3(x, y, z), out ComponentBlockEntity value);
            return value;
        }

        public ComponentBlockEntity GetBlockEntity(Point3 coordinates) {
            m_blockEntities.TryGetValue(coordinates, out ComponentBlockEntity value);
            return value;
        }

        public ComponentBlockEntity GetBlockEntity(MovingBlock movingBlock) {
            m_movingBlockEntities.TryGetValue(movingBlock, out ComponentBlockEntity value);
            return value;
        }

        public override void OnEntityAdded(Entity entity) {
            ComponentBlockEntity componentBlockEntity = entity.FindComponent<ComponentBlockEntity>();
            if (componentBlockEntity != null) {
                if (!MovingBlock.IsNullOrStopped(componentBlockEntity.MovingBlock)) {
                    m_movingBlockEntities.Add(componentBlockEntity.MovingBlock, componentBlockEntity);
                }
                else if (componentBlockEntity.Coordinates.Y >= 0) {
                    m_blockEntities.Add(componentBlockEntity.Coordinates, componentBlockEntity);
                }
            }
        }

        public override void OnEntityRemoved(Entity entity) {
            ComponentBlockEntity componentBlockEntity = entity.FindComponent<ComponentBlockEntity>();
            if (componentBlockEntity != null) {
                m_blockEntities.Remove(componentBlockEntity.Coordinates);
                if (componentBlockEntity.MovingBlock != null) {
                    m_movingBlockEntities.Remove(componentBlockEntity.MovingBlock);
                }
            }
        }
    }
}