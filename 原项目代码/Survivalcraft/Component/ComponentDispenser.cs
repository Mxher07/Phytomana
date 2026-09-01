using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentDispenser : ComponentInventoryBase {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemProjectiles m_subsystemProjectiles;

        public ComponentBlockEntity m_componentBlockEntity;

        public virtual void Dispense() {
            int data = Terrain.ExtractData(m_componentBlockEntity.BlockValue);
            int direction = DispenserBlock.GetDirection(data);
            DispenserBlock.Mode mode = DispenserBlock.GetMode(data);
            int num = 0;
            int slotValue;
            while (true) {
                if (num < SlotsCount) {
                    slotValue = GetSlotValue(num);
                    int slotCount = GetSlotCount(num);
                    if (slotValue != 0
                        && slotCount > 0
                        && BlocksManager.Blocks[Terrain.ExtractContents(slotValue)].CanBeFiredByDispenser(slotValue)) {
                        break;
                    }
                    num++;
                    continue;
                }
                return;
            }
            ModsManager.HookAction(
                "DispenserChooseItemToDispense",
                loader => {
                    loader.DispenserChooseItemToDispense(this, ref num, ref slotValue, out bool chosen);
                    return chosen;
                }
            );
            if (num >= 0) //投掷的Slot
            {
                int num2 = 1;
                for (int i = 0; i < num2 && GetSlotCount(num) > 0; i++) {
                    int itemDispense = DispenseItem(m_componentBlockEntity.Position, direction, slotValue, mode);
                    try {
                        RemoveSlotItems(num, itemDispense);
                    }
                    catch (Exception e) {
                        Log.Error(e.ToString());
                    }
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(true);
            m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>(true);
            m_componentBlockEntity.m_inventoryToGatherPickable = this;
        }

        public virtual int DispenseItem(Vector3 point, int face, int value, DispenserBlock.Mode mode) {
            Vector3 vector = CellFace.FaceToVector3(face);
            Vector3 position = new Vector3(point.X + 0.5f, point.Y + 0.5f, point.Z + 0.5f) + 0.6f * vector;
            int removeSlotCount = 1;
            //投掷物品
            if (mode == DispenserBlock.Mode.Dispense) {
                float s = 1.8f;
                Pickable pickable = m_subsystemPickables.CreatePickable(value, 1, position, s * (vector + m_random.Vector3(0.2f)), null, Entity);
                ModsManager.HookAction(
                    "OnDispenserDispensePickable",
                    loader => {
                        loader.OnDispenserDispensePickable(this, ref pickable, ref removeSlotCount);
                        return false;
                    }
                );
                if (pickable != null) {
                    m_subsystemPickables.AddPickable(pickable);
                    m_subsystemAudio.PlaySound("Audio/DispenserDispense", 1f, 0f, new Vector3(position.X, position.Y, position.Z), 3f, true);
                    return removeSlotCount;
                }
                return 0;
            }
            //发射物品
            float s2 = m_random.Float(39f, 41f);
            bool canFireProjectile = m_subsystemProjectiles.CanFireProjectile(value, position, vector, null, out Vector3 position2);
            bool canDispensePickable = true;
            Projectile projectile = m_subsystemProjectiles.CreateProjectile(
                value,
                position2,
                s2 * (vector + m_random.Vector3(0.025f) + new Vector3(0f, 0.05f, 0f)),
                Vector3.Zero,
                null
            );
            projectile.Creator = this;
            projectile.OwnerEntity = Entity;
            ModsManager.HookAction(
                "OnDispenserShoot",
                loader => {
                    loader.OnDispenserShoot(this, ref projectile, ref canDispensePickable, ref removeSlotCount);
                    return false;
                }
            );
            if (canFireProjectile) {
                if (projectile != null) {
                    m_subsystemProjectiles.FireProjectileFast(projectile);
                    m_subsystemAudio.PlaySound("Audio/DispenserShoot", 1f, 0f, new Vector3(position.X, position.Y, position.Z), 4f, true);
                    return removeSlotCount;
                }
                return 0;
            }
            if (canDispensePickable) {
                return DispenseItem(point, face, value, DispenserBlock.Mode.Dispense);
            }
            return 0;
        }
    }
}