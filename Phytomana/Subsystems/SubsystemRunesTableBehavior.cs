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
    /// 符文台节点：逐位置状态（已放置材料 + 魔力）。
    /// </summary>
    public class RunesTable : IManaReceiver {
        public Point3 Position { get; }

        public ManaStorage ManaStorage { get; }

        /// <summary>已放置的材料：完整方块值 → 数量（点击放入，悬浮显示，不会消失）。</summary>
        public Dictionary<int, int> Items = [];

        /// <summary>就绪指示粒子的下次播报时间。</summary>
        public double NextIndicatorTime;

        public RunesTable(Point3 position, float maxMana) {
            Position = position;
            ManaStorage = new ManaStorage(maxMana);
        }
    }

    /// <summary>
    /// 符文台行为：与花药台类似但以「手持点击」放置材料（悬浮显示、不会消失），
    /// 材料与魔力齐备时冒白色粒子提示；投掷生息岩到祭坛上并用须弥法杖（工作模式）
    /// 右键即完成炼制。符文类原料在生存模式下不消耗，随成品一起返还；
    /// 创造模式下照常消耗。配方由外部 .rr 文件声明。
    /// </summary>
    public class SubsystemRunesTableBehavior : SubsystemBlockBehavior, IUpdateable {
        /// <summary>就绪指示粒子的播报间隔（秒）。</summary>
        public const float ReadyIndicatorInterval = 2.5f;

        public Dictionary<Point3, RunesTable> m_tables = [];

        public SubsystemParticles m_subsystemParticles;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemGameInfo m_subsystemGameInfo;

        public ManaNetworkManager m_network;

        public int m_grownStoneIndex;

        public List<IManaReceiver> m_receiverBuffer = [];

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<RunesTableBlock>()];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            m_grownStoneIndex = BlocksManager.GetBlockIndex<GrownStoneBlock>();
            // 存档格式：「x,y,z,魔力,物品值1,数量1,物品值2,数量2,...;」
            string text = valuesDictionary.GetValue("RunesTables", string.Empty);
            foreach (string entry in text.Split([';'], StringSplitOptions.RemoveEmptyEntries)) {
                string[] parts = entry.Split([','], StringSplitOptions.None);
                if (parts.Length < 4
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int z)
                    || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float mana)) {
                    continue;
                }
                RunesTable table = new(new Point3(x, y, z), MaxMana);
                table.ManaStorage.LoadData(mana);
                for (int i = 4; i + 1 < parts.Length; i += 2) {
                    if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                        && int.TryParse(parts[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)
                        && count > 0) {
                        table.Items[value] = count;
                    }
                }
                m_tables[new Point3(x, y, z)] = table;
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            StringBuilder stringBuilder = new();
            foreach (KeyValuePair<Point3, RunesTable> pair in m_tables) {
                RunesTable table = pair.Value;
                stringBuilder.Append(pair.Key.X.ToString(CultureInfo.InvariantCulture)).Append(',');
                stringBuilder.Append(pair.Key.Y.ToString(CultureInfo.InvariantCulture)).Append(',');
                stringBuilder.Append(pair.Key.Z.ToString(CultureInfo.InvariantCulture)).Append(',');
                stringBuilder.Append(table.ManaStorage.Current.ToString("R", CultureInfo.InvariantCulture));
                foreach (KeyValuePair<int, int> item in table.Items) {
                    stringBuilder.Append(',').Append(item.Key.ToString(CultureInfo.InvariantCulture));
                    stringBuilder.Append(',').Append(item.Value.ToString(CultureInfo.InvariantCulture));
                }
                stringBuilder.Append(';');
            }
            valuesDictionary.SetValue("RunesTables", stringBuilder.ToString());
        }

        public float MaxMana => ManaBlockRegistry.GetMaxMana("RunesTableBlock", PhytoConfig.Instance.RunesTableMaxMana);

        public void EnsureTable(int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (!m_tables.TryGetValue(point, out RunesTable table)) {
                table = new RunesTable(point, MaxMana);
                m_tables[point] = table;
            }
            // 网络侧按坐标去重并仅持弱引用，重复注册无副作用。
            m_network.RegisterReceiver(table);
        }

        /// <summary>方块渲染查询：按字典顺序返回祭坛上已放置物品的完整方块值（悬浮显示用）。</summary>
        public List<int> GetPlacedItemValues(int x, int y, int z) {
            List<int> list = [];
            if (m_tables.TryGetValue(new Point3(x, y, z), out RunesTable table)) {
                foreach (KeyValuePair<int, int> pair in table.Items) {
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
            if (!m_tables.TryGetValue(point, out RunesTable table)) {
                return;
            }
            m_tables.Remove(point);
            m_network.UnregisterReceiver(table, true);
            // 拆除祭坛时退回全部已放置材料，避免材料凭空消失。
            Vector3 center = new(x + 0.5f, y + 1f, z + 0.5f);
            foreach (KeyValuePair<int, int> item in table.Items) {
                if (item.Value > 0) {
                    m_subsystemPickables.AddPickable(item.Key, item.Value, center, new Vector3(0f, 2f, 0f), null);
                }
            }
        }

        public void Update(float dt) {
            foreach (RunesTable table in m_tables.Values) {
                if (SubsystemTerrain.Terrain.GetChunkAtCell(table.Position.X, table.Position.Z) == null) {
                    continue;
                }
                UpdateReadyIndicator(table);
            }
        }

        /// <summary>材料与魔力齐备时，周期性在祭坛上方冒白色粒子，提示「可以炼制」。</summary>
        public void UpdateReadyIndicator(RunesTable table) {
            double time = m_subsystemGameInfo.TotalElapsedGameTime;
            if (time < table.NextIndicatorTime) {
                return;
            }
            List<int> provided = BuildProvidedValues(table);
            if (provided.Count == 0
                || !RunesRecipeRegistry.TryMatch(provided, out RunesRecipe recipe)
                || table.ManaStorage.Current < recipe.ManaCost) {
                return;
            }
            table.NextIndicatorTime = time + ReadyIndicatorInterval;
            SpawnReadyParticle(table.Position);
        }

        public void SpawnReadyParticle(Point3 cell) {
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(cell.X + 0.5f, cell.Y + 1.2f, cell.Z + 0.5f),
                0.25f,
                1.5f,
                Color.White
            ));
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            Point3 point = raycastResult.CellFace.Point;
            if (!m_tables.TryGetValue(point, out RunesTable table)) {
                return false;
            }
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            int heldContents = Terrain.ExtractContents(componentMiner.ActiveBlockValue);
            // 生息法杖交给其工作模式处理（右键炼制）
            if (heldContents == BlocksManager.GetBlockIndex<GrownStaffBlock>()) {
                return false;
            }
            if (player == null) {
                return false;
            }
            ComponentBody body = player.Entity.FindComponent<ComponentBody>();
            bool crouching = body != null && body.IsCrouching;
            if (heldContents == 0) {
                if (crouching) {
                    return ReturnAllItems(table);
                }
                ShowStatus(player, table);
                return true;
            }
            if (heldContents == m_grownStoneIndex) {
                // 生息岩是炼制引子，须投掷到祭坛上，不能点击放置
                ShowMessage(player, "CatalystThrow");
                return true;
            }
            return PlaceItem(table, componentMiner, heldContents);
        }

        /// <summary>手持物品点击：放置一件到祭坛上（只收配方认识的材料，不超配方所需上限）。</summary>
        public bool PlaceItem(RunesTable table, ComponentMiner componentMiner, int heldContents) {
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            if (!RunesRecipeRegistry.IsKnownIngredient(heldContents)) {
                ShowMessage(player, "NotNeeded");
                return true;
            }
            int heldValue = componentMiner.ActiveBlockValue;
            int held = table.Items.GetValueOrDefault(heldValue);
            if (held >= RunesRecipeRegistry.MaxRequiredCount(heldContents)) {
                ShowMessage(player, "Enough");
                return true;
            }
            IInventory inventory = componentMiner.Inventory;
            int activeSlot = inventory.ActiveSlotIndex;
            inventory.RemoveSlotItems(activeSlot, 1);
            table.Items[heldValue] = held + 1;
            SpawnReadyParticle(table.Position);
            m_subsystemAudio.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f, 0f);
            return true;
        }

        /// <summary>空手潜行右键：取回全部已放置材料。</summary>
        public bool ReturnAllItems(RunesTable table) {
            Vector3 center = new(table.Position.X + 0.5f, table.Position.Y + 1.1f, table.Position.Z + 0.5f);
            foreach (KeyValuePair<int, int> item in table.Items) {
                if (item.Value > 0) {
                    m_subsystemPickables.AddPickable(item.Key, item.Value, center, new Vector3(0f, 2f, 0f), null);
                }
            }
            table.Items.Clear();
            return true;
        }

        public void ShowStatus(ComponentPlayer player, RunesTable table) {
            List<string> names = [];
            foreach (KeyValuePair<int, int> item in table.Items) {
                if (item.Value <= 0) {
                    continue;
                }
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(item.Key)];
                names.Add($"{block.GetDisplayName(SubsystemTerrain, item.Key)}×{item.Value}");
            }
            string text = string.Format(
                LanguageControl.Get("RunesTableMessages", "StatusFormat"),
                MathF.Round(table.ManaStorage.Current),
                MathF.Round(table.ManaStorage.Max),
                names.Count > 0 ? string.Join("、", names) : LanguageControl.Get("RunesTableMessages", "Empty")
            );
            string hint = BuildCraftHint(table);
            if (!string.IsNullOrEmpty(hint)) {
                text += "｜" + hint;
            }
            player.ComponentGui.DisplaySmallMessage(text, Color.White, false, false);
        }

        /// <summary>炼制进度提示：齐备 → 可投生息岩开炼；差材料 → 列缺口；魔力不足 → 提示。</summary>
        public string BuildCraftHint(RunesTable table) {
            List<int> provided = BuildProvidedValues(table);
            if (provided.Count == 0) {
                return LanguageControl.Get("RunesTableMessages", "HintEmpty");
            }
            if (RunesRecipeRegistry.TryMatch(provided, out RunesRecipe exact)) {
                if (table.ManaStorage.Current < exact.ManaCost) {
                    return string.Format(
                        LanguageControl.Get("RunesTableMessages", "HintManaLow"),
                        MathF.Round(table.ManaStorage.Current),
                        MathF.Round(exact.ManaCost)
                    );
                }
                Block resultBlock = BlocksManager.Blocks[exact.ResultContents];
                string resultName = resultBlock.GetDisplayName(SubsystemTerrain, Terrain.MakeBlockValue(exact.ResultContents, 0, exact.ResultData));
                return string.Format(LanguageControl.Get("RunesTableMessages", "HintReady"), resultName);
            }
            if (RunesRecipeRegistry.TryMatchClosest(provided, out RunesRecipe closest, out List<KeyValuePair<RunesRecipeIngredient, int>> missing)) {
                List<string> parts = [];
                foreach (KeyValuePair<RunesRecipeIngredient, int> pair in missing) {
                    Block block = BlocksManager.Blocks[pair.Key.Contents];
                    int displayValue = Terrain.MakeBlockValue(pair.Key.Contents, 0, pair.Key.ExpectedData >= 0 ? pair.Key.ExpectedData : 0);
                    parts.Add($"{block.GetDisplayName(SubsystemTerrain, displayValue)}×{pair.Value}");
                }
                return string.Format(LanguageControl.Get("RunesTableMessages", "HintMissing"), string.Join("、", parts));
            }
            return LanguageControl.Get("RunesTableMessages", "HintNoMatch");
        }

        public List<int> BuildProvidedValues(RunesTable table) {
            List<int> provided = [];
            foreach (KeyValuePair<int, int> item in table.Items) {
                for (int i = 0; i < item.Value; i++) {
                    provided.Add(item.Key);
                }
            }
            return provided;
        }

        /// <summary>
        /// 须弥法杖（工作模式）右键触发炼制：需要祭坛上有生息岩引子、
        /// 材料与某条配方一致、魔力足够。成功后弹出成品；生存模式下符文类原料
        /// 随成品一起返还；带返还物的原料（如水桶）弹出返还方块。
        /// </summary>
        public bool TryCraftByStaff(ComponentPlayer player, Point3 point) {
            if (!m_tables.TryGetValue(point, out RunesTable table)) {
                return false;
            }
            bool creative = m_subsystemGameInfo != null
                && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
            // 引子：祭坛上的一块生息岩
            Pickable catalyst = null;
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (Terrain.ExtractContents(pickable.Value) == m_grownStoneIndex
                    && IsPickableInCell(pickable, point)) {
                    catalyst = pickable;
                    break;
                }
            }
            if (catalyst == null) {
                ShowMessage(player, "MissingCatalyst");
                return true;
            }
            List<int> provided = BuildProvidedValues(table);
            if (provided.Count == 0
                || !RunesRecipeRegistry.TryMatch(provided, out RunesRecipe recipe)) {
                ShowMessage(player, "HintNoMatch");
                return true;
            }
            if (table.ManaStorage.Current < recipe.ManaCost) {
                ShowMessage(player, "ManaLow",
                    MathF.Round(table.ManaStorage.Current).ToString(CultureInfo.InvariantCulture),
                    MathF.Round(recipe.ManaCost).ToString(CultureInfo.InvariantCulture));
                return true;
            }
            // 结算：耗引子与魔力；消耗全部材料；生存模式下符文类原料随成品返还
            catalyst.Count = MathUtils.Max(catalyst.Count - 1, 0);
            if (catalyst.Count == 0) {
                catalyst.ToRemove = true;
            }
            table.ManaStorage.Take(recipe.ManaCost);
            Vector3 ejectCenter = new(point.X + 0.5f, point.Y + 1.3f, point.Z + 0.5f);
            foreach (KeyValuePair<int, int> item in table.Items) {
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(item.Key)];
                bool isRune = block is BasicRunesBlock or MediumRunesBlock or ExpertRunesBlock;
                if (isRune && !creative && item.Value > 0) {
                    // 符文不消耗：随成品一起返还
                    m_subsystemPickables.AddPickable(item.Key, item.Value, ejectCenter, new Vector3(0f, 2.5f, 0f), null);
                    continue;
                }
                // 带返还物的原料（如水桶→空桶）按消耗数量弹出返还方块
                foreach (RunesRecipeIngredient ingredient in recipe.Ingredients) {
                    if (ingredient.Contents == Terrain.ExtractContents(item.Key)
                        && ingredient.RemainContents >= 0
                        && item.Value > 0) {
                        m_subsystemPickables.AddPickable(
                            ingredient.RemainContents,
                            ingredient.Count,
                            ejectCenter,
                            new Vector3(0f, 2f, 0f),
                            null
                        );
                        break;
                    }
                }
            }
            table.Items.Clear();
            int resultValue = Terrain.MakeBlockValue(recipe.ResultContents, 0, recipe.ResultData);
            m_subsystemPickables.AddPickable(resultValue, recipe.ResultCount, ejectCenter, new Vector3(0f, 2.5f, 0f), null);
            SpawnReadyParticle(point);
            SpawnReadyParticle(point);
            m_subsystemAudio.PlaySound("Audio/PhytoMana/ding", 1f, 0f, 0f, 0f);
            ShowMessage(player, "Crafted");
            return true;
        }

        public void ShowMessage(ComponentPlayer player, string key) {
            player?.ComponentGui.DisplaySmallMessage(
                LanguageControl.Get("RunesTableMessages", key), Color.White, false, false);
        }

        public void ShowMessage(ComponentPlayer player, string key, string arg0, string arg1) {
            player?.ComponentGui.DisplaySmallMessage(
                string.Format(LanguageControl.Get("RunesTableMessages", key), arg0, arg1), Color.White, false, false);
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
    }
}