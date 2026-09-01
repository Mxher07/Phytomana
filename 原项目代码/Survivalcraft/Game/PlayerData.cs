using System.Globalization;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class PlayerData : IDisposable {
        public enum SpawnMode {
            InitialIntro = 0,
            InitialNoIntro = 1,
            Respawn = 2
        }

        public static string fName = "PlayerData";

        public Project m_project;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemSky m_subsystemSky;

        public GameWidget m_gameWidget;

        public StateMachine m_stateMachine = new();

        public PlayerClass m_playerClass;

        public string m_name;

        public SpawnMode m_spawnMode;

        public double? m_playerDeathTime;

        public double m_terrainWaitStartTime;
        public double TerrainMaxWaitTime { get; set; } = 15;

        public SpawnDialog m_spawnDialog;

        public float m_progress;
        public int PlayerIndex { get; set; }

        public SubsystemGameWidgets SubsystemGameWidgets { get; set; }

        public SubsystemPlayers SubsystemPlayers { get; set; }

        public ComponentPlayer ComponentPlayer { get; set; }

        public Entity LastDeadPlayer { get; set; }

        public GameWidget GameWidget {
            get {
                if (m_gameWidget == null) {
                    foreach (GameWidget gameWidget in SubsystemGameWidgets.GameWidgets) {
                        if (gameWidget.PlayerData == this) {
                            m_gameWidget = gameWidget;
                            break;
                        }
                    }
                    if (m_gameWidget == null) {
                        throw new InvalidOperationException(LanguageControl.Get(fName, 11));
                    }
                }
                return m_gameWidget;
            }
        }

        public Vector3 SpawnPosition { get; set; }

        public double FirstSpawnTime { get; set; }

        public double LastSpawnTime { get; set; }

        public int SpawnsCount { get; set; }

        public string Name {
            get => m_name;
            set {
                if (value != m_name) {
                    m_name = value;
                    IsDefaultName = false;
                }
            }
        }

        public bool IsDefaultName { get; set; }

        public PlayerClass PlayerClass {
            get => m_playerClass;
            set {
                if (SubsystemPlayers.PlayersData.Contains(this)) {
                    throw new InvalidOperationException(LanguageControl.Get(fName, 1));
                }
                m_playerClass = value;
            }
        }

        public float Level { get; set; }
        public bool ReduceLevelUponDeath = true;
        public string CharacterSkinName { get; set; }

        public WidgetInputDevice InputDevice { get; set; }

        public bool IsReadyForPlaying {
            get {
                if (m_stateMachine.CurrentState != "Playing") {
                    return m_stateMachine.CurrentState == "PlayerDead";
                }
                return true;
            }
        }

        public double m_initialSpawnWaitTime = 0;
        public double m_respawnWaitTime = 2.0;

        public bool IsTimeReadyToSpawn {
            get {
                if (m_spawnMode == SpawnMode.Respawn) {
                    return Time.FrameStartTime - m_terrainWaitStartTime > m_respawnWaitTime;
                }
                return Time.FrameStartTime - m_terrainWaitStartTime > m_initialSpawnWaitTime;
            }
        }

        public PlayerData() { }

        public PlayerData(Project project) {
            m_project = project;
            SubsystemPlayers = project.FindSubsystem<SubsystemPlayers>(true);
            SubsystemGameWidgets = project.FindSubsystem<SubsystemGameWidgets>(true);
            m_subsystemTerrain = project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemGameInfo = project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemSky = project.FindSubsystem<SubsystemSky>(true);
            m_playerClass = PlayerClass.Male;
            Level = 1f;
            FirstSpawnTime = -1.0;
            LastSpawnTime = -1.0;
            RandomizeCharacterSkin();
            ResetName();
            InputDevice = WidgetInputDevice.None;
            m_stateMachine.AddState(
                "FirstUpdate",
                null,
                delegate {
                    if (ComponentPlayer != null) {
                        UpdateSpawnDialog(string.Format(LanguageControl.Get(fName, 4), Name, MathF.Floor(Level)), null, 0f, true);
                        m_stateMachine.TransitionTo("WaitForTerrain");
                    }
                    else {
                        m_stateMachine.TransitionTo("PrepareSpawn");
                    }
                    ModsManager.HookAction(
                        "PlayerDataFirstUpdate",
                        loader => {
                            loader.PlayerDataFirstUpdate(this);
                            return false;
                        }
                    );
                },
                null
            );
            m_stateMachine.AddState(
                "PrepareSpawn",
                delegate {
                    if (SpawnPosition == Vector3.Zero) {
                        if (SubsystemPlayers.GlobalSpawnPosition == Vector3.Zero) {
                            PlayerData playerData = SubsystemPlayers.PlayersData.FirstOrDefault(pd => pd.SpawnPosition != Vector3.Zero);
                            if (playerData != null) {
                                if (playerData.ComponentPlayer != null) {
                                    SpawnPosition = playerData.ComponentPlayer.ComponentBody.Position;
                                    m_spawnMode = SpawnMode.InitialNoIntro;
                                }
                                else {
                                    SpawnPosition = playerData.SpawnPosition;
                                    m_spawnMode = SpawnMode.InitialNoIntro;
                                }
                            }
                            else {
                                SpawnPosition = m_subsystemTerrain.TerrainContentsGenerator.FindCoarseSpawnPosition();
                                m_spawnMode = SpawnMode.InitialIntro;
                            }
                            SubsystemPlayers.GlobalSpawnPosition = SpawnPosition;
                        }
                        else {
                            SpawnPosition = SubsystemPlayers.GlobalSpawnPosition;
                            m_spawnMode = SpawnMode.InitialNoIntro;
                        }
                    }
                    else {
                        m_spawnMode = SpawnMode.Respawn;
                    }
                    if (m_spawnMode == SpawnMode.Respawn) {
                        UpdateSpawnDialog(
                            string.Format(LanguageControl.Get(fName, 2), Name, MathF.Floor(Level)),
                            LanguageControl.Get(fName, 3),
                            0f,
                            true
                        );
                    }
                    else {
                        UpdateSpawnDialog(string.Format(LanguageControl.Get(fName, 4), Name, MathF.Floor(Level)), null, 0f, true);
                    }
                    m_subsystemTerrain.TerrainUpdater.SetUpdateLocation(PlayerIndex, SpawnPosition.XZ, 0f, 64f);
                    m_terrainWaitStartTime = Time.FrameStartTime;
                },
                delegate {
                    if (Time.PeriodicEvent(0.1, 0.0)) {
                        float updateProgress2 = m_subsystemTerrain.TerrainUpdater.GetUpdateProgress(PlayerIndex, 0f, 64f);
                        UpdateSpawnDialog(null, null, 0.5f * updateProgress2, false);
                        if (!(updateProgress2 < 1f)
                            || !(Time.FrameStartTime - m_terrainWaitStartTime < TerrainMaxWaitTime)) {
                            switch (m_spawnMode) {
                                case SpawnMode.InitialIntro: SpawnPosition = FindIntroSpawnPosition(SpawnPosition.XZ); break;
                                case SpawnMode.InitialNoIntro: SpawnPosition = FindNoIntroSpawnPosition(SpawnPosition, false); break;
                                case SpawnMode.Respawn: SpawnPosition = FindNoIntroSpawnPosition(SpawnPosition, true); break;
                                default: throw new InvalidOperationException(LanguageControl.Get(fName, 5));
                            }
                            m_stateMachine.TransitionTo("WaitForTerrain");
                        }
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "WaitForTerrain",
                delegate {
                    m_terrainWaitStartTime = Time.FrameStartTime;
                    Vector2 center = ComponentPlayer != null ? ComponentPlayer.ComponentBody.Position.XZ : SpawnPosition.XZ;
                    m_subsystemTerrain.TerrainUpdater.SetUpdateLocation(PlayerIndex, center, MathUtils.Min(m_subsystemSky.VisibilityRange, 64f), 0f);
                },
                delegate {
                    if (Time.PeriodicEvent(0.1, 0.0)) {
                        float updateProgress = m_subsystemTerrain.TerrainUpdater.GetUpdateProgress(
                            PlayerIndex,
                            MathUtils.Min(m_subsystemSky.VisibilityRange, 64f),
                            0f
                        );
                        UpdateSpawnDialog(null, null, 0.5f + 0.5f * updateProgress, false);
                        if ((updateProgress >= 1f && IsTimeReadyToSpawn)
                            || Time.FrameStartTime - m_terrainWaitStartTime >= 15.0) {
                            if (ComponentPlayer == null) {
                                try {
                                    SpawnPlayer(SpawnPosition, m_spawnMode);
                                    SubsystemPlayers.PlayerStartedPlaying = true;
                                }
                                catch (Exception ex) {
                                    Log.Error("Spawning Player Error!");
                                    Log.Error(ex);
                                    ScreensManager.SwitchScreen(ScreensManager.FindScreen<PlayScreen>("Play"));
                                    ViewGameLogDialog dialog = new();
                                    dialog.SetErrorHead(12, 10);
                                    DialogsManager.ShowDialog(null, dialog);
                                    GameManager.DisposeProject();
                                }
                            }
                            if (m_playerDeathTime.HasValue
                                || ComponentPlayer?.ComponentHealth.Health <= 0f) {
                                m_playerDeathTime = Time.RealTime;
                                m_stateMachine.TransitionTo("PlayerDead");
                            }
                            else {
                                m_stateMachine.TransitionTo("Playing");
                            }
                        }
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Playing",
                HideSpawnDialog,
                delegate {
                    if (ComponentPlayer == null) {
                        m_stateMachine.TransitionTo("PrepareSpawn");
                    }
                    else if (m_playerDeathTime.HasValue) {
                        m_stateMachine.TransitionTo("PlayerDead");
                    }
                    else {
                        lock (ComponentPlayer.ComponentHealth) {
                            if (ComponentPlayer.ComponentHealth.Health <= 0f) {
                                m_playerDeathTime = Time.RealTime;
                            }
                        }
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "PlayerDead",
                delegate {
                    HideSpawnDialog();
                    ModsManager.HookAction(
                        "OnPlayerDead",
                        modLoader => {
                            modLoader.OnPlayerDead(this);
                            return false;
                        }
                    );
                    if (ReduceLevelUponDeath && m_stateMachine.PreviousState == "Playing") {
                        Level = MathUtils.Max(MathF.Floor(Level / 2f), 1f);
                    }
                },
                delegate {
                    if (ComponentPlayer == null) {
                        m_stateMachine.TransitionTo("PrepareSpawn");
                        return;
                    }
                    bool respawn = false;
                    bool disableVanillaTapToRespawnAction = false;
                    ModsManager.HookAction(
                        "UpdateDeathCameraWidget",
                        loader => {
                            // ReSharper disable AccessToModifiedClosure
                            loader.UpdateDeathCameraWidget(this, ref disableVanillaTapToRespawnAction, ref respawn);
                            // ReSharper restore AccessToModifiedClosure
                            return false;
                        }
                    );
                    if (!disableVanillaTapToRespawnAction
                        && Time.RealTime - (m_playerDeathTime ?? 0) > 1.5
                        && !DialogsManager.HasDialogs(ComponentPlayer.GuiWidget)
                        && ComponentPlayer.GameWidget.Input.Any) {
                        if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Cruel) {
                            DialogsManager.ShowDialog(ComponentPlayer.GuiWidget, new GameMenuDialog(ComponentPlayer));
                        }
                        else if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure
                            && !m_subsystemGameInfo.WorldSettings.IsAdventureRespawnAllowed) {
                            ScreensManager.SwitchScreen("GameLoading", GameManager.WorldInfo, "AdventureRestart");
                        }
                        else {
                            respawn = true;
                        }
                    }
                    if (respawn) {
                        LastDeadPlayer = ComponentPlayer.Entity;
                        ComponentPlayer = null;
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("FirstUpdate");
        }

        public virtual void Dispose() {
            HideSpawnDialog();
        }

        public virtual void RandomizeCharacterSkin() {
            CharacterSkinsManager.UpdateCharacterSkinsList();
            if (CharacterSkinsManager.UseEmptySkinAsDefault) {
                CharacterSkinName = "$Empty";
                return;
            }
            Random random = new();
            string[] array = CharacterSkinsManager.CharacterSkinsNames
                .Where(n => CharacterSkinsManager.IsBuiltIn(n) && CharacterSkinsManager.GetPlayerClass(n) == m_playerClass)
                .ToArray();
            string[] second = SubsystemPlayers.PlayersData.Select(pd => pd.CharacterSkinName).ToArray();
            string[] array2 = array.Except(second).ToArray();
            CharacterSkinName = array2.Length != 0 ? array2[random.Int(0, array2.Length - 1)] : array[random.Int(0, array.Length - 1)];
        }

        public virtual void ResetName() {
            m_name = CharacterSkinsManager.GetDisplayName(CharacterSkinName);
            IsDefaultName = true;
        }

        public static bool VerifyName(string name) => name.Length >= 1;

        public virtual void Update() {
            m_stateMachine.Update();
        }

        public virtual void Load(ValuesDictionary valuesDictionary) {
            SpawnPosition = valuesDictionary.GetValue("SpawnPosition", Vector3.Zero);
            FirstSpawnTime = valuesDictionary.GetValue("FirstSpawnTime", 0.0);
            LastSpawnTime = valuesDictionary.GetValue("LastSpawnTime", 0.0);
            SpawnsCount = valuesDictionary.GetValue("SpawnsCount", 0);
            Name = valuesDictionary.GetValue("Name", "Walter");
            PlayerClass = valuesDictionary.GetValue("PlayerClass", PlayerClass.Male);
            Level = valuesDictionary.GetValue("Level", 1f);
            CharacterSkinName = valuesDictionary.GetValue("CharacterSkinName", CharacterSkinsManager.CharacterSkinsNames[0]);
            InputDevice = valuesDictionary.GetValue("InputDevice", InputDevice);
        }

        public virtual void Save(ValuesDictionary valuesDictionary) {
            valuesDictionary.SetValue("SpawnPosition", SpawnPosition);
            valuesDictionary.SetValue("FirstSpawnTime", FirstSpawnTime);
            valuesDictionary.SetValue("LastSpawnTime", LastSpawnTime);
            valuesDictionary.SetValue("SpawnsCount", SpawnsCount);
            valuesDictionary.SetValue("Name", Name);
            valuesDictionary.SetValue("PlayerClass", PlayerClass);
            valuesDictionary.SetValue("Level", Level);
            valuesDictionary.SetValue("CharacterSkinName", CharacterSkinName);
            valuesDictionary.SetValue("InputDevice", InputDevice);
        }

        public virtual void OnEntityAdded(Entity entity) {
            ComponentPlayer componentPlayer = entity.FindComponent<ComponentPlayer>();
            if (componentPlayer != null
                && componentPlayer.PlayerData == this) {
                //多维世界mod会提前为PlayerData.ComponentPlayer赋值，原版行为会在SubsystemPlayers.OnEntityAdded再次赋值
                //为阻止再次赋值时引发异常所以加了条ComponentPlayer != componentPlayer判断
                if (ComponentPlayer != null && ComponentPlayer != componentPlayer) {
                    throw new InvalidOperationException(string.Format(LanguageControl.Get(fName, 10), PlayerIndex));
                }
                ComponentPlayer = componentPlayer;
                GameWidget.ActiveCamera = GameWidget.FindCamera<FppCamera>();
                GameWidget.Target = componentPlayer;
                if (FirstSpawnTime < 0.0) {
                    FirstSpawnTime = m_subsystemGameInfo.TotalElapsedGameTime;
                }
            }
        }

        public virtual void OnEntityRemoved(Entity entity) { }

        public virtual Vector3 FindIntroSpawnPosition(Vector2 desiredSpawnPosition) {
            Vector2 vector = Vector2.Zero;
            float num = float.MinValue;
            for (int i = -30; i <= 30; i += 2) {
                for (int j = -30; j <= 30; j += 2) {
                    int num2 = Terrain.ToCell(desiredSpawnPosition.X) + i;
                    int num3 = Terrain.ToCell(desiredSpawnPosition.Y) + j;
                    float num4 = ScoreIntroSpawnPosition(desiredSpawnPosition, num2, num3);
                    if (num4 > num) {
                        num = num4;
                        vector = new Vector2(num2, num3);
                    }
                }
            }
            float num5 = m_subsystemTerrain.Terrain.CalculateTopmostCellHeight(Terrain.ToCell(vector.X), Terrain.ToCell(vector.Y)) + 1;
            return new Vector3(vector.X + 0.5f, num5 + 0.01f, vector.Y + 0.5f);
        }

        public virtual Vector3 FindNoIntroSpawnPosition(Vector3 desiredSpawnPosition, bool respawn) {
            Vector3 vector = Vector3.Zero;
            float num = float.MinValue;
            for (int i = -8; i <= 8; i++) {
                for (int j = -8; j <= 8; j++) {
                    for (int k = -8; k <= 8; k++) {
                        int num2 = Terrain.ToCell(desiredSpawnPosition.X) + i;
                        int num3 = Terrain.ToCell(desiredSpawnPosition.Y) + j;
                        int num4 = Terrain.ToCell(desiredSpawnPosition.Z) + k;
                        float num5 = ScoreNoIntroSpawnPosition(desiredSpawnPosition, num2, num3, num4);
                        if (num5 > num) {
                            num = num5;
                            vector = new Vector3(num2, num3, num4);
                        }
                    }
                }
            }
            return new Vector3(vector.X + 0.5f, vector.Y + 0.01f, vector.Z + 0.5f);
        }

        public virtual float ScoreIntroSpawnPosition(Vector2 desiredSpawnPosition, int x, int z) {
            float num = -0.01f * Vector2.Distance(new Vector2(x, z), desiredSpawnPosition);
            int num2 = m_subsystemTerrain.Terrain.CalculateTopmostCellHeight(x, z);
            if (num2 < 64
                || num2 > 85) {
                num -= 5f;
            }
            if (m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) < 8) {
                num -= 5f;
            }
            int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, num2, z);
            if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsTransparent_(cellValue)) {
                num -= 5f;
            }
            for (int i = x - 1; i <= x + 1; i++) {
                for (int j = z - 1; j <= z + 1; j++) {
                    if (m_subsystemTerrain.Terrain.GetCellContents(i, num2 + 2, j) != 0) {
                        num -= 1f;
                    }
                }
            }
            Vector2 vector = ComponentIntro.FindOceanDirection(m_subsystemTerrain.TerrainContentsGenerator, new Vector2(x, z));
            Vector3 vector2 = new(x, num2 + 1.5f, z);
            for (int k = -1; k <= 1; k++) {
                Vector3 end = vector2 + new Vector3(30f * vector.X, 5f * k, 30f * vector.Y);
                TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(
                    vector2,
                    end,
                    false,
                    true,
                    (value, _) => Terrain.ExtractContents(value) != 0
                );
                if (terrainRaycastResult.HasValue) {
                    CellFace cellFace = terrainRaycastResult.Value.CellFace;
                    int cellContents2 = m_subsystemTerrain.Terrain.GetCellContents(cellFace.X, cellFace.Y, cellFace.Z);
                    if (cellContents2 != 18
                        && cellContents2 != 0) {
                        num -= 2f;
                    }
                }
            }
            return num;
        }

        public virtual float ScoreNoIntroSpawnPosition(Vector3 desiredSpawnPosition, int x, int y, int z) {
            float num = -0.01f * Vector3.Distance(new Vector3(x, y, z), desiredSpawnPosition);
            if (y < 1
                || y >= 255) {
                num -= 100f;
            }
            int objvalue = m_subsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
            int blockvalue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
            int block2value = m_subsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
            Block obj = BlocksManager.Blocks[Terrain.ExtractContents(objvalue)];
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(blockvalue)];
            Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(block2value)];
            if (obj.IsTransparent_(objvalue)) {
                num -= 10f;
            }
            if (!obj.IsCollidable_(objvalue)) {
                num -= 10f;
            }
            if (block.IsCollidable_(blockvalue)) {
                num -= 10f;
            }
            if (block2.IsCollidable_(block2value)) {
                num -= 10f;
            }
            foreach (PlayerData playersDatum in SubsystemPlayers.PlayersData) {
                if (playersDatum != this
                    && Vector3.DistanceSquared(playersDatum.SpawnPosition, new Vector3(x, y, z)) < MathUtils.Sqr(2)) {
                    num -= 1f;
                }
            }
            return num;
        }

        public virtual bool CheckIsPointInWater(Point3 p) {
            for (int i = p.X - 1; i < p.X + 1; i++) {
                for (int j = p.Z - 1; j < p.Z + 1; j++) {
                    for (int num = p.Y; num > 0; num--) {
                        int cellValue = m_subsystemTerrain.Terrain.GetCellValue(p.X, num, p.Z);
                        Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
                        if (block.IsCollidable_(cellValue)) {
                            return false;
                        }
                        if (block is WaterBlock) {
                            break;
                        }
                    }
                }
            }
            return true;
        }

        public virtual void SpawnPlayer(Vector3 position, SpawnMode spawnMode) {
            if (LastDeadPlayer != null) {
                m_project.RemoveEntity(LastDeadPlayer, false);
            }
            m_playerDeathTime = null;
            ComponentMount componentMount = null;
            if (spawnMode != SpawnMode.Respawn
                && CheckIsPointInWater(Terrain.ToCell(position))) {
                Entity entity = DatabaseManager.CreateEntity(m_project, "Boat", true);
                entity.FindComponent<ComponentBody>(true).Position = position;
                entity.FindComponent<ComponentBody>(true).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathUtils.DegToRad(45f));
                componentMount = entity.FindComponent<ComponentMount>(true);
                m_project.AddEntity(entity);
                position.Y += entity.FindComponent<ComponentBody>(true).BoxSize.Y;
            }
            string value = "";
            string value2 = "";
            string value3 = "";
            string value4 = "";
            int value5 = 0;
            if (spawnMode != SpawnMode.Respawn) {
                if (PlayerClass == PlayerClass.Female) {
                    if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName)
                        && CharacterSkinName.Contains("2")) {
                        value = "";
                        value2 = MakeClothingValue(37, 2);
                        value3 = MakeClothingValue(16, 14);
                        value4 = $"{MakeClothingValue(26, 6)};{MakeClothingValue(27, 0)}";
                    }
                    else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName)
                        && CharacterSkinName.Contains("3")) {
                        value = MakeClothingValue(31, 0);
                        value2 = $"{MakeClothingValue(13, 7)};{MakeClothingValue(5, 0)}";
                        value3 = MakeClothingValue(17, 15);
                        value4 = MakeClothingValue(29, 0);
                    }
                    else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName)
                        && CharacterSkinName.Contains("4")) {
                        value = MakeClothingValue(30, 7);
                        value2 = MakeClothingValue(14, 6);
                        value3 = MakeClothingValue(25, 7);
                        value4 = $"{MakeClothingValue(26, 6)};{MakeClothingValue(8, 0)}";
                    }
                    else {
                        value = MakeClothingValue(30, 12);
                        value2 = $"{MakeClothingValue(37, 3)};{MakeClothingValue(1, 3)}";
                        value3 = MakeClothingValue(0, 12);
                        value4 = $"{MakeClothingValue(26, 6)};{MakeClothingValue(29, 0)}";
                    }
                }
                else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName)
                    && CharacterSkinName.Contains("2")) {
                    value = "";
                    value2 = $"{MakeClothingValue(13, 0)};{MakeClothingValue(5, 0)}";
                    value3 = MakeClothingValue(25, 8);
                    value4 = $"{MakeClothingValue(26, 6)};{MakeClothingValue(29, 0)}";
                }
                else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName)
                    && CharacterSkinName.Contains("3")) {
                    value = MakeClothingValue(32, 0);
                    value2 = MakeClothingValue(37, 5);
                    value3 = MakeClothingValue(0, 15);
                    value4 = $"{MakeClothingValue(26, 6)};{MakeClothingValue(8, 0)}";
                }
                else if (CharacterSkinsManager.IsBuiltIn(CharacterSkinName)
                    && CharacterSkinName.Contains("4")) {
                    value = MakeClothingValue(31, 0);
                    value2 = MakeClothingValue(15, 14);
                    value3 = MakeClothingValue(0, 0);
                    value4 = $"{MakeClothingValue(26, 6)};{MakeClothingValue(8, 0)}";
                }
                else {
                    value = MakeClothingValue(32, 0);
                    value2 = $"{MakeClothingValue(37, 0)};{MakeClothingValue(1, 9)}";
                    value3 = MakeClothingValue(0, 12);
                    value4 = $"{MakeClothingValue(26, 6)};{MakeClothingValue(29, 0)}";
                }
                value5 = m_subsystemGameInfo.WorldSettings.GameMode <= GameMode.Survival ? 1 : 0;
            }
            ValuesDictionary overrides = new() {
                { "Player", new ValuesDictionary { { "PlayerIndex", PlayerIndex } } },
                { "Intro", new ValuesDictionary { { "PlayIntro", spawnMode == SpawnMode.InitialIntro } } }, {
                    "Clothing",
                    new ValuesDictionary {
                        { "Clothes", new ValuesDictionary { { "Feet", value4 }, { "Legs", value3 }, { "Torso", value2 }, { "Head", value } } }
                    }
                }, {
                    "Inventory",
                    new ValuesDictionary {
                        { "Slots", new ValuesDictionary { { "Slot1", new ValuesDictionary { { "Contents", 162 }, { "Count", value5 } } } } }
                    }
                }
            };
            Vector2 v = ComponentIntro.FindOceanDirection(m_subsystemTerrain.TerrainContentsGenerator, position.XZ);
            string entityTemplateName = PlayerClass == PlayerClass.Male ? "MalePlayer" : "FemalePlayer";
            Entity entity2 = DatabaseManager.CreateEntity(m_project, entityTemplateName, overrides, true);
            entity2.FindComponent<ComponentBody>(true).Position = position;
            entity2.FindComponent<ComponentBody>(true).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, Vector2.Angle(v, -Vector2.UnitY));
            m_project.AddEntity(entity2);
            if (componentMount != null) {
                entity2.FindComponent<ComponentRider>(true).StartMounting(componentMount);
            }
            LastSpawnTime = m_subsystemGameInfo.TotalElapsedGameTime;
            ++SpawnsCount;
            ModsManager.HookAction(
                "OnPlayerSpawned",
                modLoader => {
                    modLoader.OnPlayerSpawned(spawnMode, entity2.FindComponent<ComponentPlayer>(), position);
                    return false;
                }
            );
            LastDeadPlayer = null;
        }

        public virtual string GetEntityTemplateName() {
            if (PlayerClass != 0) {
                return "FemalePlayer";
            }
            return "MalePlayer";
        }

        public virtual void UpdateSpawnDialog(string largeMessage, string smallMessage, float progress, bool resetProgress) {
            if (resetProgress) {
                m_progress = 0f;
            }
            m_progress = MathUtils.Max(progress, m_progress);
            if (m_spawnDialog == null) {
                m_spawnDialog = new SpawnDialog();
                DialogsManager.ShowDialog(GameWidget.GuiWidget, m_spawnDialog);
            }
            m_spawnDialog.TimeOfYear = m_subsystemGameInfo.WorldSettings.TimeOfYear;
            if (largeMessage != null) {
                m_spawnDialog.LargeMessage = largeMessage;
            }
            if (smallMessage != null) {
                m_spawnDialog.SmallMessage = smallMessage;
            }
            m_spawnDialog.Progress = m_progress;
        }

        public virtual void HideSpawnDialog() {
            if (m_spawnDialog != null) {
                DialogsManager.HideDialog(m_spawnDialog);
                m_spawnDialog = null;
            }
        }

        public static string MakeClothingValue(int index, int color) => Terrain
            .MakeBlockValue(203, 0, ClothingBlock.SetClothingIndex(ClothingBlock.SetClothingColor(0, color), index))
            .ToString(CultureInfo.InvariantCulture);
    }
}