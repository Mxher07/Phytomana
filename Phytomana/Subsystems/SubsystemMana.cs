using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ManaLink {
        public Point3 From;
        public Point3 To;
        public float TransferAccumulator;

        public ManaLink(Point3 from, Point3 to) {
            From = from;
            To = to;
        }
    }

    public class SubsystemMana : Subsystem, IUpdateable {
        public const string ManaName = "mana";
        public const string ManaShortName = "mn";
        public const float StaffLinkTransferAmount = 160f;
        public const float StaffLinkTransferPeriod = 1f;
        public const float IngotConversionCost = 300f;
        public const float BlockConversionCost = 3000f;

        public Dictionary<Point3, float> m_manaAmounts = [];

        public Dictionary<int, float> m_maxManaAmounts = [];

        public List<ManaLink> m_links = [];

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemParticles m_subsystemParticles;

        public int m_sunPowerFlowerIndex;

        public int m_manaSpreaderIndex;

        public int m_waterDonFlowerIndex;

        public int m_manaPoolIndex;

        public int m_ironIngotIndex;

        public int m_manaIngotIndex;

        public int m_ironBlockIndex;

        public int m_manaBlockIndex;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_sunPowerFlowerIndex = BlocksManager.GetBlockIndex<SunPowerFlower>();
            m_manaSpreaderIndex = BlocksManager.GetBlockIndex<ManaSpreaderBlock>();
            m_waterDonFlowerIndex = BlocksManager.GetBlockIndex<WaterDonFlower>();
            m_manaPoolIndex = BlocksManager.GetBlockIndex<ManaPoolBlock>();
            m_ironIngotIndex = BlocksManager.GetBlockIndex<IronIngotBlock>();
            m_manaIngotIndex = BlocksManager.GetBlockIndex<ManaIngotBlock>();
            m_ironBlockIndex = BlocksManager.GetBlockIndex<IronBlock>();
            m_manaBlockIndex = BlocksManager.GetBlockIndex<ManaBlock>();
            m_maxManaAmounts[m_sunPowerFlowerIndex] = 800f;
            m_maxManaAmounts[m_manaSpreaderIndex] = 1200f;
            m_maxManaAmounts[m_waterDonFlowerIndex] = 240f;
            m_maxManaAmounts[m_manaPoolIndex] = 3800f;
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
            string linkText = valuesDictionary.GetValue("ManaLinks", string.Empty);
            foreach (string item in linkText.Split([';'], StringSplitOptions.RemoveEmptyEntries)) {
                string[] array = item.Split([','], StringSplitOptions.None);
                if (array.Length == 6
                    && int.TryParse(array[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fx)
                    && int.TryParse(array[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fy)
                    && int.TryParse(array[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fz)
                    && int.TryParse(array[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tx)
                    && int.TryParse(array[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ty)
                    && int.TryParse(array[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tz)) {
                    AddLink(new Point3(fx, fy, fz), new Point3(tx, ty, tz), false);
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

            StringBuilder linkBuilder = new();
            foreach (ManaLink link in m_links) {
                linkBuilder.Append(link.From.X.ToString(CultureInfo.InvariantCulture));
                linkBuilder.Append(',');
                linkBuilder.Append(link.From.Y.ToString(CultureInfo.InvariantCulture));
                linkBuilder.Append(',');
                linkBuilder.Append(link.From.Z.ToString(CultureInfo.InvariantCulture));
                linkBuilder.Append(',');
                linkBuilder.Append(link.To.X.ToString(CultureInfo.InvariantCulture));
                linkBuilder.Append(',');
                linkBuilder.Append(link.To.Y.ToString(CultureInfo.InvariantCulture));
                linkBuilder.Append(',');
                linkBuilder.Append(link.To.Z.ToString(CultureInfo.InvariantCulture));
                linkBuilder.Append(';');
            }
            valuesDictionary.SetValue("ManaLinks", linkBuilder.ToString());
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

        public bool AddLink(Point3 from, Point3 to, bool apply = true) {
            if (from == to || HasLink(from, to)) {
                return false;
            }
            m_links.Add(new ManaLink(from, to));
            if (apply) {
                PruneLinks();
            }
            return true;
        }

        public bool HasLink(Point3 from, Point3 to) {
            foreach (ManaLink link in m_links) {
                if (link.From == from && link.To == to) {
                    return true;
                }
            }
            return false;
        }

        public void RemoveLink(Point3 from, Point3 to) {
            for (int i = 0; i < m_links.Count; i++) {
                if (m_links[i].From == from && m_links[i].To == to) {
                    m_links.RemoveAt(i);
                    return;
                }
            }
        }

        public bool IsManaStorage(int contents) => contents == m_manaSpreaderIndex || contents == m_manaPoolIndex;

        public void Update(float dt) {
            List<KeyValuePair<Point3, float>> snapshot = [.. m_manaAmounts];
            foreach (KeyValuePair<Point3, float> pair in snapshot) {
                Point3 point = pair.Key;
                if (m_subsystemTerrain.Terrain.GetCellContents(point) != m_manaPoolIndex) {
                    continue;
                }
                if (GetManaAmount(point) >= BlockConversionCost) {
                    TryConvertBlock(point);
                }
                if (GetManaAmount(point) >= IngotConversionCost) {
                    TryConvertIngot(point);
                }
            }
        }

        public void TryConvertIngot(Point3 poolPoint) {
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (Terrain.ExtractContents(pickable.Value) != m_ironIngotIndex) {
                    continue;
                }
                if (!IsPickableInCell(pickable, poolPoint)) {
                    continue;
                }
                RemoveMana(poolPoint, IngotConversionCost);
                Vector3 position = pickable.Position;
                pickable.ToRemove = true;
                m_subsystemPickables.AddPickable(m_manaIngotIndex, Math.Max(1, pickable.Count), position, pickable.Velocity, null);
                Vector3 center = new(poolPoint.X + 0.5f, poolPoint.Y + 0.2f, poolPoint.Z + 0.5f);
                foreach (Vector3 offset in new[] {
                    new Vector3(0.4f, 0f, 0.4f),
                    new Vector3(0.4f, 0f, -0.4f),
                    new Vector3(-0.4f, 0f, 0.4f),
                    new Vector3(-0.4f, 0f, -0.4f)
                }) {
                    m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                        center + offset,
                        0.8f,
                        1.2f,
                        new Color(102, 204, 255)
                    ));
                }
                return;
            }
        }

        public void TryConvertBlock(Point3 poolPoint) {
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (Terrain.ExtractContents(pickable.Value) != m_ironBlockIndex) {
                    continue;
                }
                if (!IsPickableInCell(pickable, poolPoint)) {
                    continue;
                }
                RemoveMana(poolPoint, BlockConversionCost);
                Vector3 position = pickable.Position;
                pickable.ToRemove = true;
                m_subsystemPickables.AddPickable(m_manaBlockIndex, Math.Max(1, pickable.Count), position, pickable.Velocity, null);
                Vector3 center = new(poolPoint.X + 0.5f, poolPoint.Y + 0.2f, poolPoint.Z + 0.5f);
                foreach (Vector3 offset in new[] {
                    new Vector3(0.4f, 0f, 0.4f),
                    new Vector3(0.4f, 0f, -0.4f),
                    new Vector3(-0.4f, 0f, 0.4f),
                    new Vector3(-0.4f, 0f, -0.4f)
                }) {
                    m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                        center + offset,
                        0.8f,
                        1.2f,
                        new Color(102, 204, 255)
                    ));
                }
                return;
            }
        }

        public bool IsPickableInCell(Pickable pickable, Point3 cell) {
            Vector3 position = pickable.Position;
            return position.X >= cell.X
                && position.X < cell.X + 1f
                && position.Z >= cell.Z
                && position.Z < cell.Z + 1f
                && position.Y >= cell.Y - 0.5f
                && position.Y < cell.Y + 1.5f;
        }

        public void PruneLinks() {
            for (int i = m_links.Count - 1; i >= 0; i--) {
                ManaLink link = m_links[i];
                if (m_subsystemTerrain.Terrain.GetCellContents(link.From) != m_manaSpreaderIndex
                    || !IsManaStorage(m_subsystemTerrain.Terrain.GetCellContents(link.To))) {
                    m_links.RemoveAt(i);
                }
            }
        }

        public float GetOutgoingUsage(Point3 from) {
            float usage = 0f;
            foreach (ManaLink link in m_links) {
                if (link.From != from) {
                    continue;
                }
                int toContents = m_subsystemTerrain.Terrain.GetCellContents(link.To);
                if (!IsManaStorage(toContents)) {
                    continue;
                }
                float targetFree = GetMaxManaAmount(toContents) - GetManaAmount(link.To);
                if (GetManaAmount(from) >= StaffLinkTransferAmount && targetFree >= StaffLinkTransferAmount) {
                    usage += StaffLinkTransferAmount;
                }
            }
            return usage;
        }

        public void TransferManaToLinked(Point3 from, float amount) {
            if (amount <= 0f) {
                return;
            }
            foreach (ManaLink link in m_links) {
                if (link.From != from) {
                    continue;
                }
                int toContents = m_subsystemTerrain.Terrain.GetCellContents(link.To);
                if (!IsManaStorage(toContents)) {
                    continue;
                }
                if (GetManaAmount(from) <= 0f) {
                    break;
                }
                float targetFree = GetMaxManaAmount(toContents) - GetManaAmount(link.To);
                float transfer = Math.Min(Math.Min(amount, GetManaAmount(from)), targetFree);
                if (transfer > 0f) {
                    RemoveMana(from, transfer);
                    AddMana(link.To, transfer);
                }
            }
        }
    }
}