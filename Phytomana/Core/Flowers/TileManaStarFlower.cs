using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 魔法星：观测型花朵。自身不产魔、不储魔、不接法杖，只做「魔力潮汐的星图」：
    /// 每 3 秒对比平面 3×3 范围内全部魔法池的存量快照——
    /// 任一池变多则蓝色星屑向上升腾（+0.16），变少则红色星屑向下跌落（-0.08）；存量不变则静默。
    /// </summary>
    public class TileManaStarFlower : TilePhytoFlower {
        public const double CheckInterval = 3.0;

        /// <summary>扫描范围：以花为心的 3×3 平面（x/z 各 ±1）。</summary>
        public const int ScanRadius = 1;

        /// <summary>单次星屑粒子数量。</summary>
        public const int ParticleCount = 4;

        public SubsystemParticles m_subsystemParticles;

        public double m_nextCheckTime;

        /// <summary>上一轮快照：平面 3×3 内各魔法池存量（仅同坐标池有记忆）。</summary>
        public Dictionary<Point3, float> m_lastPoolLevels = [];

        public TileManaStarFlower(Point3 position) : base(position) { }

        public override void OnPlaced() {
            InitializeTimers();
        }

        public override void OnChunkLoad() {
            InitializeTimers();
        }

        public void InitializeTimers() {
            m_nextCheckTime = TotalTime;
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            if (time < m_nextCheckTime) {
                return;
            }
            m_nextCheckTime = time + CheckInterval;
            CheckPools();
        }

        public void CheckPools() {
            SubsystemMana mana = Scheduler?.Project?.FindSubsystem<SubsystemMana>(false);
            if (mana == null) {
                return;
            }
            // 采集本轮快照，与上轮逐池对比
            Dictionary<Point3, float> current = [];
            for (int dx = -ScanRadius; dx <= ScanRadius; dx++) {
                for (int dz = -ScanRadius; dz <= ScanRadius; dz++) {
                    Point3 point = new(Position.X + dx, Position.Y, Position.Z + dz);
                    float level = mana.GetManaAmount(point);
                    if (level > 0f) {
                        current[point] = level;
                    }
                }
            }
            bool rose = false;
            bool fell = false;
            foreach (Point3 point in current.Keys) {
                float level = current[point];
                if (m_lastPoolLevels.TryGetValue(point, out float last) && last > 0f) {
                    if (level > last) {
                        rose = true;
                    }
                    else if (level < last) {
                        fell = true;
                    }
                }
            }
            m_lastPoolLevels = current;
            if (!rose && !fell) {
                return;
            }
            // 单色版构造（count 默认 2），上升粒子向 +0.16 偏移，下降粒子向 -0.08 偏移
            Vector3 origin = new(Position.X + 0.5f, Position.Y + 0.8f, Position.Z + 0.5f);
            if (rose) {
                m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    origin,
                    0.5f,
                    1.2f,
                    Color.Blue,
                    origin + new Vector3(0, 0.16f, 0),
                    ParticleCount
                ));
            }
            if (fell) {
                m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    origin,
                    0.5f,
                    1.2f,
                    Color.Red,
                    origin + new Vector3(0, -0.08f, 0),
                    ParticleCount
                ));
            }
        }

        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
        }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            values.SetValue("NextCheck", Math.Max(0.0, m_nextCheckTime - TotalTime));
        }

        public override void LoadData(ValuesDictionary values) {
            base.LoadData(values);
            m_nextCheckTime = TotalTime + values.GetValue("NextCheck", 0.0);
        }
    }
}
