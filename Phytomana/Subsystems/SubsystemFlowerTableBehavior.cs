using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using Phytomana.Network;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 花药台合成逻辑，模仿植物魔法「花瓣药剂台」的用法：
    /// 1. 拿水桶右键花药台注入水（空桶右键取回水）；无水时不吸收原料；
    /// 2. 向花药台上投掷原料（.fr 配方声明的方块），落地即被吸收进内部缓存
    ///    （缓存按键为完整方块值，颜色等特殊值变体分格保存）；
    /// 3. 缓存与某条 .fr 配方完全一致时，再投掷任意种子完成合成：
    ///    消耗种子、全部原料与一池水（配方声明 ManaCost 时还需花药台存有足量魔力），
    ///    在台面上弹出目标物品；配方声明 CopyData 时产物继承第一份原料的 data；
    /// 4. 空手右键查看状态，空手潜行右键取回已投入的原料；
    /// 5. 雨天自动集水：露天（上方无遮挡）且正在下雨时 30 秒集满，
    ///    进度可在空手右键状态中查看，集满时对附近玩家提示。
    /// </summary>
    public class SubsystemFlowerTableBehavior : SubsystemBlockBehavior, IUpdateable {
        public const float DefaultMaxMana = 300f;

        /// <summary>雨天集满一池水所需的时间（秒）。</summary>
        public const float RainFillSeconds = 30f;

        /// <summary>集满提示的广播半径（格）。</summary>
        public const float RainMessageRadius = 10f;

        public Dictionary<Point3, FlowerTable> m_tables = [];

        public SubsystemPickables m_subsystemPickables;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        // 魔力网络：注册后花药台可作为接收端，由产魔源/发射器投递魔力（供 ManaCost 配方消耗）。
        public ManaNetworkManager m_network;

        public SubsystemWeather m_subsystemWeather;

        public SubsystemPlayers m_subsystemPlayers;

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
            m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(false);
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(false);
            m_seedsBlockIndex = BlocksManager.GetBlockIndex<SeedsBlock>();
            m_waterBucketIndex = BlocksManager.GetBlockIndex<WaterBucketBlock>();
            m_emptyBucketIndex = BlocksManager.GetBlockIndex<EmptyBucketBlock>();
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            PhytoNet.AddTableHandler(HandleTableActionFromClient);
            PhytoNet.HandleTableStatusClient += HandleTableStatusReply;
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

        /// <summary>方块渲染查询：返回缓存中原料的完整方块值（含颜色变体，渲染漂浮小方块用）。</summary>
        public List<int> GetIngredientValues(int x, int y, int z) {
            List<int> list = [];
            if (m_tables.TryGetValue(new Point3(x, y, z), out FlowerTable table)) {
                foreach (KeyValuePair<int, int> pair in table.Ingredients) {
                    if (pair.Value > 0) {
                        list.Add(pair.Key);
                    }
                }
            }
            return list;
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
            // 服务器每帧兜底补注册自定义包（见 PhytoNet.EnsureRegistered）。
            PhytoNet.EnsureRegistered();
            // 服务端权威：吸收拾取物、雨天集水只在服务器模拟。
            if (NetworkManager.IsClientRunning) {
                return;
            }
            foreach (FlowerTable table in m_tables.Values) {
                // 区块未加载时拾取物不会与之交互，跳过以免误吸收远处数据。
                if (SubsystemTerrain.Terrain.GetChunkAtCell(table.Position.X, table.Position.Z) == null) {
                    continue;
                }
                AbsorbPickables(table);
            }
            UpdateRainCollection(dt);
        }

        /// <summary>
        /// 雨天集水：正在下雨、花药台未装水且露天（其方块位于该列高度图顶部，
        /// 与原版玩家淋雨判定一致，屋顶会自动挡雨）时，30 秒集满并提示附近玩家。
        /// </summary>
        public void UpdateRainCollection(float dt) {
            if (m_subsystemWeather == null || !m_subsystemWeather.IsPrecipitationStarted) {
                return;
            }
            foreach (FlowerTable table in m_tables.Values) {
                if (table.HasWater) {
                    continue;
                }
                if (SubsystemTerrain.Terrain.GetChunkAtCell(table.Position.X, table.Position.Z) == null) {
                    continue;
                }
                PrecipitationShaftInfo info = m_subsystemWeather.GetPrecipitationShaftInfo(table.Position.X, table.Position.Z);
                if (info.Type != PrecipitationType.Rain
                    || info.Intensity <= 0f
                    || table.Position.Y + 1 < info.YLimit) {
                    continue;
                }
                table.RainFill += dt;
                if (table.RainFill < RainFillSeconds) {
                    continue;
                }
                table.RainFill = 0f;
                table.HasWater = true;
                SpawnSplashParticles(table.Position);
                m_subsystemAudio.PlaySound("Audio/Splashes", 1f, 0f, 0f, 0f);
                RefreshCell(table.Position);
                NotifyRainFilled(table);
            }
        }

        /// <summary>集满雨水时对附近玩家弹出提示。</summary>
        public void NotifyRainFilled(FlowerTable table) {
            
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
                int value = pickable.Value;
                int contents = Terrain.ExtractContents(value);
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
                // 按完整方块值缓存：颜色等特殊值变体分格保存，合成产物可继承。
                int held = table.Ingredients.GetValueOrDefault(value);
                if (held + Math.Max(1, pickable.Count) > MaxRequiredCount(contents)) {
                    continue;
                }
                table.Ingredients[value] = held + Math.Max(1, pickable.Count);
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
            int firstValue = 0;
            foreach (KeyValuePair<int, int> ingredient in table.Ingredients) {
                if (firstValue == 0 && ingredient.Value > 0) {
                    firstValue = ingredient.Key;
                }
                for (int i = 0; i < ingredient.Value; i++) {
                    // 传完整方块值：配方的色号槽位按 data 精确匹配
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
            // CopyData 配方：产物的 data 继承第一份原料（颜色变体保色）；否则用配方声明的固定 data。
            int resultValue = Terrain.MakeBlockValue(
                recipe.ResultContents,
                0,
                recipe.CopyData ? Terrain.ExtractData(firstValue) : recipe.ResultData
            );
            // 合成耗尽一池水（雨水集的进度同步清零），下次合成需重新注水/集水。
            table.HasWater = false;
            table.RainFill = 0f;
            RefreshCell(table.Position);
            Vector3 center = new(table.Position.X + 0.5f, table.Position.Y + 1.1f, table.Position.Z + 0.5f);
            m_subsystemPickables.AddPickable(resultValue, recipe.ResultCount, center, new Vector3(0f, 2.5f, 0f), null);
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
            bool isMain = player?.PlayerData?.IsMainPlayer ?? true;
            // 服务端权威：服务器重放的远程玩家交互被抑制，权威逻辑改走请求。
            if (NetworkManager.IsServerRunning && !isMain) {
                return true;
            }
            int activeContents = Terrain.ExtractContents(componentMiner.ActiveBlockValue);
            // 联机客户端：只发请求，由服务器执行并回执反馈文本。
            if (NetworkManager.IsClientRunning) {
                bool request = activeContents == m_waterBucketIndex
                    || activeContents == m_emptyBucketIndex
                    || (activeContents == 0 && player != null);
                if (request && isMain) {
                    PhytoNet.Send(PhytoNet.BuildTableAction(PhytoTableAction.FlowerTable, point));
                }
                return request;
            }
            // 单机/主机：直接执行权威交互。
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

        /// <summary>服务器处理客户端花药台交互请求：先按手持物/潜行落权威逻辑，再回执状态文本。</summary>
        public void HandleTableActionFromClient(PhytoModPacket packet) {
            if (packet.ByteA != PhytoTableAction.FlowerTable || !packet.HasA) {
                return;
            }
            if (!m_tables.TryGetValue(packet.PointA, out FlowerTable table)) {
                return;
            }
            ComponentPlayer player = PhytoNet.FindPlayer(packet.From?.PlayerIndex ?? packet.PlayerIndex);
            if (player == null) {
                return;
            }
            ComponentMiner miner = player.ComponentMiner;
            if (miner == null) {
                return;
            }
            int activeContents = Terrain.ExtractContents(miner.ActiveBlockValue);
            if (activeContents == m_waterBucketIndex) {
                FillWater(table, miner);
            }
            else if (activeContents == m_emptyBucketIndex) {
                TakeWater(table, miner);
            }
            else if (activeContents == 0) {
                ComponentBody body = player.Entity.FindComponent<ComponentBody>();
                if (body != null && body.IsCrouching) {
                    DumpIngredients(table);
                }
            }
            PhytoNet.ReplyTo(packet, PhytoNet.BuildTableStatusReply(packet, BuildStatusText(table)));
        }

        /// <summary>客户端显示服务器回执的方块状态/结果文本。</summary>
        public void HandleTableStatusReply(PhytoModPacket packet) {
            if (!packet.HasText || packet.Text == null) {
                return;
            }
            if (PhytoNet.FindPlayer(packet.PlayerIndex) is { } player) {
                player.ComponentGui.DisplaySmallMessage(packet.Text, Color.White, false, false);
            }
        }

        public void ShowStatus(ComponentPlayer player, FlowerTable table) {
            player.ComponentGui.DisplaySmallMessage(BuildStatusText(table), Color.White, false, false);
        }

        /// <summary>构建花药台状态文本（水/魔力/原料/合成提示）。</summary>
        public string BuildStatusText(FlowerTable table) {
            List<string> names = [];
            foreach (KeyValuePair<int, int> ingredient in table.Ingredients) {
                if (ingredient.Value <= 0) {
                    continue;
                }
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(ingredient.Key)];
                names.Add($"{block.GetDisplayName(SubsystemTerrain, ingredient.Key)}×{ingredient.Value}");
            }
            string water;
            if (table.HasWater) {
                water = LanguageControl.Get("FlowerTableMessages", "WaterYes");
            }
            else if (table.RainFill > 0f) {
                water = string.Format(
                    LanguageControl.Get("FlowerTableMessages", "WaterCollecting"),
                    (int)(table.RainFill / RainFillSeconds * 100f)
                );
            }
            else {
                water = LanguageControl.Get("FlowerTableMessages", "WaterNo");
            }
            string text = string.Format(
                LanguageControl.Get("FlowerTableMessages", "StatusFormat"),
                water,
                MathF.Round(table.ManaStorage.Current),
                MathF.Round(table.ManaStorage.Max),
                names.Count > 0 ? string.Join("、", names) : LanguageControl.Get("FlowerTableMessages", "Empty")
            );
            string hint = BuildCraftHint(table);
            if (!string.IsNullOrEmpty(hint)) {
                text += "｜" + hint;
            }
            return text;
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

        /// <summary>
        /// 合成进度提示：材料齐全 → 可投入种子；差一点 → 列出缺口；跑偏 → 提示取回。
        /// </summary>
        public string BuildCraftHint(FlowerTable table) {
            List<int> provided = [];
            foreach (KeyValuePair<int, int> ingredient in table.Ingredients) {
                for (int i = 0; i < ingredient.Value; i++) {
                    provided.Add(ingredient.Key);
                }
            }
            if (provided.Count == 0) {
                return LanguageControl.Get("FlowerTableMessages", "HintEmpty");
            }
            if (FlowerTableRecipeRegistry.TryMatch(provided, out FlowerRecipe exact)) {
                if (exact.ManaCost > 0f && table.ManaStorage.Current < exact.ManaCost) {
                    return string.Format(
                        LanguageControl.Get("FlowerTableMessages", "HintManaLow"),
                        MathF.Round(table.ManaStorage.Current),
                        MathF.Round(exact.ManaCost)
                    );
                }
                Block resultBlock = BlocksManager.Blocks[exact.ResultContents];
                string resultName = resultBlock.GetDisplayName(SubsystemTerrain, Terrain.MakeBlockValue(exact.ResultContents));
                return string.Format(LanguageControl.Get("FlowerTableMessages", "HintReady"), resultName);
            }
            if (FlowerTableRecipeRegistry.TryMatchClosest(provided, out FlowerRecipe closest, out List<KeyValuePair<FlowerRecipeIngredient, int>> missing)) {
                List<string> parts = [];
                foreach (KeyValuePair<FlowerRecipeIngredient, int> pair in missing) {
                    Block block = BlocksManager.Blocks[pair.Key.Contents];
                    int displayValue = Terrain.MakeBlockValue(
                        pair.Key.Contents,
                        0,
                        pair.Key.ExpectedData >= 0 ? pair.Key.ExpectedData : 0
                    );
                    string name = block.GetDisplayName(SubsystemTerrain, displayValue);
                    parts.Add($"{name}×{pair.Value}");
                }
                return string.Format(LanguageControl.Get("FlowerTableMessages", "HintMissing"), string.Join("、", parts));
            }
            return LanguageControl.Get("FlowerTableMessages", "HintNoMatch");
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