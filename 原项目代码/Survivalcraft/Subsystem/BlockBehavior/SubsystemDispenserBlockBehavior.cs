using TemplatesDatabase;

namespace Game {
    public class SubsystemDispenserBlockBehavior : SubsystemEntityBlockBehavior {
        public override int[] HandledBlocks => [216];

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_databaseObject = Project.GameDatabase.Database.FindDatabaseObject("Dispenser", Project.GameDatabase.EntityTemplateType, true);
        }

        public override bool InteractBlockEntity(ComponentBlockEntity blockEntity, ComponentMiner componentMiner) {
            if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Adventure) {
                if (blockEntity != null
                    && componentMiner.ComponentPlayer != null) {
                    ComponentDispenser componentDispenser = blockEntity.Entity.FindComponent<ComponentDispenser>(true);
                    componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new DispenserWidget(componentMiner.Inventory, componentDispenser);
                    AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
                    return true;
                }
            }
            return false;
        }

        public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem) {
            bool acceptDrops = DispenserBlock.GetAcceptsDrops(
                Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z))
            );
            if (acceptDrops) {
                base.OnHitByProjectile(cellFace, worldItem);
            }
        }

        public override void OnHitByProjectile(MovingBlock movingBlock, WorldItem worldItem) {
            bool acceptDrops = DispenserBlock.GetAcceptsDrops(Terrain.ExtractData(movingBlock.Value));
            if (acceptDrops) {
                base.OnHitByProjectile(movingBlock, worldItem);
            }
        }
    }
}