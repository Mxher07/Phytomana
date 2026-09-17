using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using GameEntitySystem;
using Phytomana;
using Phytomana.Api;
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

        public Dictionary<Point3, float> m_manaAmounts = [];

        public Dictionary<int, float> m_maxManaAmounts = [];

        public List<ManaLink> m_links = [];

        /// <summary>入链计数：每个目标坐标被多少条链路指向（供 O(1) 判定功能花是否被绑链供魔）。</summary>
        public Dictionary<Point3, int> m_incomingLinks = [];

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemParticles m_subsystemParticles;

        public ManaNetworkManager m_network;

        public List<IManaReceiver> m_receiverBuffer = [];

        public int m_sunPowerFlowerIndex;

        public int m_manaSpreaderIndex;

        public int m_waterDonFlowerIndex;

        public int m_manaPoolIndex;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            m_sunPowerFlowerIndex = BlocksManager.GetBlockIndex<SunPowerFlower>();
            m_manaSpreaderIndex = BlocksManager.GetBlockIndex<ManaSpreaderBlock>();
            m_waterDonFlowerIndex = BlocksManager.GetBlockIndex<WaterDonFlower>();
            m_manaPoolIndex = BlocksManager.GetBlockIndex<ManaPoolBlock>();
            m_manaTabletIndex = BlocksManager.GetBlockIndex<ManaTabletBlock>();
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_maxManaAmounts[m_sunPowerFlowerIndex] = PhytoConfig.Instance.SunPowerMaxMana;
            m_maxManaAmounts[m_manaSpreaderIndex] = PhytoConfig.Instance.SpreaderMaxMana;
            m_maxManaAmounts[m_waterDonFlowerIndex] = PhytoConfig.Instance.WaterDonMaxMana;
            m_maxManaAmounts[m_manaPoolIndex] = ManaPool.MaxMana;
            ManaBlockRegistry.ApplyMaxManaOverrides(m_maxManaAmounts);
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

        public float GetMaxManaAmount(int contents) => m_maxManaAmounts.TryGetValue(contents, out float value) ? value : 0f;

        public float GetManaAmount(Point3 point) {
            if (m_network.TryGetMana(point, out float networkAmount)) {
                return networkAmount;
            }
            return m_manaAmounts.TryGetValue(point, out float value) ? value : 0f;
        }

        public void SetManaAmount(Point3 point, float amount) {
            if (m_network.TryGetReceiverStorage(point, out ManaStorage storage)) {
                storage.SetCurrent(amount);
                return;
            }
            int contents = m_subsystemTerrain.Terrain.GetCellContents(point);
            float max = GetMaxManaAmount(contents);
            if (max <= 0f) {
                return;
            }
            m_manaAmounts[point] = Math.Clamp(amount, 0f, max);
        }

        /// <summary>
        /// 旧版存档中魔法池魔力曾记在本字典里；节点注册时迁移进魔力网络并清除残留，避免幽灵魔力。
        /// </summary>
        public bool TakeLegacyMana(Point3 point, out float amount) {
            if (m_manaAmounts.TryGetValue(point, out amount)) {
                m_manaAmounts.Remove(point);
                return true;
            }
            return false;
        }

        public void AddMana(Point3 point, float amount) => SetManaAmount(point, GetManaAmount(point) + amount);

        public void RemoveMana(Point3 point, float amount) => SetManaAmount(point, GetManaAmount(point) - amount);

        public bool AddLink(Point3 from, Point3 to, bool apply = true) {
            if (from == to || HasLink(from, to)) {
                return false;
            }
            m_links.Add(new ManaLink(from, to));
            m_incomingLinks[to] = m_incomingLinks.GetValueOrDefault(to) + 1;
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
                    break;
                }
            }
            DecrementIncomingLink(to);
        }

        /// <summary>目标坐标是否被至少一条发射器链路指向（O(1)，供功能花判定是否被绑链供魔）。</summary>
        public bool HasIncomingLink(Point3 target) => m_incomingLinks.GetValueOrDefault(target) > 0;

        void DecrementIncomingLink(Point3 target) {
            if (m_incomingLinks.TryGetValue(target, out int count) && count > 1) {
                m_incomingLinks[target] = count - 1;
            }
            else {
                m_incomingLinks.Remove(target);
            }
        }

        public bool IsManaStorage(int contents) {
            if (ManaBlockRegistry.TryGet(contents, out ManaBlockDefinition definition)) {
                return definition.CanTransfer;
            }
            return contents == m_manaSpreaderIndex || contents == m_manaPoolIndex;
        }

        public double m_nextTabletDrainTime;

        public int m_manaTabletIndex = -1;

        public SubsystemGameInfo m_subsystemGameInfo;

        public void Update(float dt) {
            if (m_subsystemGameInfo != null) {
                TryDrainManaTablets();
            }
            if (ManaPoolRecipeRegistry.Count == 0) {
                return;
            }
            m_network.GetActiveReceivers(m_receiverBuffer);
            foreach (IManaReceiver receiver in m_receiverBuffer) {
                Point3 point = receiver.Position;
                if (m_subsystemTerrain.Terrain.GetCellContents(point) != m_manaPoolIndex) {
                    continue;
                }
                TryConvertPool(point, receiver);
            }
        }

        /// <summary>
        /// 空魔力石板丢入魔法池：每 1 秒从池中吸走 500mn（池中不足则不吸），
        /// 直到石板存满 3000mn。
        /// </summary>
        public void TryDrainManaTablets() {
            double time = m_subsystemGameInfo.TotalElapsedGameTime;
            if (time < m_nextTabletDrainTime) {
                return;
            }
            m_nextTabletDrainTime = time + 1.0;
            m_network.GetActiveReceivers(m_receiverBuffer);
            foreach (IManaReceiver receiver in m_receiverBuffer) {
                Point3 point = receiver.Position;
                if (m_subsystemTerrain.Terrain.GetCellContents(point) != m_manaPoolIndex
                    || receiver.ManaStorage.IsEmpty) {
                    continue;
                }
                Vector3 poolCenter = new(point.X + 0.5f, point.Y, point.Z + 0.5f);
                foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                    if (pickable.ToRemove
                        || Terrain.ExtractContents(pickable.Value) != m_manaTabletIndex) {
                        continue;
                    }
                    // 放宽命中：掉落在池格及其周边 1.25 格内（含池顶）的石板都算
                    Vector3 pos = pickable.Position;
                    float dx = pos.X - poolCenter.X;
                    float dz = pos.Z - poolCenter.Z;
                    if (pos.Y < point.Y - 0.5f
                        || pos.Y >= point.Y + 1.5f
                        || dx * dx + dz * dz > 1.25f * 1.25f) {
                        continue;
                    }
                    int mana = Terrain.ExtractData(pickable.Value);
                    if (mana >= ManaTabletBlock.MaxMana) {
                        continue;
                    }
                    int take = (int)MathF.Min(
                        PhytoConfig.Instance.TabletPoolDrainPerSecond,
                        MathF.Min(receiver.ManaStorage.Current, ManaTabletBlock.MaxMana - mana));
                    if (take <= 0) {
                        continue;
                    }
                    receiver.ManaStorage.Take(take);
                    pickable.Value = Terrain.ReplaceData(pickable.Value, mana + take);
                    break; // 每池每秒只充一块石板
                }
            }
        }

        /// <summary>
        /// 魔力池物品转化（配方由外部 .mp 文件声明）：掉入池中的原料被逐件转化，
        /// 每件消耗配方声明的魔力；魔力不足时只转化可负担的件数，剩余留在池中。
        /// </summary>
        public void TryConvertPool(Point3 poolPoint, IManaReceiver receiver) {
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (!TilePhytoFlower.IsPickableInCell(pickable, poolPoint)) {
                    continue;
                }
                ManaPoolRecipe recipe = ManaPoolRecipeRegistry.FindByIngredient(Terrain.ExtractContents(pickable.Value));
                if (recipe == null) {
                    continue;
                }
                int count = Math.Max(1, pickable.Count);
                int units = count;
                if (recipe.ManaCost > 0f) {
                    units = Math.Min(units, (int)(receiver.ManaStorage.Current / recipe.ManaCost));
                }
                if (units <= 0) {
                    continue;
                }
                if (recipe.ManaCost > 0f) {
                    receiver.ManaStorage.Take(units * recipe.ManaCost);
                }
                Vector3 position = pickable.Position;
                if (units >= count) {
                    pickable.ToRemove = true;
                }
                else {
                    pickable.Count -= units;
                }
                m_subsystemPickables.AddPickable(recipe.ResultContents, units * recipe.ResultCount, position, pickable.Velocity, null);
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

        public void PruneLinks() {
            for (int i = m_links.Count - 1; i >= 0; i--) {
                ManaLink link = m_links[i];
                if (m_subsystemTerrain.Terrain.GetCellContents(link.From) != m_manaSpreaderIndex
                    || !IsManaStorage(m_subsystemTerrain.Terrain.GetCellContents(link.To))) {
                    m_links.RemoveAt(i);
                    DecrementIncomingLink(link.To);
                }
            }
        }

        /// <summary>
        /// 推进法杖链路魔力传输（链路本质是网络边，规则归属魔力层而非行为子系统）：
        /// 遍历链路，按周期 1s/160mn 从发射器搬运到下游存储（须满足双方容量），
        /// 返回本轮实际发生传输的「源→目标」坐标对，供行为层播粒子表现。
        /// </summary>
        public List<(Point3, Point3)> AdvanceLinkTransfers(float dt) {
            List<(Point3, Point3)> transferred = [];
            foreach (ManaLink link in m_links) {
                link.TransferAccumulator += dt;
                while (link.TransferAccumulator >= StaffLinkTransferPeriod) {
                    link.TransferAccumulator -= StaffLinkTransferPeriod;
                    Point3 from = link.From;
                    Point3 to = link.To;
                    if (m_subsystemTerrain.Terrain.GetCellContents(from) != m_manaSpreaderIndex
                        || !IsManaStorage(m_subsystemTerrain.Terrain.GetCellContents(to))) {
                        continue;
                    }
                    int toContents = m_subsystemTerrain.Terrain.GetCellContents(to);
                    float maxTarget = GetMaxManaAmount(toContents);
                    if (GetManaAmount(from) >= StaffLinkTransferAmount
                        && maxTarget - GetManaAmount(to) >= StaffLinkTransferAmount) {
                        RemoveMana(from, StaffLinkTransferAmount);
                        AddMana(to, StaffLinkTransferAmount);
                        transferred.Add((from, to));
                    }
                }
            }
            PruneLinks();
            return transferred;
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
    }
}