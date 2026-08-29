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
        public const float MaxManaAmount = 1000f;

        public Dictionary<Point3, float> m_manaAmounts = [];

        public int[] m_manaBlockIndexes;

        public override void Load(ValuesDictionary valuesDictionary) {
            m_manaBlockIndexes =
            [
                BlocksManager.GetBlockIndex<SunPowerFlower>(),
                BlocksManager.GetBlockIndex<ManaSpreaderBlock>()
            ];
            string text = valuesDictionary.GetValue("ManaAmounts", string.Empty);
            foreach (string item in text.Split([';'], StringSplitOptions.RemoveEmptyEntries)) {
                string[] array = item.Split([','], StringSplitOptions.None);
                if (array.Length == 4) {
                    int x = int.Parse(array[0], CultureInfo.InvariantCulture);
                    int y = int.Parse(array[1], CultureInfo.InvariantCulture);
                    int z = int.Parse(array[2], CultureInfo.InvariantCulture);
                    float amount = float.Parse(array[3], CultureInfo.InvariantCulture);
                    m_manaAmounts[new Point3(x, y, z)] = Math.Clamp(amount, 0f, MaxManaAmount);
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

        public bool CanStoreMana(int contents) {
            foreach (int blockIndex in m_manaBlockIndexes) {
                if (blockIndex == contents) {
                    return true;
                }
            }
            return false;
        }

        public float GetMaxManaAmount(int contents) => MaxManaAmount;

        public float GetManaAmount(Point3 point) => m_manaAmounts.TryGetValue(point, out float amount) ? amount : 0f;

        public void SetManaAmount(Point3 point, float amount) {
            m_manaAmounts[point] = Math.Clamp(amount, 0f, MaxManaAmount);
        }

        public void AddMana(Point3 point, float amount) {
            SetManaAmount(point, GetManaAmount(point) + amount);
        }

        public void RemoveMana(Point3 point, float amount) {
            SetManaAmount(point, GetManaAmount(point) - amount);
        }

        public void RemoveBlockMana(Point3 point) {
            m_manaAmounts.Remove(point);
        }
    }
}