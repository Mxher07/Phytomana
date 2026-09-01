using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemBowBlockBehavior : SubsystemBlockBehavior {
        public SubsystemTime m_subsystemTime;

        public SubsystemProjectiles m_subsystemProjectiles;

        public SubsystemAudio m_subsystemAudio;
        public static string fName = "SubsystemBowBlockBehavior";
        public Random m_random = new();

        public Dictionary<ComponentMiner, double> m_aimStartTimes = [];

        int m_bowBlockIndex;
        int m_arrowBlockIndex;

        public ArrowBlock.ArrowType[] m_supportedArrowTypes = [
            ArrowBlock.ArrowType.WoodenArrow,
            ArrowBlock.ArrowType.StoneArrow,
            ArrowBlock.ArrowType.CopperArrow,
            ArrowBlock.ArrowType.IronArrow,
            ArrowBlock.ArrowType.DiamondArrow,
            ArrowBlock.ArrowType.FireArrow
        ];

        public override int[] HandledBlocks => [];

        public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer) {
            componentPlayer.ComponentGui.ModalPanelWidget = componentPlayer.ComponentGui.ModalPanelWidget == null
                ? new BowWidget(inventory, slotIndex)
                : null;
            return true;
        }

        public override bool OnAim(Ray3 aim, ComponentMiner componentMiner, AimState state) {
            IInventory inventory = componentMiner.Inventory;
            if (inventory != null) {
                int activeSlotIndex = inventory.ActiveSlotIndex;
                if (activeSlotIndex >= 0) {
                    int slotValue = inventory.GetSlotValue(activeSlotIndex);
                    int slotCount = inventory.GetSlotCount(activeSlotIndex);
                    int num = Terrain.ExtractContents(slotValue);
                    int data = Terrain.ExtractData(slotValue);
                    if (slotCount > 0) {
                        if (!m_aimStartTimes.TryGetValue(componentMiner, out double value)) {
                            value = m_subsystemTime.GameTime;
                            m_aimStartTimes[componentMiner] = value;
                        }
                        float num2 = (float)(m_subsystemTime.GameTime - value);
                        float num3 = (float)MathUtils.Remainder(m_subsystemTime.GameTime, 1000.0);
                        Vector3 v =
                            ((componentMiner.ComponentCreature.ComponentBody.IsCrouching ? 0.02f : 0.04f)
                                + 0.25f * MathUtils.Saturate((num2 - 2.1f) / 5f))
                            * new Vector3 {
                                X = SimplexNoise.OctavedNoise(num3, 2f, 3, 2f, 0.5f),
                                Y = SimplexNoise.OctavedNoise(num3 + 100f, 2f, 3, 2f, 0.5f),
                                Z = SimplexNoise.OctavedNoise(num3 + 200f, 2f, 3, 2f, 0.5f)
                            };
                        aim.Direction = Vector3.Normalize(aim.Direction + v);
                        switch (state) {
                            case AimState.InProgress: {
                                if (num2 >= 9f) {
                                    componentMiner.ComponentCreature.ComponentCreatureSounds.PlayMoanSound();
                                    return true;
                                }
                                ComponentFirstPersonModel
                                    componentFirstPersonModel = componentMiner.Entity.FindComponent<ComponentFirstPersonModel>();
                                if (componentFirstPersonModel != null) {
                                    componentMiner.ComponentPlayer?.ComponentAimingSights.ShowAimingSights(aim.Position, aim.Direction);
                                    componentFirstPersonModel.ItemOffsetOrder = new Vector3(-0.1f, 0.15f, 0f);
                                    componentFirstPersonModel.ItemRotationOrder = new Vector3(0f, -0.7f, 0f);
                                }
                                componentMiner.ComponentCreature.ComponentCreatureModel.AimHandAngleOrder = 1.2f;
                                componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemOffsetOrder = new Vector3(0f, 0f, 0f);
                                componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemRotationOrder = new Vector3(0f, -0.2f, 0f);
                                if (m_subsystemTime.PeriodicGameTimeEvent(0.10000000149011612, 0.0)) {
                                    int draw2 = MathUtils.Min(BowBlock.GetDraw(data) + 1, 15);
                                    inventory.RemoveSlotItems(activeSlotIndex, 1);
                                    inventory.AddSlotItems(activeSlotIndex, Terrain.MakeBlockValue(num, 0, BowBlock.SetDraw(data, draw2)), 1);
                                }
                                break;
                            }
                            case AimState.Cancelled:
                                inventory.RemoveSlotItems(activeSlotIndex, 1);
                                inventory.AddSlotItems(activeSlotIndex, Terrain.MakeBlockValue(num, 0, BowBlock.SetDraw(data, 0)), 1);
                                m_aimStartTimes.Remove(componentMiner);
                                break;
                            case AimState.Completed: {
                                int draw = BowBlock.GetDraw(data);
                                ArrowBlock.ArrowType? arrowType = BowBlock.GetArrowType(data);
                                if (arrowType.HasValue) {
                                    Vector3 vector = componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition
                                        + componentMiner.ComponentCreature.ComponentBody.Matrix.Right * 0.3f
                                        - componentMiner.ComponentCreature.ComponentBody.Matrix.Up * 0.2f;
                                    Vector3 vector2 = Vector3.Normalize(vector + aim.Direction * 10f - vector);
                                    float num4 = MathUtils.Lerp(0f, 28f, MathF.Pow(draw / 15f, 0.75f));
                                    if (componentMiner.ComponentPlayer != null) {
                                        num4 *= 0.5f * (componentMiner.ComponentPlayer.ComponentLevel.StrengthFactor - 1f) + 1f;
                                    }
                                    Vector3 vector3 = Vector3.Zero;
                                    if (arrowType == ArrowBlock.ArrowType.WoodenArrow) {
                                        vector3 = new Vector3(0.025f, 0.025f, 0.025f);
                                    }
                                    if (arrowType == ArrowBlock.ArrowType.StoneArrow) {
                                        vector3 = new Vector3(0.01f, 0.01f, 0.01f);
                                    }
                                    int value2 = Terrain.MakeBlockValue(m_arrowBlockIndex, 0, ArrowBlock.SetArrowType(0, arrowType.Value));
                                    Vector3 vector4 = Vector3.Normalize(Vector3.Cross(vector2, Vector3.UnitY));
                                    Vector3 v2 = Vector3.Normalize(Vector3.Cross(vector2, vector4));
                                    Vector3 v3 = m_random.Float(0f - vector3.X, vector3.X) * vector4
                                        + m_random.Float(0f - vector3.Y, vector3.Y) * v2
                                        + m_random.Float(0f - vector3.Z, vector3.Z) * vector2;
                                    Vector3 velocity = componentMiner.ComponentCreature.ComponentBody.Velocity + (vector2 + v3) * num4;
                                    if (m_subsystemProjectiles.FireProjectile(
                                            value2,
                                            vector,
                                            velocity,
                                            Vector3.Zero,
                                            componentMiner.ComponentCreature
                                        )
                                        != null) {
                                        data = BowBlock.SetArrowType(data, null);
                                        m_subsystemAudio.PlaySound("Audio/Bow", 1f, m_random.Float(-0.1f, 0.1f), vector, 3f, true);
                                    }
                                }
                                else {
                                    componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(
                                        LanguageControl.Get(fName, 0),
                                        Color.White,
                                        true,
                                        false
                                    );
                                }
                                inventory.RemoveSlotItems(activeSlotIndex, 1);
                                int value3 = Terrain.MakeBlockValue(num, 0, BowBlock.SetDraw(data, 0));
                                inventory.AddSlotItems(activeSlotIndex, value3, 1);
                                int damageCount = 0;
                                if (draw >= 15) {
                                    damageCount = 2;
                                }
                                else if (draw >= 4) {
                                    damageCount = 1;
                                }
                                componentMiner.DamageActiveTool(damageCount);
                                m_aimStartTimes.Remove(componentMiner);
                                break;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public override int GetProcessInventoryItemCapacity(IInventory inventory, int slotIndex, int value) {
            int num = Terrain.ExtractContents(value);
            ArrowBlock.ArrowType arrowType = ArrowBlock.GetArrowType(Terrain.ExtractData(value));
            if (num == m_arrowBlockIndex
                && m_supportedArrowTypes.Contains(arrowType)) {
                if (!BowBlock.GetArrowType(Terrain.ExtractData(inventory.GetSlotValue(slotIndex))).HasValue) {
                    return 1;
                }
            }
            return 0;
        }

        public override void ProcessInventoryItem(IInventory inventory,
            int slotIndex,
            int value,
            int count,
            int processCount,
            out int processedValue,
            out int processedCount) {
            if (processCount == 1) {
                ArrowBlock.ArrowType arrowType = ArrowBlock.GetArrowType(Terrain.ExtractData(value));
                int data = Terrain.ExtractData(inventory.GetSlotValue(slotIndex));
                processedValue = 0;
                processedCount = 0;
                inventory.RemoveSlotItems(slotIndex, 1);
                inventory.AddSlotItems(slotIndex, Terrain.MakeBlockValue(m_bowBlockIndex, 0, BowBlock.SetArrowType(data, arrowType)), 1);
            }
            else {
                processedValue = value;
                processedCount = count;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_bowBlockIndex = BlocksManager.GetBlockIndex<BowBlock>();
            m_arrowBlockIndex = BlocksManager.GetBlockIndex<ArrowBlock>();
            base.Load(valuesDictionary);
        }
    }
}