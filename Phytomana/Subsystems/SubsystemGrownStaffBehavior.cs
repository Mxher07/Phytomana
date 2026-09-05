using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using Phytomana;
using TemplatesDatabase;

namespace Game {
    public class SubsystemGrownStaffBehavior : SubsystemBlockBehavior, IUpdateable, IDrawable {
        public class StaffState {
            public ComponentPlayer Player;
            public bool WasHoldingStaff;
            public Point3? BindStart;
            public Point3? HoverCell;
        }

        public class PendingTransferParticle {
            public double Time;
            public Vector3 From;
            public Vector3 To;
        }

        public static readonly Color TianyiBlue = new(102, 204, 255);
        public static readonly Color LightPink = new(255, 179, 193);

        public SubsystemGameInfo m_subsystemGameInfo;
        public SubsystemTerrain m_subsystemTerrain;
        public SubsystemParticles m_subsystemParticles;
        public SubsystemMana m_subsystemMana;
        public FlowerTickScheduler m_flowerScheduler;
        public SubsystemAudio m_subsystemAudio;

        public PrimitivesRenderer3D m_primitivesRenderer3D = new();

        public int m_sunPowerIndex;
        public int m_waterDonIndex;
        public int m_spreaderIndex;
        public int m_manaPoolIndex;
        public int m_staffIndex;

        public Dictionary<PlayerData, StaffState> m_states = [];

