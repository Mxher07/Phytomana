using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemMusketBlockBehavior : SubsystemBlockBehavior {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemTime m_subsystemTime;

        public SubsystemProjectiles m_subsystemProjectiles;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemNoise m_subsystemNoise;
        public static string fName = "SubsystemMusketBlockBehavior";
        public Random m_random = new();

        public Dictionary<ComponentMiner, double> m_aimStartTimes = [];

        public int m_BulletBlockIndex;

        public int m_MusketBlockIndex;
        public override int[] HandledBlocks => [];

        public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer) {
            componentPlayer.ComponentGui.ModalPanelWidget = componentPlayer.ComponentGui.ModalPanelWidget == null
                ? new MusketWidget(inventory, slotIndex)
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
                    int num2 = slotValue;
                    int num3 = 0;
                    if (num == m_MusketBlockIndex
                        && slotCount > 0) {
                        if (!m_aimStartTimes.TryGetValue(componentMiner, out double value)) {
                            value = m_subsystemTime.GameTime;
                            m_aimStartTimes[componentMiner] = value;
                        }
                        float num4 = (float)(m_subsystemTime.GameTime - value);
                        float num5 = (float)MathUtils.Remainder(m_subsystemTime.GameTime, 1000.0);
                        Vector3 v =
                            ((componentMiner.ComponentCreature.ComponentBody.IsCrouching ? 0.01f : 0.03f)
                                + 0.2f * MathUtils.Saturate((num4 - 2.5f) / 6f))
                            * new Vector3 {
                                X = SimplexNoise.OctavedNoise(num5, 2f, 3, 2f, 0.5f),
                                Y = SimplexNoise.OctavedNoise(num5 + 100f, 2f, 3, 2f, 0.5f),
                                Z = SimplexNoise.OctavedNoise(num5 + 200f, 2f, 3, 2f, 0.5f)
                            };
                        aim.Direction = Vector3.Normalize(aim.Direction + v);
                        switch (state) {
                            case AimState.InProgress: {
                                if (num4 >= 10f) {
                                    componentMiner.ComponentCreature.ComponentCreatureSounds.PlayMoanSound();
                                    return true;
                                }
                                if (num4 > 0.5f
                                    && !MusketBlock.GetHammerState(Terrain.ExtractData(num2))) {
                                    num2 = Terrain.MakeBlockValue(num, 0, MusketBlock.SetHammerState(Terrain.ExtractData(num2), true));
                                    m_subsystemAudio.PlaySound("Audio/HammerCock", 1f, m_random.Float(-0.1f, 0.1f), 0f, 0f);
                                }
                                ComponentFirstPersonModel
                                    componentFirstPersonModel = componentMiner.Entity.FindComponent<ComponentFirstPersonModel>();
                                if (componentFirstPersonModel != null) {
                                    componentMiner.ComponentPlayer?.ComponentAimingSights.ShowAimingSights(aim.Position, aim.Direction);
                                    componentFirstPersonModel.ItemOffsetOrder = new Vector3(-0.21f, 0.15f, 0.08f);
                                    componentFirstPersonModel.ItemRotationOrder = new Vector3(-0.7f, 0f, 0f);
                                }
                                componentMiner.ComponentCreature.ComponentCreatureModel.AimHandAngleOrder = 1.4f;
                                componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemOffsetOrder = new Vector3(-0.08f, -0.08f, 0.07f);
                                componentMiner.ComponentCreature.ComponentCreatureModel.InHandItemRotationOrder = new Vector3(-1.7f, 0f, 0f);
                                break;
                            }
                            case AimState.Cancelled:
                                if (MusketBlock.GetHammerState(Terrain.ExtractData(num2))) {
                                    num2 = Terrain.MakeBlockValue(num, 0, MusketBlock.SetHammerState(Terrain.ExtractData(num2), false));
                                    m_subsystemAudio.PlaySound("Audio/HammerUncock", 1f, m_random.Float(-0.1f, 0.1f), 0f, 0f);
                                }
                                m_aimStartTimes.Remove(componentMiner);
                                break;
                            case AimState.Completed: {
                                bool flag = false;
                                int value2 = 0;
                                int num6 = 0;
                                float s = 0f;
                                Vector3 vector = Vector3.Zero;
                                MusketBlock.LoadState loadState = MusketBlock.GetLoadState(data);
                                BulletBlock.BulletType? bulletType = MusketBlock.GetBulletType(data);
                                if (MusketBlock.GetHammerState(Terrain.ExtractData(num2))) {
                                    switch (loadState) {
                                        case MusketBlock.LoadState.Empty:
                                            componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(
                                                LanguageControl.Get(fName, 0),
                                                Color.White,
                                                true,
                                                false
                                            ); break;
                                        case MusketBlock.LoadState.Gunpowder:
                                        case MusketBlock.LoadState.Wad:
                                            flag = true;
                                            componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(
                                                LanguageControl.Get(fName, 1),
                                                Color.White,
                                                true,
                                                false
                                            );
                                            break;
                                        case MusketBlock.LoadState.Loaded:
                                            flag = true;
                                            if (bulletType == BulletBlock.BulletType.Buckshot) {
                                                value2 = Terrain.MakeBlockValue(
                                                    m_BulletBlockIndex,
                                                    0,
                                                    BulletBlock.SetBulletType(0, BulletBlock.BulletType.BuckshotBall)
                                                );
                                                num6 = 8;
                                                vector = new Vector3(0.04f, 0.04f, 0.25f);
                                                s = 80f;
                                            }
                                            else if (bulletType == BulletBlock.BulletType.BuckshotBall) {
                                                value2 = Terrain.MakeBlockValue(
                                                    m_BulletBlockIndex,
                                                    0,
                                                    BulletBlock.SetBulletType(0, BulletBlock.BulletType.BuckshotBall)
                                                );
                                                num6 = 1;
                                                vector = new Vector3(0.06f, 0.06f, 0f);
                                                s = 60f;
                                            }
                                            else if (bulletType.HasValue) {
                                                value2 = Terrain.MakeBlockValue(
                                                    m_BulletBlockIndex,
                                                    0,
                                                    BulletBlock.SetBulletType(0, bulletType.Value)
                                                );
                                                num6 = 1;
                                                s = 120f;
                                            }
                                            break;
                                    }
                                }
                                if (flag) {
                                    if (componentMiner.ComponentCreature.ComponentBody.ImmersionFactor > 0.4f) {
                                        m_subsystemAudio.PlaySound(
                                            "Audio/MusketMisfire",
                                            1f,
                                            m_random.Float(-0.1f, 0.1f),
                                            componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition,
                                            3f,
                                            true
                                        );
                                    }
                                    else {
                                        Vector3 vector2 = componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition
                                            + componentMiner.ComponentCreature.ComponentBody.Matrix.Right * 0.3f
                                            - componentMiner.ComponentCreature.ComponentBody.Matrix.Up * 0.2f;
                                        Vector3 vector3 = Vector3.Normalize(vector2 + aim.Direction * 10f - vector2);
                                        Vector3 vector4 = Vector3.Normalize(Vector3.Cross(vector3, Vector3.UnitY));
                                        Vector3 v2 = Vector3.Normalize(Vector3.Cross(vector3, vector4));
                                        for (int i = 0; i < num6; i++) {
                                            Vector3 v3 = m_random.Float(0f - vector.X, vector.X) * vector4
                                                + m_random.Float(0f - vector.Y, vector.Y) * v2
                                                + m_random.Float(0f - vector.Z, vector.Z) * vector3;
                                            Vector3 velocity = componentMiner.ComponentCreature.ComponentBody.Velocity + s * (vector3 + v3);
                                            Projectile projectile = m_subsystemProjectiles.FireProjectile(
                                                value2,
                                                vector2,
                                                velocity,
                                                Vector3.Zero,
                                                componentMiner.ComponentCreature
                                            );
                                            if (projectile != null) {
                                                projectile.ProjectileStoppedAction = ProjectileStoppedAction.Disappear;
                                            }
                                        }
                                        m_subsystemAudio.PlaySound(
                                            "Audio/MusketFire",
                                            1f,
                                            m_random.Float(-0.1f, 0.1f),
                                            componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition,
                                            10f,
                                            true
                                        );
                                        m_subsystemParticles.AddParticleSystem(
                                            new GunSmokeParticleSystem(m_subsystemTerrain, vector2 + 0.3f * vector3, vector3)
                                        );
                                        m_subsystemNoise.MakeNoise(vector2, 1f, 40f);
                                        componentMiner.ComponentCreature.ComponentBody.ApplyImpulse(-4f * vector3);
                                    }
                                    num2 = Terrain.MakeBlockValue(
                                        Terrain.ExtractContents(num2),
                                        0,
                                        MusketBlock.SetLoadState(Terrain.ExtractData(num2), MusketBlock.LoadState.Empty)
                                    );
                                    num3 = 1;
                                }
                                if (MusketBlock.GetHammerState(Terrain.ExtractData(num2))) {
                                    num2 = Terrain.MakeBlockValue(
                                        Terrain.ExtractContents(num2),
                                        0,
                                        MusketBlock.SetHammerState(Terrain.ExtractData(num2), false)
                                    );
                                    m_subsystemAudio.PlaySound("Audio/HammerRelease", 1f, m_random.Float(-0.1f, 0.1f), 0f, 0f);
                                }
                                m_aimStartTimes.Remove(componentMiner);
                                break;
                            }
                        }
                    }
                    if (num2 != slotValue) {
                        inventory.RemoveSlotItems(activeSlotIndex, 1);
                        inventory.AddSlotItems(activeSlotIndex, num2, 1);
                    }
                    if (num3 > 0) {
                        componentMiner.DamageActiveTool(num3);
                    }
                }
            }
            return false;
        }

        public override int GetProcessInventoryItemCapacity(IInventory inventory, int slotIndex, int value) {
            int num = Terrain.ExtractContents(value);
            MusketBlock.LoadState loadState = MusketBlock.GetLoadState(Terrain.ExtractData(inventory.GetSlotValue(slotIndex)));
            if (loadState == MusketBlock.LoadState.Empty
                && num == 109) {
                return 1;
            }
            if (loadState == MusketBlock.LoadState.Gunpowder
                && num == 205) {
                return 1;
            }
            if (loadState == MusketBlock.LoadState.Wad
                && num == m_BulletBlockIndex) {
                return 1;
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
            processedValue = value;
            processedCount = count;
            if (processCount == 1) {
                int data = Terrain.ExtractData(inventory.GetSlotValue(slotIndex));
                MusketBlock.LoadState loadState = MusketBlock.GetLoadState(data);
                BulletBlock.BulletType? bulletType = MusketBlock.GetBulletType(data);
                switch (loadState) {
                    case MusketBlock.LoadState.Empty:
                        loadState = MusketBlock.LoadState.Gunpowder;
                        bulletType = null;
                        break;
                    case MusketBlock.LoadState.Gunpowder:
                        loadState = MusketBlock.LoadState.Wad;
                        bulletType = null;
                        break;
                    case MusketBlock.LoadState.Wad: {
                        loadState = MusketBlock.LoadState.Loaded;
                        int data2 = Terrain.ExtractData(value);
                        bulletType = BulletBlock.GetBulletType(data2);
                        break;
                    }
                }
                processedValue = 0;
                processedCount = 0;
                inventory.RemoveSlotItems(slotIndex, 1);
                inventory.AddSlotItems(
                    slotIndex,
                    Terrain.MakeBlockValue(m_MusketBlockIndex, 0, MusketBlock.SetBulletType(MusketBlock.SetLoadState(data, loadState), bulletType)),
                    1
                );
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(true);
            m_BulletBlockIndex = BlocksManager.GetBlockIndex<BulletBlock>();
            m_MusketBlockIndex = BlocksManager.GetBlockIndex<MusketBlock>();
            base.Load(valuesDictionary);
        }
    }
}