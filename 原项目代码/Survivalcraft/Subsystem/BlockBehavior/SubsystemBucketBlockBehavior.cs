using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemBucketBlockBehavior : SubsystemBlockBehavior {
        public SubsystemAudio m_subsystemAudio;

        public SubsystemParticles m_subsystemParticles;

        public Random m_random = new();

        public static string fName = "SubsystemBucketBlockBehavior";

        public int m_emptyBucketBlockIndex;
        public int m_waterBucketBlockIndex;
        public int m_magmaBucketBlockIndex;
        public int m_milkBucketBlockIndex;
        public int m_waterBlockIndex;
        public int m_magmaBlockIndex;

        public override int[] HandledBlocks => [
            BlocksManager.GetBlockIndex<EmptyBucketBlock>(),
            BlocksManager.GetBlockIndex<WaterBucketBlock>(),
            BlocksManager.GetBlockIndex<MagmaBucketBlock>(),
            BlocksManager.GetBlockIndex<MilkBucketBlock>(),
            245,
            251,
            252,
            129,
            128
        ];

        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) {
            IInventory inventory = componentMiner.Inventory;
            int activeBlockValue = componentMiner.ActiveBlockValue;
            int num = Terrain.ExtractContents(activeBlockValue);
            if (num == m_emptyBucketBlockIndex) {
                object obj = componentMiner.Raycast(ray, RaycastMode.Gathering);
                if (obj is TerrainRaycastResult) {
                    CellFace cellFace = ((TerrainRaycastResult)obj).CellFace;
                    int cellValue = SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
                    int num2 = Terrain.ExtractContents(cellValue);
                    int data = Terrain.ExtractData(cellValue);
                    Block block = BlocksManager.Blocks[num2];
                    int newBucketValue = 0;
                    if (block is WaterBlock
                        && FluidBlock.GetLevel(data) == 0) {
                        newBucketValue = m_waterBucketBlockIndex;
                    }
                    if (block is MagmaBlock
                        && FluidBlock.GetLevel(data) == 0) {
                        newBucketValue = m_magmaBucketBlockIndex;
                    }
                    if (newBucketValue == 0) {
                        return false;
                    }
                    int currentCount = inventory.GetSlotCount(inventory.ActiveSlotIndex);
                    if (currentCount > 1) {
                        inventory.RemoveSlotItems(inventory.ActiveSlotIndex, 1);
                        int acquireSlot = ComponentInventoryBase.FindAcquireSlotForItem(inventory, newBucketValue);
                        if (acquireSlot >= 0) {
                            inventory.AddSlotItems(acquireSlot, newBucketValue, 1);
                            SubsystemTerrain.DestroyCell(
                                0,
                                cellFace.X,
                                cellFace.Y,
                                cellFace.Z,
                                0,
                                false,
                                false
                            );
                            return true;
                        }
                        inventory.AddSlotItems(inventory.ActiveSlotIndex, activeBlockValue, 1);
                        componentMiner?.ComponentPlayer?.ComponentGui?.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, true, true);
                    }
                    else {
                        inventory.RemoveSlotItems(inventory.ActiveSlotIndex, currentCount);
                        if (inventory.GetSlotCount(inventory.ActiveSlotIndex) == 0) {
                            inventory.AddSlotItems(inventory.ActiveSlotIndex, newBucketValue, 1);
                        }
                        SubsystemTerrain.DestroyCell(
                            0,
                            cellFace.X,
                            cellFace.Y,
                            cellFace.Z,
                            0,
                            false,
                            false
                        );
                        return true;
                    }
                }
                else if (obj is BodyRaycastResult) {
                    ComponentUdder componentUdder = ((BodyRaycastResult)obj).ComponentBody.Entity.FindComponent<ComponentUdder>();
                    int newBucketValue = m_milkBucketBlockIndex;
                    int currentCount = inventory.GetSlotCount(inventory.ActiveSlotIndex);
                    if (currentCount > 1) {
                        inventory.RemoveSlotItems(inventory.ActiveSlotIndex, 1);
                        int acquireSlot = ComponentInventoryBase.FindAcquireSlotForItem(inventory, newBucketValue);
                        if (acquireSlot < 0) {
                            componentMiner?.ComponentPlayer?.ComponentGui?.DisplaySmallMessage(
                                LanguageControl.Get(fName, 2),
                                Color.White,
                                true,
                                true
                            );
                        }
                        if (acquireSlot >= 0
                            && componentUdder != null
                            && componentUdder.Milk(componentMiner)) {
                            inventory.AddSlotItems(acquireSlot, newBucketValue, 1);
                            m_subsystemAudio.PlaySound("Audio/Milked", 1f, 0f, ray.Position, 2f, true);
                            return true;
                        }
                        inventory.AddSlotItems(inventory.ActiveSlotIndex, activeBlockValue, 1);
                        return false;
                    }
                    if (componentUdder != null
                        && componentUdder.Milk(componentMiner)) {
                        inventory.RemoveSlotItems(inventory.ActiveSlotIndex, currentCount);
                        if (inventory.GetSlotCount(inventory.ActiveSlotIndex) == 0) {
                            inventory.AddSlotItems(inventory.ActiveSlotIndex, m_milkBucketBlockIndex, 1);
                        }
                        m_subsystemAudio.PlaySound("Audio/Milked", 1f, 0f, ray.Position, 2f, true);
                        return true;
                    }
                    return false;
                }
            }
            if (num == m_waterBucketBlockIndex
                || num == m_magmaBucketBlockIndex) {
                int fluidValue = num == m_waterBucketBlockIndex ? m_waterBlockIndex : m_magmaBlockIndex;
                TerrainRaycastResult? terrainRaycastResult = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
                if (terrainRaycastResult.HasValue) {
                    int currentCount = inventory.GetSlotCount(inventory.ActiveSlotIndex);
                    if (currentCount > 1) {
                        inventory.RemoveSlotItems(inventory.ActiveSlotIndex, 1);
                        int newBucketValue = m_emptyBucketBlockIndex;
                        int acquireSlot = ComponentInventoryBase.FindAcquireSlotForItem(inventory, newBucketValue);
                        if (acquireSlot >= 0
                            && componentMiner.Place(terrainRaycastResult.Value, Terrain.MakeBlockValue(fluidValue))) {
                            inventory.AddSlotItems(acquireSlot, newBucketValue, 1);
                        }
                        else {
                            inventory.AddSlotItems(inventory.ActiveSlotIndex, activeBlockValue, 1);
                            componentMiner?.ComponentPlayer?.ComponentGui?.DisplaySmallMessage(
                                LanguageControl.Get(fName, 3),
                                Color.White,
                                true,
                                true
                            );
                            return false;
                        }
                        return true;
                    }
                    if (componentMiner.Place(terrainRaycastResult.Value, Terrain.MakeBlockValue(fluidValue))) {
                        inventory.RemoveSlotItems(inventory.ActiveSlotIndex, 1);
                        if (inventory.GetSlotCount(inventory.ActiveSlotIndex) == 0) {
                            inventory.AddSlotItems(inventory.ActiveSlotIndex, m_emptyBucketBlockIndex, 1);
                        }
                        return true;
                    }
                }
            }
            switch (num) {
                case 110:
                case 245: return true;
                case 251:
                case 252: return true;
                case 128:
                case 129: {
                    TerrainRaycastResult? terrainRaycastResult3 = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Digging);
                    if (terrainRaycastResult3.HasValue) {
                        CellFace cellFace2 = terrainRaycastResult3.Value.CellFace;
                        int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(cellFace2.X, cellFace2.Y, cellFace2.Z);
                        int num3 = Terrain.ExtractContents(cellValue2);
                        Block block2 = BlocksManager.Blocks[num3];
                        if (block2 is IPaintableBlock) {
                            Vector3 normal = CellFace.FaceToVector3(terrainRaycastResult3.Value.CellFace.Face);
                            Vector3 position = terrainRaycastResult3.Value.HitPoint();
                            int? num4 = num == 128 ? null : new int?(PaintBucketBlock.GetColor(Terrain.ExtractData(activeBlockValue)));
                            Color color = num4.HasValue ? SubsystemPalette.GetColor(SubsystemTerrain, num4) : new Color(128, 128, 128, 128);
                            int value6 = ((IPaintableBlock)block2).Paint(SubsystemTerrain, cellValue2, num4);
                            if (value6 != cellValue2) {
                                SubsystemTerrain.ChangeCell(cellFace2.X, cellFace2.Y, cellFace2.Z, value6);
                                componentMiner.DamageActiveTool(1);
                                m_subsystemAudio.PlayRandomSound(
                                    "Audio/Paint",
                                    0.4f,
                                    m_random.Float(-0.1f, 0.1f),
                                    componentMiner.ComponentCreature.ComponentBody.Position,
                                    2f,
                                    true
                                );
                                m_subsystemParticles.AddParticleSystem(new PaintParticleSystem(SubsystemTerrain, position, normal, color));
                            }
                        }
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_waterBlockIndex = BlocksManager.GetBlockIndex<WaterBlock>();
            m_magmaBlockIndex = BlocksManager.GetBlockIndex<MagmaBlock>();
            m_waterBucketBlockIndex = BlocksManager.GetBlockIndex<WaterBucketBlock>();
            m_emptyBucketBlockIndex = BlocksManager.GetBlockIndex<EmptyBucketBlock>();
            m_magmaBucketBlockIndex = BlocksManager.GetBlockIndex<MagmaBucketBlock>();
            m_milkBucketBlockIndex = BlocksManager.GetBlockIndex<MilkBucketBlock>();
        }
    }
}