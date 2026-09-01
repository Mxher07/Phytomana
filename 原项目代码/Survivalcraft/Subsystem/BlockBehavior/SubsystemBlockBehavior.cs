using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public abstract class SubsystemBlockBehavior : Subsystem {
        public virtual int[] HandledBlocks => [];

        public SubsystemTerrain SubsystemTerrain { get; set; }

        public virtual void OnChunkInitialized(TerrainChunk chunk) { }

        public virtual void OnChunkDiscarding(TerrainChunk chunk) { }

        public virtual void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) { }

        public virtual void OnBlockAdded(int value, int oldValue, int x, int y, int z) { }

        public virtual void OnBlockRemoved(int value, int newValue, int x, int y, int z) { }

        public virtual void OnBlockModified(int value, int oldValue, int x, int y, int z) { }

        public virtual void OnBlockStartMoving(int value, int newValue, int x, int y, int z, MovingBlock movingBlock) {
            OnBlockRemoved(value, newValue, x, y, z);
        }

        public virtual void OnBlockStopMoving(int value, int oldValue, int x, int y, int z, MovingBlock movingBlock) {
            OnBlockAdded(value, oldValue, x, y, z);
        }

        public virtual void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) { }

        public virtual bool OnUse(Ray3 ray, ComponentMiner componentMiner) => false;

        public virtual bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) => false;

        public virtual bool OnInteract(MovingBlocksRaycastResult movingBlocksRaycastResult, ComponentMiner componentMiner) => false;

        public virtual bool OnAim(Ray3 aim, ComponentMiner componentMiner, AimState state) => false;

        public virtual bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer) => false;

        public virtual bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer) => false;

        public virtual void OnItemPlaced(int x, int y, int z, ref BlockPlacementData placementData, int itemValue) { }

        public virtual void OnItemHarvested(int x, int y, int z, int blockValue, ref BlockDropValue dropValue, ref int newBlockValue) { }

        public virtual void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody) { }

        public virtual void OnExplosion(int value, int x, int y, int z, float damage) { }

        public virtual void OnFiredAsProjectile(Projectile projectile) { }

        public virtual bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem) => false;

        public virtual void OnHitByProjectile(CellFace cellFace, WorldItem worldItem) { }
        public virtual void OnHitByProjectile(MovingBlock movingBlock, WorldItem worldItem) { }

        public virtual int GetProcessInventoryItemCapacity(IInventory inventory, int slotIndex, int value) => 0;

        public virtual void ProcessInventoryItem(IInventory inventory,
            int slotIndex,
            int value,
            int count,
            int processCount,
            out int processedValue,
            out int processedCount) {
            throw new InvalidOperationException("Cannot process items.");
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
        }

        public virtual void OnPickableGathered(Pickable pickable, ComponentPickableGatherer target, Vector3 distanceToTarget) { }
    }
}