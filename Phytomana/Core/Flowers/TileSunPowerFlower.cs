using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 日耀花：吞食掉落燃料燃烧产魔（产魔花）。投递由魔力网络负责，本节点只管燃烧与流失。
    /// </summary>
    public class TileSunPowerFlower : TileGeneratingFlower {
        public const double ScanInterval = 1.0;
        public const double DrainInterval = 1.0;
        public const double ProductionParticleInterval = 3.0;
        public const double DrainParticleInterval = 5.0;
        public const float DefaultBaseManaRate = 20f;
        public const float DefaultMaxMana = 800f;
        public const float ManaRatePerLevel = 10f;
        public const float ManaRatePeriod = 5f;
        public const float DrainAmount = 5f;
        public const float IsolatedDrainDelay = 1.5f;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public double m_nextScanTime;

        public double m_burnEndTime;

        public int m_burnHeatLevel;

        public double m_nextProductionParticleTime;

        public double m_nextDrainTime;

        public double m_nextBlueParticleTime;

        public float m_idleIsolatedElapsed;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("SunPowerFlower", PhytoConfig.Instance.SunPowerMaxMana);

        public override bool IsLosingMana => !IsProducing && m_idleIsolatedElapsed > IsolatedDrainDelay;

        public TileSunPowerFlower(Point3 position) : base(position) { }

        public override void OnPlaced() {
            InitializeTimers();
        }

        public override void OnChunkLoad() {
            InitializeTimers();
        }

        public override float GetProductionRate() {
            return IsProducing
                ? (PhytoConfig.Instance.SunPowerBaseManaRate + ManaRatePerLevel * m_burnHeatLevel) / ManaRatePeriod / 1.65f
                : 0f;
        }

        public void InitializeTimers() {
            double time = TotalTime;
            m_nextScanTime = time;
            m_nextDrainTime = time;
            m_nextBlueParticleTime = time + DrainParticleInterval;
            m_nextProductionParticleTime = time;
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            if (State == FlowerState.Working) {
                if (time >= m_burnEndTime) {
                    SetState(FlowerState.Idle);
                }
                else {
                    m_idleIsolatedElapsed = 0f;
                    GenerateMana(GetProductionRate() * DeltaTime);
                    if (time >= m_nextProductionParticleTime) {
                        m_nextProductionParticleTime = time + ProductionParticleInterval;
                        SpawnManaParticle(Position, 0.6f, 0.5f, 2f, Color.Red);
                    }
                }
            }
            else {
                if (Scheduler.m_network.HasReachableReceiver(this)) {
                    m_idleIsolatedElapsed = 0f;
                }
                else {
                    m_idleIsolatedElapsed += DeltaTime;
                }
                if (m_idleIsolatedElapsed > IsolatedDrainDelay) {
                    if (time >= m_nextDrainTime) {
                        m_nextDrainTime = time + DrainInterval;
                        if (!ManaStorage.IsEmpty) {
                            ManaStorage.Take(DrainAmount);
                        }
                    }
                    if (time >= m_nextBlueParticleTime) {
                        m_nextBlueParticleTime = time + DrainParticleInterval;
                        if (!ManaStorage.IsEmpty) {
                            SpawnManaParticle(Position, 0.9f, 0.25f, 1.5f, Color.Blue);
                        }
                    }
                }
                if (time >= m_nextScanTime) {
                    m_nextScanTime = time + ScanInterval;
                    TryEatFuel();
                }
            }
        }

        public void TryEatFuel() {
            if (ManaStorage.IsFull) {
                return;
            }
            Pickable best = null;
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (!IsPickableInCell(pickable, Position)) {
                    continue;
                }
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
                if (block.GetFuelHeatLevel(pickable.Value) <= 0f) {
                    continue;
                }
                if (block.GetFuelFireDuration(pickable.Value) <= 0f) {
                    continue;
                }
                if (best == null || pickable.Count > best.Count) {
                    best = pickable;
                }
            }
            if (best == null) {
                return;
            }
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (!IsPickableInCell(pickable, Position)) {
                    continue;
                }
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
                if (block.GetFuelHeatLevel(pickable.Value) > 0f) {
                    pickable.ToRemove = true;
                }
            }
            Block bestBlock = BlocksManager.Blocks[Terrain.ExtractContents(best.Value)];
            SetState(FlowerState.Working);
            m_burnHeatLevel = (int)bestBlock.GetFuelHeatLevel(best.Value);
            m_burnEndTime = TotalTime + bestBlock.GetFuelFireDuration(best.Value);
            m_nextProductionParticleTime = TotalTime + ProductionParticleInterval;
            m_subsystemAudio.PlaySound("Audio/PhytoMana/spreaderFire", 1f, 0f, 0f, 0f);
        }

        public void SpawnManaParticle(Point3 point, float yOffset, float size, float duration, Color color) {
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(point.X + 0.5f, point.Y + yOffset, point.Z + 0.5f),
                size,
                duration,
                color
            ));
        }

        public void ResolveSubsystems() {
            if (m_subsystemPickables != null) {
                return;
            }
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
        }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            values.SetValue("BurnHeatLevel", m_burnHeatLevel);
            values.SetValue("BurnRemaining", State == FlowerState.Working ? Math.Max(0.0, m_burnEndTime - TotalTime) : 0.0);
        }

        public override void LoadData(ValuesDictionary values) {
            base.LoadData(values);
            m_burnHeatLevel = values.GetValue("BurnHeatLevel", 0);
            double remaining = values.GetValue("BurnRemaining", 0.0);
            if (remaining > 0.0) {
                m_burnEndTime = TotalTime + remaining;
                m_nextProductionParticleTime = TotalTime;
            }
        }
    }
}
