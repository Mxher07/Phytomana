using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;

namespace Game {
    public class GameMenuDialog : Dialog {
        public static bool m_increaseDetailDialogShown;

        public static bool m_decreaseDetailDialogShown;

        public bool m_adventureRestartExists;

        public StackPanelWidget m_statsPanel;

        public ComponentPlayer m_componentPlayer;

        public static string fName = "GameMenuDialog";

        public GameMenuDialog(ComponentPlayer componentPlayer) {
            XElement node = ContentManager.Get<XElement>("Dialogs/GameMenuDialog");
            LoadContents(this, node);
            m_statsPanel = Children.Find<StackPanelWidget>("StatsPanel");
            m_componentPlayer = componentPlayer;
            m_adventureRestartExists = WorldsManager.SnapshotExists(GameManager.WorldInfo.DirectoryName, "AdventureRestart");
            if (!m_increaseDetailDialogShown
                && PerformanceManager.LongTermAverageFrameTime.HasValue
                && PerformanceManager.LongTermAverageFrameTime.Value * 1000f < 25f
                && (SettingsManager.VisibilityRange <= 64 || SettingsManager.ResolutionMode == ResolutionMode.Low)) {
                m_increaseDetailDialogShown = true;
                DialogsManager.ShowDialog(
                    ParentWidget,
                    new MessageDialog(LanguageControl.Get(fName, 1), LanguageControl.Get(fName, 2), LanguageControl.Ok, null, null)
                );
            }
            if (!m_decreaseDetailDialogShown
                && PerformanceManager.LongTermAverageFrameTime.HasValue
                && PerformanceManager.LongTermAverageFrameTime.Value * 1000f > 50f
                && (SettingsManager.VisibilityRange >= 64 || SettingsManager.ResolutionMode == ResolutionMode.High)) {
                m_decreaseDetailDialogShown = true;
                DialogsManager.ShowDialog(
                    ParentWidget,
                    new MessageDialog(LanguageControl.Get(fName, 3), LanguageControl.Get(fName, 4), LanguageControl.Ok, null, null)
                );
            }
            m_statsPanel.Children.Clear();
            Project project = componentPlayer.Project;
            PlayerData playerData = componentPlayer.PlayerData;
            PlayerStats playerStats = componentPlayer.PlayerStats;
            SubsystemGameInfo subsystemGameInfo = project.FindSubsystem<SubsystemGameInfo>(true);
            SubsystemTimeOfDay subsystemTimeOfDay = project.FindSubsystem<SubsystemTimeOfDay>(true);
            SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior = project.FindSubsystem<SubsystemFurnitureBlockBehavior>(true);
            Terrain terrain = project.FindSubsystem<SubsystemTerrain>(true).Terrain;
            SubsystemMetersBlockBehavior subsystemMetersBlockBehavior = project.FindSubsystem<SubsystemMetersBlockBehavior>(true);
            BitmapFont font = LabelWidget.BitmapFont;
            BitmapFont font2 = LabelWidget.BitmapFont;
            Color white = Color.White;
            StackPanelWidget stackPanelWidget = new() { Direction = LayoutDirection.Vertical, HorizontalAlignment = WidgetAlignment.Center };
            m_statsPanel.Children.Add(stackPanelWidget);
            stackPanelWidget.Children.Add(
                new LabelWidget {
                    Text = LanguageControl.Get(fName, 5),
                    Font = font,
                    HorizontalAlignment = WidgetAlignment.Center,
                    Margin = new Vector2(0f, 10f),
                    Color = white
                }
            );
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 6),
                $"{LanguageControl.Get("GameMode", subsystemGameInfo.WorldSettings.GameMode.ToString())}, {LanguageControl.Get("EnvironmentBehaviorMode", subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode.ToString())}"
            );
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 7),
                StringsManager.GetString($"TerrainGenerationMode.{subsystemGameInfo.WorldSettings.TerrainGenerationMode}.Name")
            );
            string seed = subsystemGameInfo.WorldSettings.Seed;
            AddStat(stackPanelWidget, LanguageControl.Get(fName, 8), $"{(!string.IsNullOrEmpty(seed) ? seed : LanguageControl.Get(fName, 9))} ({subsystemGameInfo.WorldSeed})");
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 10),
                WorldOptionsScreen.FormatOffset(subsystemGameInfo.WorldSettings.SeaLevelOffset)
            );
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 11),
                WorldOptionsScreen.FormatOffset(subsystemGameInfo.WorldSettings.TemperatureOffset)
            );
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 12),
                WorldOptionsScreen.FormatOffset(subsystemGameInfo.WorldSettings.HumidityOffset)
            );
            AddStat(stackPanelWidget, LanguageControl.Get(fName, 13), $"{subsystemGameInfo.WorldSettings.BiomeSize}x");
            if (subsystemGameInfo.WorldSettings.AreSeasonsChanging) {
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 96),
                    subsystemGameInfo.WorldSettings.YearDays + LanguageControl.Get(fName, "23")
                );
            }
            string value0 = subsystemGameInfo.WorldSettings.AreSeasonsChanging ? "" : LanguageControl.Get(fName, 98);
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 97),
                SubsystemSeasons.GetTimeOfYearName(subsystemGameInfo.WorldSettings.TimeOfYear),
                value0,
                SubsystemSeasons.GetTimeOfYearColor(subsystemGameInfo.WorldSettings.TimeOfYear)
            );
            int num = 0;
            for (int i = 0; i < FurnitureDesign.maxDesign; i++) {
                if (subsystemFurnitureBlockBehavior.GetDesign(i) != null) {
                    num++;
                }
            }
            AddStat(stackPanelWidget, LanguageControl.Get(fName, 14), $"{num}/{FurnitureDesign.maxDesign}");
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 15),
                string.IsNullOrEmpty(subsystemGameInfo.WorldSettings.OriginalSerializationVersion)
                    ? LanguageControl.Get(fName, 16)
                    : subsystemGameInfo.WorldSettings.OriginalSerializationVersion
            );
            stackPanelWidget.Children.Add(
                new LabelWidget {
                    Text = LanguageControl.Get(fName, 17),
                    Font = font,
                    HorizontalAlignment = WidgetAlignment.Center,
                    Margin = new Vector2(0f, 10f),
                    Color = white
                }
            );
            AddStat(stackPanelWidget, LanguageControl.Get(fName, 18), playerData.Name);
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 19),
                m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Male ? LanguageControl.Get(fName, 93) : LanguageControl.Get(fName, 94)
            );
            string value = playerData.FirstSpawnTime >= 0.0
                ? ((subsystemGameInfo.TotalElapsedGameTime - playerData.FirstSpawnTime) / subsystemTimeOfDay.DayDuration).ToString("N1")
                + LanguageControl.Get(fName, 20)
                : LanguageControl.Get(fName, 21);
            AddStat(stackPanelWidget, LanguageControl.Get(fName, 22), value);
            string value2 = playerData.LastSpawnTime >= 0.0
                ? ((subsystemGameInfo.TotalElapsedGameTime - playerData.LastSpawnTime) / subsystemTimeOfDay.DayDuration).ToString("N1")
                + LanguageControl.Get(fName, 23)
                : LanguageControl.Get(fName, 24);
            AddStat(stackPanelWidget, LanguageControl.Get(fName, 25), value2);
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 26),
                MathUtils.Max(playerData.SpawnsCount - 1, 0).ToString("N0") + LanguageControl.Get(fName, 27)
            );
            if (subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) {
                Vector3 position = playerData.SpawnPosition;
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, "103"),
                    string.Format(LanguageControl.Get(fName, 31), $"{position.X:F1}", $"{position.Z:F1}", $"{position.Y:F1}")
                );
            }
            AddStat(
                stackPanelWidget,
                LanguageControl.Get(fName, 28),
                string.Format(LanguageControl.Get(fName, 29), ((int)MathF.Floor(playerStats.HighestLevel)).ToString("N0"))
            );
            if (componentPlayer != null) {
                if (subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) {
                    Vector3 position = componentPlayer.ComponentBody.Position;
                    AddStat(
                        stackPanelWidget,
                        LanguageControl.Get(fName, 30),
                        string.Format(LanguageControl.Get(fName, 31), $"{position.X:F1}", $"{position.Z:F1}", $"{position.Y:F1}")
                    );
                    Point3 point = Terrain.ToCell(position);
                    int shaftValue = terrain.GetShaftValue(point.X, point.Z);
                    int terrainTemperature = Terrain.ExtractTemperature(shaftValue) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(point.Y);
                    int seasonalTemperatureOffset = terrain.SeasonTemperature;
                    subsystemMetersBlockBehavior.CalculateTemperature(
                        point.X,
                        point.Y,
                        point.Z,
                        0f,
                        0f,
                        out _,
                        out _,
                        out float finalTemperature
                    );
                    AddStat(
                        stackPanelWidget,
                        LanguageControl.Get(fName, "99"),
                        string.Format(
                            LanguageControl.Get(fName, "100"),
                            $"{terrainTemperature:+0;-0;+0}",
                            $"{seasonalTemperatureOffset:+0;-0;+0}",
                            $"{finalTemperature - terrainTemperature - seasonalTemperatureOffset:+0.0;-0.0;+0}",
                            $"{finalTemperature:F1}"
                        )
                    );
                    int terrainHumidity = Terrain.ExtractHumidity(shaftValue);
                    int seasonalHumidityOffset = terrain.SeasonHumidity;
                    AddStat(
                        stackPanelWidget,
                        LanguageControl.Get(fName, "101"),
                        string.Format(
                            LanguageControl.Get(fName, "102"),
                            $"{terrainHumidity:+0;-0;+0}",
                            $"{seasonalHumidityOffset:+0;-0;+0}",
                            $"{terrainHumidity + seasonalHumidityOffset}"
                        )
                    );
                }
                else {
                    AddStat(
                        stackPanelWidget,
                        LanguageControl.Get(fName, 30),
                        string.Format(
                            LanguageControl.Get(fName, 32),
                            LanguageControl.Get("GameMode", subsystemGameInfo.WorldSettings.GameMode.ToString())
                        )
                    );
                }
            }
            if (string.CompareOrdinal(subsystemGameInfo.WorldSettings.OriginalSerializationVersion, "1.29") > 0) {
                stackPanelWidget.Children.Add(
                    new LabelWidget {
                        Text = LanguageControl.Get(fName, 33),
                        Font = font,
                        HorizontalAlignment = WidgetAlignment.Center,
                        Margin = new Vector2(0f, 10f),
                        Color = white
                    }
                );
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 34), playerStats.PlayerKills.ToString("N0"));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 35), playerStats.LandCreatureKills.ToString("N0"));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 36), playerStats.WaterCreatureKills.ToString("N0"));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 37), playerStats.AirCreatureKills.ToString("N0"));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 38), playerStats.MeleeAttacks.ToString("N0"));
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 39),
                    playerStats.MeleeHits.ToString("N0"),
                    $"({(playerStats.MeleeHits == 0L ? 0.0 : playerStats.MeleeHits / (double)playerStats.MeleeAttacks * 100.0):0}%)"
                );
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 40), playerStats.RangedAttacks.ToString("N0"));
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 41),
                    playerStats.RangedHits.ToString("N0"),
                    $"({(playerStats.RangedHits == 0L ? 0.0 : playerStats.RangedHits / (double)playerStats.RangedAttacks * 100.0):0}%)"
                );
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 42), playerStats.HitsReceived.ToString("N0"));
                stackPanelWidget.Children.Add(
                    new LabelWidget {
                        Text = LanguageControl.Get(fName, 43),
                        Font = font,
                        HorizontalAlignment = WidgetAlignment.Center,
                        Margin = new Vector2(0f, 10f),
                        Color = white
                    }
                );
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 44), playerStats.BlocksDug.ToString("N0"));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 45), playerStats.BlocksPlaced.ToString("N0"));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 46), playerStats.BlocksInteracted.ToString("N0"));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 47), playerStats.ItemsCrafted.ToString("N0"));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 48), playerStats.FurnitureItemsMade.ToString("N0"));
                stackPanelWidget.Children.Add(
                    new LabelWidget {
                        Text = LanguageControl.Get(fName, 49),
                        Font = font,
                        HorizontalAlignment = WidgetAlignment.Center,
                        Margin = new Vector2(0f, 10f),
                        Color = white
                    }
                );
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 50), FormatDistance(playerStats.DistanceTravelled));
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 51),
                    FormatDistance(playerStats.DistanceWalked),
                    $"({(playerStats.DistanceTravelled > 0.0 ? playerStats.DistanceWalked / playerStats.DistanceTravelled * 100.0 : 0.0):0.0}%)"
                );
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 52),
                    FormatDistance(playerStats.DistanceFallen),
                    $"({(playerStats.DistanceTravelled > 0.0 ? playerStats.DistanceFallen / playerStats.DistanceTravelled * 100.0 : 0.0):0.0}%)"
                );
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 53),
                    FormatDistance(playerStats.DistanceClimbed),
                    $"({(playerStats.DistanceTravelled > 0.0 ? playerStats.DistanceClimbed / playerStats.DistanceTravelled * 100.0 : 0.0):0.0}%)"
                );
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 54),
                    FormatDistance(playerStats.DistanceFlown),
                    $"({(playerStats.DistanceTravelled > 0.0 ? playerStats.DistanceFlown / playerStats.DistanceTravelled * 100.0 : 0.0):0.0}%)"
                );
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 55),
                    FormatDistance(playerStats.DistanceSwam),
                    $"({(playerStats.DistanceTravelled > 0.0 ? playerStats.DistanceSwam / playerStats.DistanceTravelled * 100.0 : 0.0):0.0}%)"
                );
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 56),
                    FormatDistance(playerStats.DistanceRidden),
                    $"({(playerStats.DistanceTravelled > 0.0 ? playerStats.DistanceRidden / playerStats.DistanceTravelled * 100.0 : 0.0):0.0}%)"
                );
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 57), FormatDistance(playerStats.LowestAltitude));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 58), FormatDistance(playerStats.HighestAltitude));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 59), $"{playerStats.DeepestDive:N1}m");
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 60), playerStats.Jumps.ToString("N0"));
                stackPanelWidget.Children.Add(
                    new LabelWidget {
                        Text = LanguageControl.Get(fName, 61),
                        Font = font,
                        HorizontalAlignment = WidgetAlignment.Center,
                        Margin = new Vector2(0f, 10f),
                        Color = white
                    }
                );
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 62), $"{(playerStats.TotalHealthLost * 100.0):N0}%");
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 63), playerStats.FoodItemsEaten.ToString("N0") + LanguageControl.Get(fName, 64));
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 65),
                    playerStats.TimesWentToSleep.ToString("N0") + LanguageControl.Get(fName, 66)
                );
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 67),
                    (playerStats.TimeSlept / subsystemTimeOfDay.DayDuration).ToString("N1") + LanguageControl.Get(fName, 68)
                );
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 69), playerStats.TimesWasSick.ToString("N0") + LanguageControl.Get(fName, 66));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 70), playerStats.TimesPuked.ToString("N0") + LanguageControl.Get(fName, 66));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 71), playerStats.TimesHadFlu.ToString("N0") + LanguageControl.Get(fName, 66));
                stackPanelWidget.Children.Add(
                    new LabelWidget {
                        Text = LanguageControl.Get(fName, 72),
                        Font = font,
                        HorizontalAlignment = WidgetAlignment.Center,
                        Margin = new Vector2(0f, 10f),
                        Color = white
                    }
                );
                AddStat(
                    stackPanelWidget,
                    LanguageControl.Get(fName, 73),
                    playerStats.StruckByLightning.ToString("N0") + LanguageControl.Get(fName, 66)
                );
                GameMode easiestModeUsed = playerStats.EasiestModeUsed;
                AddStat(stackPanelWidget, LanguageControl.Get(fName, 74), LanguageControl.Get("GameMode", easiestModeUsed.ToString()));
                AddStat(stackPanelWidget, LanguageControl.Get(fName, "105"), LanguageControl.Get("GameplayImpactLevel", playerStats.HighestGameplayImpactLevel));
                if (playerStats.DeathRecords.Count > 0) {
                    stackPanelWidget.Children.Add(
                        new LabelWidget {
                            Text = LanguageControl.Get(fName, 75),
                            Font = font,
                            HorizontalAlignment = WidgetAlignment.Center,
                            Margin = new Vector2(0f, 10f),
                            Color = white
                        }
                    );
                    foreach (PlayerStats.DeathRecord deathRecord in playerStats.DeathRecords) {
                        AddStat(stackPanelWidget, $"Day {Math.Floor(deathRecord.Day) + 1.0:0}", "", deathRecord.Cause);
                    }
                }
            }
            else {
                stackPanelWidget.Children.Add(
                    new LabelWidget {
                        Text = LanguageControl.Get(fName, 81),
                        WordWrap = true,
                        Font = font2,
                        HorizontalAlignment = WidgetAlignment.Center,
                        TextAnchor = TextAnchor.HorizontalCenter,
                        Margin = new Vector2(20f, 10f),
                        Color = white
                    }
                );
            }
        }

        public override void Update() {
            if (Children.Find<ButtonWidget>("More").IsClicked) {
                List<Tuple<string, Action>> list = new();
                if (m_adventureRestartExists && GameManager.WorldInfo.WorldSettings.GameMode == GameMode.Adventure) {
                    list.Add(
                        new Tuple<string, Action>(
                            LanguageControl.Get(fName, 82),
                            delegate {
                                DialogsManager.ShowDialog(
                                    ParentWidget,
                                    new MessageDialog(
                                        LanguageControl.Get(fName, 83),
                                        LanguageControl.Get(fName, 84),
                                        LanguageControl.Yes,
                                        LanguageControl.No,
                                        delegate(MessageDialogButton result) {
                                            if (result == MessageDialogButton.Button1) {
                                                ScreensManager.SwitchScreen("GameLoading", GameManager.WorldInfo, "AdventureRestart");
                                            }
                                        }
                                    )
                                );
                            }
                        )
                    );
                }
                if (GetRateableItems().FirstOrDefault() != null
                    && UserManager.ActiveUser != null) {
                    list.Add(
                        new Tuple<string, Action>(
                            LanguageControl.Get(fName, 85),
                            delegate {
                                DialogsManager.ShowDialog(
                                    ParentWidget,
                                    new ListSelectionDialog(
                                        LanguageControl.Get(fName, 86),
                                        GetRateableItems(),
                                        60f,
                                        o => ((ActiveExternalContentInfo)o).DisplayName,
                                        delegate(object o) {
                                            ActiveExternalContentInfo activeExternalContentInfo = (ActiveExternalContentInfo)o;
                                            DialogsManager.ShowDialog(
                                                ParentWidget,
                                                new RateCommunityContentDialog(
                                                    activeExternalContentInfo.Address,
                                                    activeExternalContentInfo.DisplayName,
                                                    UserManager.ActiveUser.UniqueId
                                                )
                                            );
                                        }
                                    )
                                );
                            }
                        )
                    );
                }
                list.Add(
                    new Tuple<string, Action>(
                        LanguageControl.Get(fName, 87),
                        delegate { ScreensManager.SwitchScreen("Players", m_componentPlayer.Project.FindSubsystem<SubsystemPlayers>(true)); }
                    )
                );
                list.Add(new Tuple<string, Action>(LanguageControl.Get(fName, 88), delegate { ScreensManager.SwitchScreen("Settings"); }));
                list.Add(
                    new Tuple<string, Action>(
                        LanguageControl.Get("ContentWidgets", "SettingsScreen", "8"),
                        () => ScreensManager.SwitchScreen("ModSettings")
                    )
                );
                list.Add(new Tuple<string, Action>(LanguageControl.Get(fName, 89), delegate { ScreensManager.SwitchScreen("Help"); }));
                if ((Input.Devices & (WidgetInputDevice.Keyboard | WidgetInputDevice.Mouse)) != 0) {
                    list.Add(new Tuple<string, Action>(LanguageControl.Get(fName, 90), delegate { ScreensManager.SwitchScreen("KeyboardMapping"); }));
                }
                if ((Input.Devices & WidgetInputDevice.Gamepads) != 0) {
                    list.Add(
                        new Tuple<string, Action>(
                            LanguageControl.Get(fName, 91),
                            delegate { ScreensManager.SwitchScreen("GamepadMapping"); }
                        )
                    );
                }
                if (VrManager.IsVrStarted
                    && (VrManager.IsControllerPresent(VrController.Left) || VrManager.IsControllerPresent(VrController.Right))) {
                    list.Add(new Tuple<string, Action>(LanguageControl.Get(fName, "106"), delegate { ScreensManager.SwitchScreen("VrControllerMapping"); }));
                }
#if !BROWSER
                list.Add(
                    new Tuple<string, Action>(
                        LanguageControl.Get(fName, 95),
                        delegate { DialogsManager.ShowDialog(ParentWidget, new RunJsDialog()); }
                    )
                );
#endif
#if BROWSER
                list.Add(new Tuple<string, Action>(LanguageControl.Get(fName, "104"), () => Window.WindowMode = Window.WindowMode == WindowMode.Fullscreen ? WindowMode.Fixed : WindowMode.Fullscreen));
#endif
                ListSelectionDialog dialog = new(
                    LanguageControl.Get(fName, 92),
                    list,
                    60f,
                    t => ((Tuple<string, Action>)t).Item1,
                    delegate(object t) { ((Tuple<string, Action>)t).Item2(); }
                );
                DialogsManager.ShowDialog(ParentWidget, dialog);
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("Resume").IsClicked) {
                DialogsManager.HideDialog(this);
            }
            if (Children.Find<ButtonWidget>("Quit").IsClicked) {
                DialogsManager.HideDialog(this);
                GameManager.SaveProject(true, true);
                GameManager.DisposeProject();
                ScreensManager.SwitchScreen("MainMenu");
            }
        }

        public IEnumerable<ActiveExternalContentInfo> GetRateableItems() {
            if (GameManager.Project != null
                && UserManager.ActiveUser != null) {
                SubsystemGameInfo subsystemGameInfo = GameManager.Project.FindSubsystem<SubsystemGameInfo>(true);
                foreach (ActiveExternalContentInfo item in subsystemGameInfo.GetActiveExternalContent()) {
                    if (!CommunityContentManager.IsContentRated(item.Address, UserManager.ActiveUser.UniqueId)) {
                        yield return item;
                    }
                }
            }
        }

        public static string FormatDistance(double value) {
            if (value < 1000.0) {
                return $"{value:0}m";
            }
            return $"{value / 1000.0:N2}km";
        }

        public void AddStat(ContainerWidget containerWidget, string title, string value1, string value2 = "") {
            AddStat(containerWidget, title, value1, value2, Color.White);
        }

        public void AddStat(ContainerWidget containerWidget, string title, string value1, string value2, Color color) {
            BitmapFont font = LabelWidget.BitmapFont;
            Color gray = Color.Gray;
            containerWidget.Children.Add(
                new UniformSpacingPanelWidget {
                    Direction = LayoutDirection.Horizontal,
                    HorizontalAlignment = WidgetAlignment.Center,
                    Children = {
                        new LabelWidget {
                            Text = $"{title}:", HorizontalAlignment = WidgetAlignment.Far, Font = font, Color = gray, Margin = new Vector2(5f, 1f)
                        },
                        new StackPanelWidget {
                            Direction = LayoutDirection.Horizontal,
                            HorizontalAlignment = WidgetAlignment.Near,
                            Children = {
                                new LabelWidget { Text = value1, Font = font, Color = color, Margin = new Vector2(5f, 1f) },
                                new LabelWidget { Text = value2, Font = font, Color = gray, Margin = new Vector2(5f, 1f) }
                            }
                        }
                    }
                }
            );
        }
    }
}