        public List<PendingTransferParticle> m_pendingParticles = [];

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<GrownStaffBlock>()];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public int[] DrawOrders => [201];

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
            m_flowerScheduler = Project.FindSubsystem<FlowerTickScheduler>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_sunPowerIndex = BlocksManager.GetBlockIndex<SunPowerFlower>();
            m_waterDonIndex = BlocksManager.GetBlockIndex<WaterDonFlower>();
            m_spreaderIndex = BlocksManager.GetBlockIndex<ManaSpreaderBlock>();
            m_manaPoolIndex = BlocksManager.GetBlockIndex<ManaPoolBlock>();
            m_staffIndex = BlocksManager.GetBlockIndex<GrownStaffBlock>();
        }

        public void Update(float dt) {
            double time = m_subsystemGameInfo.TotalElapsedGameTime;
            UpdateLinkTransfers(dt);
            UpdatePendingParticles(time);
            foreach (StaffState state in m_states.Values) {
                UpdatePlayerState(state);
            }
        }

        public void UpdatePlayerState(StaffState state) {
            if (state.Player == null || state.Player.ComponentMiner == null) {
                state.WasHoldingStaff = false;
                state.HoverCell = null;
                return;
            }
            int activeValue = state.Player.ComponentMiner.ActiveBlockValue;
            bool holding = Terrain.ExtractContents(activeValue) == m_staffIndex;
            if (holding && !state.WasHoldingStaff) {
                ShowModeMessage(state.Player, false);
            }
            state.WasHoldingStaff = holding;
            if (!holding) {
                state.HoverCell = null;
                return;
            }
            int mode = Terrain.ExtractData(activeValue);
            if (mode == 0 && state.BindStart.HasValue) {
                state.BindStart = null;
            }
            Camera camera = state.Player.GameWidget.ActiveCamera;
            if (camera == null) {
                state.HoverCell = null;
                return;
            }
            Ray3 ray = new(camera.ViewPosition, camera.ViewDirection);
            state.HoverCell = null;
            TerrainRaycastResult? hit = state.Player.ComponentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Digging);
            if (!hit.HasValue) {
                return;
            }
            int contents = Terrain.ExtractContents(hit.Value.Value);
            if (IsStaffCheckable(contents)) {
                state.HoverCell = hit.Value.CellFace.Point;
            }
        }

        public void UpdateLinkTransfers(float dt) {
            foreach (ManaLink link in m_subsystemMana.m_links) {
                link.TransferAccumulator += dt;
                while (link.TransferAccumulator >= SubsystemMana.StaffLinkTransferPeriod) {
                    link.TransferAccumulator -= SubsystemMana.StaffLinkTransferPeriod;
                    Point3 from = link.From;
                    Point3 to = link.To;
                    if (m_subsystemTerrain.Terrain.GetCellContents(from) != m_spreaderIndex
                        || !m_subsystemMana.IsManaStorage(m_subsystemTerrain.Terrain.GetCellContents(to))) {
                        continue;
                    }
                    int toContents = m_subsystemTerrain.Terrain.GetCellContents(to);
                    float maxTarget = m_subsystemMana.GetMaxManaAmount(toContents);
                    if (m_subsystemMana.GetManaAmount(from) >= SubsystemMana.StaffLinkTransferAmount
                        && maxTarget - m_subsystemMana.GetManaAmount(to) >= SubsystemMana.StaffLinkTransferAmount) {
                        m_subsystemMana.RemoveMana(from, SubsystemMana.StaffLinkTransferAmount);
                        m_subsystemMana.AddMana(to, SubsystemMana.StaffLinkTransferAmount);
                        QueueTransferParticles(from, to);
                    }
                }
            }
            m_subsystemMana.PruneLinks();
        }

        public void UpdatePendingParticles(double time) {
            for (int i = m_pendingParticles.Count - 1; i >= 0; i--) {
                PendingTransferParticle pending = m_pendingParticles[i];
                if (time >= pending.Time) {
                    m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                        pending.From,
                        0.75f,
                        1.8f,
                        Color.Green,
                        pending.To,
                        1
                    ));
                    m_pendingParticles.RemoveAt(i);
                }
            }
        }

        public void QueueTransferParticles(Point3 from, Point3 to) {
            Vector3 startPos = new(from.X + 0.5f, from.Y + 0.5f, from.Z + 0.5f);
            Vector3 endPos = new(to.X + 0.5f, to.Y + 0.5f, to.Z + 0.5f);
            double now = m_subsystemGameInfo.TotalElapsedGameTime;
            for (int i = 0; i < 6; i++) {
                float t = i / 6f;
                m_pendingParticles.Add(new PendingTransferParticle {
                    Time = now + t * SubsystemMana.StaffLinkTransferPeriod,
                    From = Vector3.Lerp(startPos, endPos, t),
                    To = endPos
                });
            }
        }

        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) {
            int staffValue = componentMiner.ActiveBlockValue;
            if (Terrain.ExtractContents(staffValue) != m_staffIndex) {
                return false;
            }
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            if (player == null) {
                return false;
            }
            StaffState state = GetState(player);
            ComponentBody body = player.Entity.FindComponent<ComponentBody>();
            if (body != null && body.IsCrouching) {
                ToggleMode(player, staffValue);
                return true;
            }
            int mode = Terrain.ExtractData(staffValue);
            TerrainRaycastResult? hit = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Digging);
            if (!hit.HasValue) {
                return false;
            }
            Point3 point = hit.Value.CellFace.Point;
            int contents = Terrain.ExtractContents(hit.Value.Value);
            if (mode == 1) {
                return HandleBindingClick(player, state, point, contents);
            }
            if (ShowFlowerStatus(player, point, contents)) {
                return true;
            }
            if (contents == m_spreaderIndex) {
                ShowSpreaderStatus(player, point);
                return true;
            }
            if (IsStaffCheckable(contents)) {
                ShowStorageStatus(player, point, contents);
                return true;
            }
            return false;
        }

        public bool HandleBindingClick(ComponentPlayer player, StaffState state, Point3 point, int contents) {
            if (m_flowerScheduler.TryGetFlower(point, out TilePhytoFlower _)) {
                return true;
            }
            if (!m_subsystemMana.IsManaStorage(contents)) {
                if (state.BindStart.HasValue) {
                    ShowMessage(player, "ErrNoManaCapability", Color.Red, true);
                }
                return state.BindStart.HasValue;
            }
            if (!state.BindStart.HasValue) {
                if (contents == m_manaPoolIndex) {
                    return true;
                }
                state.BindStart = point;
                ShowMessage(player, "StartSelected", Color.White, true, point.X, point.Y, point.Z);
                return true;
            }
            Point3 from = state.BindStart.Value;
            if (from == point) {
                state.BindStart = null;
                ShowMessage(player, "StartCancelled", Color.White, true);
                return true;
            }
            float distance = Vector3.Distance(new Vector3(from.X, from.Y, from.Z), new Vector3(point.X, point.Y, point.Z));
            if (distance > 16f) {
                state.BindStart = from;
                ShowMessage(player, "ErrTooFar", Color.Red, true);
                return true;
            }
            Point3 delta = new(point.X - from.X, point.Y - from.Y, point.Z - from.Z);
            int axisCount = (delta.X != 0 ? 1 : 0) + (delta.Y != 0 ? 1 : 0) + (delta.Z != 0 ? 1 : 0);
            if (axisCount != 1) {
                state.BindStart = from;
                ShowMessage(player, "ErrWrongDirection", Color.Red, true);
                return true;
            }
            int face = FaceFromDirection(delta);
            m_subsystemTerrain.ChangeCell(
                from.X,
                from.Y,
                from.Z,
                Terrain.ReplaceData(m_subsystemTerrain.Terrain.GetCellValue(from.X, from.Y, from.Z), face),
                true,
                null
            );
            m_subsystemMana.AddLink(from, point);
            state.BindStart = null;
            ShowMessage(player, "BindSuccess", Color.Green, false, from.X, from.Y, from.Z, point.X, point.Y, point.Z);
            return true;
        }

        public void ToggleMode(ComponentPlayer player, int staffValue) {
            int newData = Terrain.ExtractData(staffValue) == 1 ? 0 : 1;
            int newValue = Terrain.ReplaceData(staffValue, newData);
            ComponentMiner miner = player.ComponentMiner;
            if (miner == null || miner.Inventory == null) {
                return;
            }
            miner.Inventory.RemoveSlotItems(miner.Inventory.ActiveSlotIndex, 1);
            miner.Inventory.AddSlotItems(miner.Inventory.ActiveSlotIndex, newValue, 1);
            StaffState state = GetState(player);
            if (newData == 0) {
                state.BindStart = null;
            }
            ShowModeMessage(player, true);
        }

        public void ShowModeMessage(ComponentPlayer player, bool isSwitch) {
            int activeValue = player.ComponentMiner != null ? player.ComponentMiner.ActiveBlockValue : 0;
            int mode = Terrain.ExtractContents(activeValue) == m_staffIndex ? Terrain.ExtractData(activeValue) : 0;
            string modeName = LanguageControl.Get("GrownStaffMessages", mode == 1 ? "ModeBind" : "ModeWork");
            string message = string.Format(
                LanguageControl.Get("GrownStaffMessages", isSwitch ? "ModeChanged" : "ModeEquip"),
                modeName
            );
            player.ComponentGui.DisplaySmallMessage(message, Color.White, false, false);
            m_subsystemAudio.PlaySound("Audio/PhytoMana/ding", 1f, 0f, 0f, 0f);
        }

        public bool ShowFlowerStatus(ComponentPlayer player, Point3 point, int contents) {
            if (!m_flowerScheduler.TryGetFlower(point, out TilePhytoFlower flower) || flower is not TileGeneratingFlower generating) {
                return false;
            }
            float max = m_subsystemMana.GetMaxManaAmount(contents);
            float current = m_subsystemMana.GetManaAmount(point);
            bool working = generating.IsProducing;
            bool draining = generating.IsLosingMana;
            string status = draining
                ? LanguageControl.Get("GrownStaffMessages", "StatusDraining")
                : working
                    ? LanguageControl.Get("GrownStaffMessages", "StatusWorking")
                    : LanguageControl.Get("GrownStaffMessages", "StatusIdle");
            string value = draining ? null : FormatMana(generating.GetProductionRate());
            string name = GetBlockName(point);
            string message;
            if (value == null) {
                message = string.Format(
                    LanguageControl.Get("GrownStaffMessages", "StatusFormatDraining"),
                    name,
                    FormatMana(current),
                    FormatMana(max),
                    status
                );
            }
            else {
                message = string.Format(
                    LanguageControl.Get("GrownStaffMessages", "StatusFormat"),
                    name,
                    FormatMana(current),
                    FormatMana(max),
                    status,
                    value
                );
            }
            player.ComponentGui.DisplaySmallMessage(message, Color.Green, false, false);
            m_subsystemAudio.PlaySound("Audio/PhytoMana/ding", 1f, 0f, 0f, 0f);
            return true;
        }

        public void ShowSpreaderStatus(ComponentPlayer player, Point3 point) {
            float max = m_subsystemMana.GetMaxManaAmount(m_spreaderIndex);
            float current = m_subsystemMana.GetManaAmount(point);
            float usage = m_subsystemMana.GetOutgoingUsage(point);
            string status = current > 0f || usage > 0f
                ? LanguageControl.Get("GrownStaffMessages", "StatusWorking")
                : LanguageControl.Get("GrownStaffMessages", "StatusIdle");
            string message = string.Format(
                LanguageControl.Get("GrownStaffMessages", "SpreadStatusFormat"),
                GetBlockName(point),
                FormatMana(current),
                FormatMana(max),
                status,
                FormatMana(usage)
            );
            player.ComponentGui.DisplaySmallMessage(message, Color.Green, false, false);
            m_subsystemAudio.PlaySound("Audio/PhytoMana/ding", 1f, 0f, 0f, 0f);
        }

        public void ShowStorageStatus(ComponentPlayer player, Point3 point, int contents) {
            float max = m_subsystemMana.GetMaxManaAmount(contents);
            float current = m_subsystemMana.GetManaAmount(point);
            string status = current > 0f
                ? LanguageControl.Get("GrownStaffMessages", "StatusWorking")
                : LanguageControl.Get("GrownStaffMessages", "StatusIdle");
            string message = string.Format(
                LanguageControl.Get("GrownStaffMessages", "StorageStatusFormat"),
                GetBlockName(point),
                FormatMana(current),
                FormatMana(max),
                status
            );
            player.ComponentGui.DisplaySmallMessage(message, Color.Green, false, false);
            m_subsystemAudio.PlaySound("Audio/PhytoMana/ding", 1f, 0f, 0f, 0f);
        }

        /// <summary>
        /// 法杖可否检查该方块状态：优先取魔力方块注册表（数据文件驱动），无条目时回退到既有方块集合。
        /// </summary>
        public bool IsStaffCheckable(int contents) {
            if (ManaBlockRegistry.TryGet(contents, out ManaBlockDefinition definition)) {
                return definition.StaffCheckable;
            }
            return contents == m_sunPowerIndex
                || contents == m_waterDonIndex
                || contents == m_spreaderIndex
                || contents == m_manaPoolIndex;
        }

        public static int FaceFromDirection(Point3 delta) {
            if (delta.X > 0) return 1;
            if (delta.X < 0) return 3;
            if (delta.Y > 0) return 4;
            if (delta.Y < 0) return 5;
            if (delta.Z > 0) return 0;
            return 2;
        }

        public string GetBlockName(Point3 point) {
            int value = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
            return BlocksManager.Blocks[Terrain.ExtractContents(value)].GetDisplayName(m_subsystemTerrain, value);
        }

        public static string FormatMana(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        public void ShowMessage(ComponentPlayer player, string key, Color color, bool withSound, params object[] args) {
            string text = LanguageControl.Get("GrownStaffMessages", key);
            if (args.Length > 0) {
                text = string.Format(text, args);
            }
            player.ComponentGui.DisplaySmallMessage(text, color, false, false);
            m_subsystemAudio.PlaySound("Audio/PhytoMana/ding", 1f, 0f, 0f, 0f);
        }

        public StaffState GetState(ComponentPlayer player) {
            if (player.PlayerData == null) {
                return new StaffState { Player = player };
            }
            if (!m_states.TryGetValue(player.PlayerData, out StaffState state)) {
                state = new StaffState { Player = player };
                m_states[player.PlayerData] = state;
            }
            return state;
        }

        public void Draw(Camera camera, int drawOrder) {
            FlatBatch3D batch = null;
            foreach (StaffState state in m_states.Values) {
                if (state.Player == null
                    || camera.GameWidget.PlayerData != state.Player.PlayerData) {
                    continue;
                }
                if (!state.HoverCell.HasValue && !state.BindStart.HasValue) {
                    continue;
                }
                if (batch == null) {
                    batch = m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None);
                }
                if (state.HoverCell.HasValue) {
                    QueueCellBox(batch, state.HoverCell.Value, TianyiBlue);
                }
                if (state.BindStart.HasValue) {
                    QueueCellBox(batch, state.BindStart.Value, LightPink);
                }
            }
            if (batch != null) {
                batch.Flush(camera.ViewProjectionMatrix);
            }
        }

        public static void QueueCellBox(FlatBatch3D batch, Point3 point, Color color) {
            Vector3 min = new(point.X, point.Y, point.Z);
            batch.QueueBoundingBox(new BoundingBox(min, min + Vector3.One), color);
        }
    }
}