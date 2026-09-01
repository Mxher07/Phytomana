using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemEntityBlockBehavior : SubsystemBlockBehavior {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemBlockEntities m_subsystemBlockEntities;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemAudio m_subsystemAudio;

        public DatabaseObject m_databaseObject;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            if(m_subsystemBlockEntities.GetBlockEntity(x, y, z) != null) {
                Log.Error("A blockEntity already exists. The new entity does not add.");
                return;
            }
            ValuesDictionary valuesDictionary = new();
            valuesDictionary.PopulateFromDatabaseObject(m_databaseObject);
            valuesDictionary.GetValue<ValuesDictionary>("BlockEntity").SetValue("Coordinates", new Point3(x, y, z));
            Entity entity = Project.CreateEntity(valuesDictionary);
            Project.AddEntity(entity);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(x, y, z);
            if (blockEntity != null) {
                Vector3 position = new Vector3(x, y, z) + new Vector3(0.5f);
                foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>()) {
                    item.DropAllItems(position);
                }
                Project.RemoveEntity(blockEntity.Entity, true);
            }
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(
                raycastResult.CellFace.X,
                raycastResult.CellFace.Y,
                raycastResult.CellFace.Z
            );
            bool isInitialNull = blockEntity == null;
            ModsManager.HookAction(
                "OnEntityBehaviorBlockInteracted",
                loader => {
                    loader.OnEntityBehaviorBlockInteracted(raycastResult, isInitialNull, ref blockEntity);
                    return false;
                }
            );
            return InteractBlockEntity(blockEntity, componentMiner);
        }

        public override bool OnInteract(MovingBlocksRaycastResult movingBlocksRaycastResult, ComponentMiner componentMiner) {
            ComponentBlockEntity componentBlockEntity = m_subsystemBlockEntities.GetBlockEntity(movingBlocksRaycastResult.MovingBlock);
            bool isInitialNull = componentBlockEntity == null;
            ModsManager.HookAction(
                "OnEntityBehaviorBlockInteracted",
                loader => {
                    loader.OnEntityBehaviorBlockInteracted(movingBlocksRaycastResult, isInitialNull, ref componentBlockEntity);
                    return false;
                }
            );
            return InteractBlockEntity(componentBlockEntity, componentMiner);
        }

        public virtual bool InteractBlockEntity(ComponentBlockEntity blockEntity, ComponentMiner componentMiner) => false;

        public override void OnBlockStartMoving(int value, int newValue, int x, int y, int z, MovingBlock movingBlock) {
            ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(x, y, z);
            if (blockEntity != null) {
                m_subsystemBlockEntities.m_blockEntities.Remove(blockEntity.Coordinates);
                m_subsystemBlockEntities.m_movingBlockEntities.Add(movingBlock, blockEntity);
                blockEntity.MovingBlock = movingBlock;
            }
        }

        public override void OnBlockStopMoving(int value, int oldValue, int x, int y, int z, MovingBlock movingBlock) {
            ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(movingBlock);
            if (blockEntity != null) {
                m_subsystemBlockEntities.m_movingBlockEntities.Remove(movingBlock);
                m_subsystemBlockEntities.m_blockEntities.Add(new Point3(x, y, z), blockEntity);
                blockEntity.MovingBlock = null;
                blockEntity.Coordinates = new Point3(x, y, z);
            }
        }

        public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem) {
            if (worldItem.ToRemove) {
                return;
            }
            ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(cellFace.X, cellFace.Y, cellFace.Z);
            blockEntity?.GatherPickable(worldItem);
        }

        public override void OnHitByProjectile(MovingBlock movingBlock, WorldItem worldItem) {
            if (worldItem.ToRemove) {
                return;
            }
            ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(movingBlock);
            blockEntity?.GatherPickable(worldItem);
        }
    }
}