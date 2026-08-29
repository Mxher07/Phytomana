using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemMana : Subsystem {
        public const string ManaName = "mana";
        public const string ManaShortName = "mn";

        public Dictionary<Point3, float> m_manaAmounts = [];

        public Dictionary<int, float> m_maxManaAmounts = [];

        public SubsystemTerrain m_subsystemTerrain;

        public int m_sunPowerFlowerIndex;

        public int m_manaSpreaderIndex;

        public int m_waterDonFlowerIndex;

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_sunPowerFlowerIndex = BlocksManager.GetBlockIndex<SunPowerFlower>();
            m_manaSpreaderIndex = BlocksManager.GetBlockIndex<ManaSpreaderBlock>();
            m_waterDonFlowerIndex = BlocksManager.GetBlockIndex<WaterDonFlower>();
            m_maxManaAmounts[m_sunPowerFlowerIndex] = 800f;
            m_maxManaAmounts[m_manaSpreaderIndex] = 1200f;
            m_maxManaAmounts[m_waterDonFlowerIndex] = 240f;
            string text = valuesDictionary.GetValue("ManaAmounts", string.Empty);
            foreach (string item in text.Split([';'], StringSplitOptions.RemoveEmptyEntries)) {
                string[] array = item.Split([','], StringSplitOptions.None);
                if (array.Length == 4) {
                    int x = int.Parse(array[0], CultureInfo.InvariantCulture);
                    int y = int.Parse(array[1], CultureInfo.InvariantCulture);
                    int z = int.Parse(array[2], CultureInfo.InvariantCulture);
                    float amount = float.Parse(array[3], CultureInfo.InvariantCulture);
                    m_manaAmounts[new Point3(x, y, z)] = Math.Max(0f, amount);
                }
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            StringBuilder stringBuilder = new();
            foreach (KeyValuePair<Point3, float> pair in m_manaAmounts) {
                stringBuilder.Append(pair.Key.X.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append(pair.Key.Y.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append(pair.Key.Z.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append(pair.Value.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(';');
            }
            valuesDictionary.SetValue("ManaAmounts", stringBuilder.ToString());
        }

        public bool CanStoreMana(int contents) => m_maxManaAmounts.ContainsKey(contents);

        public float GetMaxManaAmount(int contents) => m_maxManaAmounts.TryGetValue(contents, out float value) ? value : 0f;

        public float GetManaAmount(Point3 point) => m_manaAmounts.TryGetValue(point, out float value) ? value : 0f;

        public void SetManaAmount(Point3 point, float amount) {
            int contents = m_subsystemTerrain.Terrain.GetCellContents(point);
            float max = GetMaxManaAmount(contents);
            if (max <= 0f) {
                return;
            }
            m_manaAmounts[point] = Math.Clamp(amount, 0f, max);
        }

        public void AddMana(Point3 point, float amount) => SetManaAmount(point, GetManaAmount(point) + amount);

        public void RemoveMana(Point3 point, float amount) => SetManaAmount(point, GetManaAmount(point) - amount);

        public void RemoveBlockMana(Point3 point) => m_manaAmounts.Remove(point);

        public bool HasSpreaderNearby(Point3 from) {
            for (int dx = -1; dx <= 1; dx++) {
                for (int dz = -1; dz <= 1; dz++) {
                    if (m_subsystemTerrain.Terrain.GetCellContents(from.X + dx, from.Y, from.Z + dz) == m_manaSpreaderIndex) {
                        return true;
                    }
                }
            }
            return false;
        }

        public float TransferManaToBestSpreader(Point3 from, float amount) {
            if (amount <= 0f) {
                return 0f;
            }
            Point3? bestPoint = null;
            float bestFree = 0f;
            for (int dx = -1; dx <= 1; dx++) {
                for (int dz = -1; dz <= 1; dz++) {
                    Point3 point = new(from.X + dx, from.Y, from.Z + dz);
                    int contents = m_subsystemTerrain.Terrain.GetCellContents(point);
                    if (contents != m_manaSpreaderIndex) {
                        continue;
                    }
                    float free = GetMaxManaAmount(contents) - GetManaAmount(point);
                    if (free > bestFree) {
                        bestFree = free;
                        bestPoint = point;
                    }
                }
            }
            if (!bestPoint.HasValue || bestFree <= 0f) {
                return 0f;
            }
            float transfer = Math.Min(Math.Min(amount, GetManaAmount(from)), bestFree);
            if (transfer <= 0f) {
                return 0f;
            }
            RemoveMana(from, transfer);
            AddMana(bestPoint.Value, transfer);
            return transfer;
        }
    }
}