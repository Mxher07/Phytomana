using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 花药台合成逻辑，模仿植物魔法「花瓣药剂台」的用法：
    /// 1. 拿水桶右键花药台注入水（空桶右键取回水）；无水时不吸收原料；
    /// 2. 向花药台上投掷原料（.fr 配方声明的方块），落地即被吸收进内部缓存；
    /// 3. 缓存与某条 .fr 配方完全一致时，再投掷任意种子完成合成：
    ///    消耗种子与全部原料（配方声明 ManaCost 时还需花药台存有足量魔力），
    ///    在台面上弹出目标物品；
    /// 4. 空手右键查看状态，空手潜行右键取回已投入的原料。
    /// </summary>
    public class SubsystemFlowerTableBehavior : SubsystemBlockBehavior, IUpdateable {
        public const float DefaultMaxMana = 300f;

        public Dictionary<Point3, FlowerTable> m_tables = [];

        public SubsystemPickables m_subsystemPickables;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        // 魔力网络：注册后花药台可作为接收端，由产魔源/发射器投递魔力（供 ManaCost 配方消耗）。
        public ManaNetworkManager m_network;

        public int m_seedsBlockIndex;

        public int m_waterBucketIndex;

        public int m_emptyBucketIndex;

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<FlowerTableBlock>()];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_seedsBlockIndex = BlocksManager.GetBlockIndex<SeedsBlock>();
            m_waterBucketIndex = BlocksManager.GetBlockIndex<WaterBucketBlock>();
            m_emptyBucketIndex = BlocksManager.GetBlockIndex<EmptyBucketBlock>();
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            // 存档格式：「x,y,z,水(0/1),魔力,原料1,数量1,原料2,数量2,...;」
            string text = valuesDictionary.GetValue("FlowerTables", string.Empty);
            foreach (string entry in text.Split([';'], StringSplitOptions.RemoveEmptyEntries)) {
                string[] parts = entry.Split([','], StringSplitOptions.None);
                if (parts.Length < 5
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int z)) {
                    continue;
                }
                FlowerTable table = new(new Point3(x, y, z)) {
                    HasWater = parts[3] == "1"
                };
                table.ManaStorage.LoadData(ParseFloat(parts[4]));
                for (int i = 5; i + 1 < parts.Length; i += 2) {
                    if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int contents)
                        && int.TryParse(parts[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)
                        && count > 0) {
                        table.Ingredients[contents] = count;
                    }
                }
                m_tables[new Point3(x, y, z)] = table;
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            StringBuilder stringBuilder = new();
            foreach (KeyValuePair<Point3, FlowerTable> pair in m_tables) {
                FlowerTable table = pair.Value;
                stringBuilder.Append(pair.Key.X.ToString(CultureInfo.InvariantCulture)).Append(',');
                stringBuilder.Append(pair.Key.Y.ToString(CultureInfo.InvariantCulture)).Append(',');
                stringBuilder.Append(pair.Key.Z.ToString(CultureInfo.InvariantCulture)).Append(',');
                stringBuilder.Append(table.HasWater ? '1' : '0').Append(',');
                stringBuilder.Append(table.ManaStorage.Current.ToString("R", CultureInfo.InvariantCulture));
                foreach (KeyValuePair<int, int> ingredient in table.Ingredients) {
                    stringBuilder.Append(',').Append(ingredient.Key.ToString(CultureInfo.InvariantCulture));
                    stringBuilder.Append(',').Append(ingredient.Value.ToString(CultureInfo.InvariantCulture));
                }
                stringBuilder.Append(';');
            }
            valuesDictionary.SetValue("FlowerTables", stringBuilder.ToString());
        }

        public void EnsureTable(int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (!m_tables.ContainsKey(point)) {
                m_tables[point] = new FlowerTable(point);
            }
            // 网络侧按坐标去重并仅持弱引用，重复注册无副作用（与魔力池一致）。
            m_network.RegisterReceiver(m_tables[point]);
        }

        /// <summary>方块渲染查询：该花药台是否已注水（决定水面网格是否绘制）。</summary>
        public bool HasWater(int x, int y, int z) {
            return m_tables.TryGetValue(new Point3(x, y, z), out FlowerTable table) && table.HasWater;
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            EnsureTable(x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            EnsureTable(x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (!m_tables.TryGetValue(point, out FlowerTable table)) {
                return;
            }
            m_tables.Remove(point);
            m_network.UnregisterReceiver(table, true);
            // 拆除花药台时把已吸收的原料退回台面上方，避免材料凭空消失。
            Vector3 center = new(x + 0.5f, y + 1f, z + 0.5f);
            foreach (KeyValuePair<int, int> ingredient in table.Ingredients) {
                if (ingredient.Value > 0) {
                    m_subsystemPickables.AddPickable(ingredient.Key, ingredient.Value, center, new Vector3(0f, 2f, 0f), null);
                }
            }
        }

        public void Update(float dt) {
            foreach (FlowerTable table in m_tables.Values) {
                // 区块未加载时拾取物不会与之交互，跳过以免误吸收远处数据。
                if (SubsystemTerrain.Terrain.GetChunkAtCell(table.Position.X, table.Position.Z) == null) {
                    continue;
                }
                AbsorbPickables(table);
            }
        }

        /// <summary>扫描花药台所在格（含台面）的掉落物：原料被吸收，种子触发合成。</summary>
        public void AbsorbPickables(FlowerTable table) {
            if (!table.HasWater) {
                return;
            }
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (!IsPickableInCell(pickable, table.Position)) {
                    continue;
                }
                int contents = Terrain.ExtractContents(pickable.Value);
                if (contents == m_seedsBlockIndex) {
                    if (TryCraft(table, pickable)) {
                        return;
                    }
                    // 条件不满足时种子留在台面上，玩家可以捡回。
                    continue;
                }
                if (!IsKnownIngredient(contents)) {
                    continue;
                }
                int held = table.Ingredients.GetValueOrDefault(contents);
                if (held + Math.Max(1, pickable.Count) > MaxRequiredCount(contents)) {
                    continue;
                }
                table.Ingredients[contents] = held + Math.Max(1, pickable.Count);
                pickable.ToRemove = true;
                SpawnSplashParticles(table.Position);
            }
        }

        /// <summary>
        /// 尝试以种子完成合成：需有水、原料与某条配方完全一致、魔力充足。
        /// 成功时消耗种子与原料（及魔力），在台面上方弹出产物。
        /// </summary>
        public bool TryCraft(FlowerTable table, Pickable seed) {
            if (!table.HasWater || table.Ingredients.Count == 0) {
                return false;
            }
            List<int> provided = [];
            foreach (KeyValuePair<int, int> ingredient in table.Ingredients) {
                for (int i = 0; i < ingredient.Value; i++) {
                    provided.Add(ingredient.Key);
                }
            }
            if (!FlowerTableRecipeRegistry.TryMatch(provided, out FlowerRecipe recipe)) {
                return false;
            }
            if (recipe.ManaCost > 0f && table.ManaStorage.Current < recipe.ManaCost) {
                return false;
            }
            // 只消耗一颗种子（多颗成组的掉落物按进食逻辑逐颗扣除）。
            seed.Count = MathUtils.Max(seed.Count - 1, 0);
            if (seed.Count == 0) {
                seed.ToRemove = true;
            }
            table.Ingredients.Clear();
            if (recipe.ManaCost > 0f) {
                table.ManaStorage.Take(recipe.ManaCost);
            }
            Vector3 center = new(table.Position.X + 0.5f, table.Position.Y + 1.1f, table.Position.Z + 0.5f);
            m_subsystemPickables.AddPickable(recipe.ResultContents, recipe.ResultCount, center, new Vector3(0f, 2.5f, 0f), null);
            SpawnSplashParticles(table.Position);
            m_subsystemAudio.PlaySound("Audio/PhytoMana/ding", 1f, 0f, 0f, 0f);
            return true;
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            Point3 point = raycastResult.CellFace.Point;
            if (!m_tables.TryGetValue(point, out FlowerTable table)) {
                return false;
            }
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            int activeContents = Terrain.ExtractContents(componentMiner.ActiveBlockValue);
            if (activeContents == m_waterBucketIndex) {
                return FillWater(table, componentMiner);
            }
            if (activeContents == m_emptyBucketIndex) {
                return TakeWater(table, componentMiner);
            }
            if (activeContents == 0 && player != null) {
                ComponentBody body = player.Entity.FindComponent<ComponentBody>();
                if (body != null && body.IsCrouching) {
                    return DumpIngredients(table);
                }
                ShowStatus(player, table);
                return true;
            }
            // 其余情况（手持方块、法杖等）返回 false，交回正常的放置/使用逻辑。
            return false;
        }

        public bool FillWater(FlowerTable table, ComponentMiner componentMiner) {
            if (table.HasWater) {
                ShowMessage(componentMiner, LanguageControl.Get("FlowerTableMessages", "AlreadyFilled"), Color.White);
                return true;
            }
            IInventory inventory = componentMiner.Inventory;
            int activeSlot = inventory.ActiveSlotIndex;
            int activeValue = componentMiner.ActiveBlockValue;
            int count = inventory.GetSlotCount(activeSlot);
            if (count > 1) {
                inventory.RemoveSlotItems(activeSlot, 1);
                int acquireSlot = ComponentInventoryBase.FindAcquireSlotForItem(inventory, m_emptyBucketIndex);
                if (acquireSlot < 0) {
                    // 背包放不下空桶：恢复水桶，保持原状。
                    inventory.AddSlotItems(activeSlot, activeValue, 1);
                    ShowMessage(componentMiner, LanguageControl.Get("FlowerTableMessages", "InventoryFull"), Color.White);
                    return true;
                }
                inventory.AddSlotItems(acquireSlot, m_emptyBucketIndex, 1);
            }
            else {
                inventory.RemoveSlotItems(activeSlot, count);
                if (inventory.GetSlotCount(activeSlot) == 0) {
                    inventory.AddSlotItems(activeSlot, m_emptyBucketIndex, 1);
                }
            }
            table.HasWater = true;
            SpawnSplashParticles(table.Position);
            m_subsystemAudio.PlaySound("Audio/Splashes", 1f, 0f, 0f, 0f);
            RefreshCell(table.Position);
            return true;
        }

        public bool TakeWater(FlowerTable table, ComponentMiner componentMiner) {
            if (!table.HasWater) {
                return false;
            }
            IInventory inventory = componentMiner.Inventory;
            int activeSlot = inventory.ActiveSlotIndex;
            int activeValue = componentMiner.ActiveBlockValue;
            int count = inventory.GetSlotCount(activeSlot);
            if (count > 1) {
                inventory.RemoveSlotItems(activeSlot, 1);
                int acquireSlot = ComponentInventoryBase.FindAcquireSlotForItem(inventory, m_waterBucketIndex);
                if (acquireSlot < 0) {
                    inventory.AddSlotItems(activeSlot, activeValue, 1);
                    ShowMessage(componentMiner, LanguageControl.Get("FlowerTableMessages", "InventoryFull"), Color.White);
                    return true;
                }
                inventory.AddSlotItems(acquireSlot, m_waterBucketIndex, 1);
            }
            else {
                inventory.RemoveSlotItems(activeSlot, count);
                if (inventory.GetSlotCount(activeSlot) == 0) {
                    inventory.AddSlotItems(activeSlot, m_waterBucketIndex, 1);
                }
            }
            table.HasWater = false;
            m_subsystemAudio.PlaySound("Audio/Splashes", 1f, 0f, 0f, 0f);
            RefreshCell(table.Position);
            return true;
        }

        /// <summary>空手潜行右键：把已吸收的原料全部退回台面上方。</summary>
        public bool DumpIngredients(FlowerTable table) {
            Vector3 center = new(table.Position.X + 0.5f, table.Position.Y + 1.1f, table.Position.Z + 0.5f);
            foreach (KeyValuePair<int, int> ingredient in table.Ingredients) {
                if (ingredient.Value > 0) {
                    m_subsystemPickables.AddPickable(ingredient.Key, ingredient.Value, center, new Vector3(0f, 2f, 0f), null);
                }
            }
            table.Ingredients.Clear();
            return true;
        }

        public void ShowStatus(ComponentPlayer player, FlowerTable table) {
            List<string> names = [];
            foreach (KeyValuePair<int, int> ingredient in table.Ingredients) {
                if (ingredient.Value <= 0) {
                    continue;
                }
                Block block = BlocksManager.Blocks[ingredient.Key];
                int value = Terrain.MakeBlockValue(ingredient.Key);
                names.Add($"{block.GetDisplayName(SubsystemTerrain, value)}×{ingredient.Value}");
            }
            string water = LanguageControl.Get("FlowerTableMessages", table.HasWater ? "WaterYes" : "WaterNo");
            string text = string.Format(
                LanguageControl.Get("FlowerTableMessages", "StatusFormat"),
                water,
                MathF.Round(table.ManaStorage.Current),
                MathF.Round(table.ManaStorage.Max),
                names.Count > 0 ? string.Join("、", names) : LanguageControl.Get("FlowerTableMessages", "Empty")
            );
            player.ComponentGui.DisplaySmallMessage(text, Color.White, false, false);
        }

        public void ShowMessage(ComponentMiner componentMiner, string text, Color color) {
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            player?.ComponentGui.DisplaySmallMessage(text, color, false, false);
        }

        /// <summary>原料是否出现在任意 .fr 配方中（只吸收配方认识的材料）。</summary>
        public bool IsKnownIngredient(int contents) {
            return MaxRequiredCount(contents) > 0;
        }

        /// <summary>该原料在所有配方中的最大需求数，作为缓存上限防止过量投入。</summary>
        public int MaxRequiredCount(int contents) {
            int max = 0;
            foreach (FlowerRecipe recipe in FlowerTableRecipeRegistry.Recipes) {
                foreach (FlowerRecipeIngredient ingredient in recipe.Ingredients) {
                    if (ingredient.Contents == contents) {
                        max = Math.Max(max, ingredient.Count);
                    }
                }
            }
            return max;
        }

        public void SpawnSplashParticles(Point3 cell) {
            Vector3 center = new(cell.X + 0.5f, cell.Y + 1f, cell.Z + 0.5f);
            foreach (Vector3 offset in new[] {
                new Vector3(0.3f, 0f, 0.3f),
                new Vector3(0.3f, 0f, -0.3f),
                new Vector3(-0.3f, 0f, 0.3f),
                new Vector3(-0.3f, 0f, -0.3f)
            }) {
                m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    center + offset,
                    0.6f,
                    1f,
                    new Color(102, 204, 255)
                ));
            }
        }

        /// <summary>注水/取水后强制重新生成该区块几何体，让水面立即显示或消失。</summary>
        public void RefreshCell(Point3 point) {
            TerrainChunk chunk = SubsystemTerrain.Terrain.GetChunkAtCell(point.X, point.Z);
            if (chunk != null) {
                SubsystemTerrain.TerrainUpdater.DowngradeChunkNeighborhoodState(
                    chunk.Coords,
                    0,
                    TerrainChunkState.InvalidVertices1,
                    true
                );
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

        static float ParseFloat(string text) {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        }
    }
}