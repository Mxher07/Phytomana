using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemCreatureSpawn : Subsystem, IUpdateable {
        public class CreatureType {
            public string Name;

            public SpawnLocationType SpawnLocationType;

            public bool RandomSpawn;

            public bool ConstantSpawn;

            public Func<CreatureType, Point3, float> SpawnSuitabilityFunction;

            public Func<CreatureType, Point3, int> SpawnFunction;

            public CreatureType() {}

            // 要实现类似 StandardCreatureSpawnRule 的从 XML 读取生成规则的类，需要具体实现此方法
            // ReSharper disable UnusedParameter.Local
            public CreatureType(SubsystemCreatureSpawn subsystem, ValuesDictionary valuesDictionary) {}
            // ReSharper restore UnusedParameter.Local

            public CreatureType(string name, SpawnLocationType spawnLocationType, bool randomSpawn, bool constantSpawn) {
                Name = name;
                SpawnLocationType = spawnLocationType;
                RandomSpawn = randomSpawn;
                ConstantSpawn = constantSpawn;
            }

            public override string ToString() => Name;
        }

        public SubsystemGameInfo m_subsystemGameInfo;
        public SubsystemSpawn m_subsystemSpawn;
        public SubsystemTerrain m_subsystemTerrain;
        public SubsystemTime m_subsystemTime;
        public SubsystemSky m_subsystemSky;
        public SubsystemSeasons m_subsystemSeasons;
        public SubsystemBodies m_subsystemBodies;
        public SubsystemGameWidgets m_subsystemViews;

        public Random m_random = new();
        public List<CreatureType> m_creatureTypes = [];
        public Dictionary<ComponentCreature, bool> m_creatures = [];
        public DynamicArray<ComponentBody> m_componentBodies = [];
        public List<SpawnChunk> m_newSpawnChunks = [];
        public List<SpawnChunk> m_spawnChunks = [];

        public static SpawnLocationType[] m_spawnLocations = EnumUtils.GetEnumValues<SpawnLocationType>().Cast<SpawnLocationType>().ToArray();
        public static int m_totalLimit = 26;
        public static int m_areaLimit = 3;
        public static int m_areaRadius = 16;
        public static int m_totalLimitConstant = 6;
        public static int m_totalLimitConstantChallenging = 12;
        public static int m_areaLimitConstant = 4;
        public static int m_areaRadiusConstant = 42;
        public const float m_populationReductionConstant = 0.25f;

        public static Dictionary<string, Type> m_creatureSpawnRules = [];

        public Dictionary<ComponentCreature, bool>.KeyCollection Creatures => m_creatures.Keys;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void Update(float dt) {
            if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living) {
                if (m_newSpawnChunks.Count > 0) {
                    m_newSpawnChunks.RandomShuffle(max => m_random.Int(0, max - 1));
                    foreach (SpawnChunk newSpawnChunk in m_newSpawnChunks) {
                        SpawnChunkCreatures(newSpawnChunk, 10, false);
                    }
                    m_newSpawnChunks.Clear();
                }
                if (m_spawnChunks.Count > 0) {
                    m_spawnChunks.RandomShuffle(max => m_random.Int(0, max - 1));
                    foreach (SpawnChunk spawnChunk in m_spawnChunks) {
                        SpawnChunkCreatures(spawnChunk, 2, true);
                    }
                    m_spawnChunks.Clear();
                }
                float num = m_subsystemSeasons.Season == Season.Winter ? 120f : 60f;
                if (m_subsystemTime.PeriodicGameTimeEvent(num, 2.0)) {
                    SpawnRandomCreature();
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemSpawn = Project.FindSubsystem<SubsystemSpawn>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemSeasons = Project.FindSubsystem<SubsystemSeasons>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(true);
            InitializeCreatureTypesFromDatabase(valuesDictionary.GetValue<ValuesDictionary>("CreatureSpawnRules", null));
            InitializeCreatureTypes();
            m_subsystemSpawn.SpawningChunk += delegate(SpawnChunk chunk) {
                m_spawnChunks.Add(chunk);
                if (!chunk.IsSpawned) {
                    m_newSpawnChunks.Add(chunk);
                }
            };
        }

        public override void OnEntityAdded(Entity entity) {
            foreach (ComponentCreature item in entity.FindComponents<ComponentCreature>()) {
                m_creatures.Add(item, true);
            }
        }

        public override void OnEntityRemoved(Entity entity) {
            foreach (ComponentCreature item in entity.FindComponents<ComponentCreature>()) {
                m_creatures.Remove(item);
            }
        }

        public virtual void InitializeCreatureTypes() {
            m_creatureTypes.Add(
                new CreatureType("Duck", SpawnLocationType.Surface, true, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float shoreDistance = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int contents = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
                        return humidity > 8
                            && temperature > 4
                            && shoreDistance > 40f
                            && point.Y >= topHeight
                            && (BlocksManager.Blocks[contents] is LeavesBlock || contents == 18 || contents == 8 || contents == 2)
                                ? 2.5f
                                : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Duck", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Raven", SpawnLocationType.Surface, true, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num95 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature37 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity25 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num96 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        int topHeight2 = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
                        return (humidity25 <= 8 || temperature37 <= 4)
                            && num95 > 40f
                            && point.Y >= topHeight2
                            && (BlocksManager.Blocks[num96] is LeavesBlock || num96 == 62 || num96 == 8 || num96 == 2 || num96 == 7)
                                ? 2.5f
                                : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Raven", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Seagull", SpawnLocationType.Surface, true, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num93 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int num94 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
                        return num93 > -100f && num93 < 40f && point.Y >= topHeight && (num94 == 18 || num94 == 7 || num94 == 6 || num94 == 62)
                            ? 2.5f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Seagull", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Wildboar", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num91 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int humidity24 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num92 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num91 > 20f && humidity24 > 8 && point.Y < 80 && (num92 == 8 || num92 == 2) ? 0.25f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Wildboar", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Brown Cattle", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num89 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int humidity23 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int temperature36 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num90 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num89 > 20f && humidity23 > 4 && temperature36 >= 8 && point.Y < 70 && (num90 == 8 || num90 == 2) ? 0.05f : 0f;
                    },
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        int num87 = m_random.Int(3, 5);
                        int num88 = MathUtils.Min(m_random.Int(1, 3), num87);
                        int count2 = num87 - num88;
                        return 0
                            + SpawnCreatures(creatureType, "Bull_Brown", point, num88).Count
                            + SpawnCreatures(creatureType, "Cow_Brown", point, count2).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Black Cattle", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num85 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int humidity22 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int temperature35 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num86 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num85 > 20f && humidity22 > 4 && temperature35 < 8 && point.Y < 70 && (num86 == 8 || num86 == 2) ? 0.05f : 0f;
                    },
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        int num83 = m_random.Int(3, 5);
                        int num84 = MathUtils.Min(m_random.Int(1, 3), num83);
                        int count = num83 - num84;
                        return 0
                            + SpawnCreatures(creatureType, "Bull_Black", point, num84).Count
                            + SpawnCreatures(creatureType, "Cow_Black", point, count).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("White Bull", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num81 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int humidity21 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int temperature34 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num82 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num81 > 20f && humidity21 > 8 && temperature34 < 4 && point.Y < 70 && (num82 == 8 || num82 == 2) ? 0.01f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Bull_White", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Gray Wolves", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num79 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int humidity20 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num80 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num79 > 40f && humidity20 >= 8 && point.Y < 100 && (num80 == 8 || num80 == 2) ? 0.075f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Wolf_Gray", point, m_random.Int(1, 3)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Coyotes", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num77 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int humidity19 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int temperature33 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num78 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num77 > 40f && temperature33 > 8 && humidity19 < 8 && humidity19 >= 2 && point.Y < 100 && num78 == 7 ? 0.075f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Wolf_Coyote", point, m_random.Int(1, 3)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Brown Bears", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num75 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature32 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity18 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num76 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num75 > 40f && humidity18 >= 4 && temperature32 >= 8 && point.Y < 110 && (num76 == 8 || num76 == 2 || num76 == 3)
                            ? 0.1f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Bear_Brown", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Black Bears", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num73 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature31 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity17 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num74 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num73 > 40f && humidity17 >= 4 && temperature31 < 8 && point.Y < 120 && (num74 == 8 || num74 == 2 || num74 == 3)
                            ? 0.1f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Bear_Black", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Polar Bears", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num71 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature30 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num72 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num71 > -40f && temperature30 < 8 && point.Y < 80 && num72 == 62 ? 0.1f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Bear_Polar", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Horses", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num69 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature29 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity16 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num70 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num69 > 20f && temperature29 > 3 && humidity16 > 6 && point.Y < 80 && (num70 == 8 || num70 == 2 || num70 == 3)
                            ? 0.05f
                            : 0f;
                    },
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        int temperature28 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num68 = 0;
                        if (m_random.Float(0f, 1f) < 0.35f) {
                            num68 += SpawnCreatures(creatureType, "Horse_Black", point, 1).Count;
                        }
                        if (m_random.Float(0f, 1f) < 0.5f) {
                            num68 += SpawnCreatures(creatureType, "Horse_Bay", point, 1).Count;
                        }
                        if (m_random.Float(0f, 1f) < 0.5f) {
                            num68 += SpawnCreatures(creatureType, "Horse_Chestnut", point, 1).Count;
                        }
                        if (temperature28 > 8
                            && m_random.Float(0f, 1f) < 0.3f) {
                            num68 += SpawnCreatures(creatureType, "Horse_Palomino", point, 1).Count;
                        }
                        if (temperature28 < 8
                            && m_random.Float(0f, 1f) < 0.3f) {
                            num68 += SpawnCreatures(creatureType, "Horse_White", point, 1).Count;
                        }
                        return num68;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Camels", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num66 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature27 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity15 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num67 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num66 > 20f && temperature27 > 8 && humidity15 < 8 && point.Y < 80 && num67 == 7 ? 0.05f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Camel", point, m_random.Int(1, 2)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Donkeys", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num64 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature26 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num65 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num64 > 20f && temperature26 > 6 && point.Y < 120 && (num65 == 8 || num65 == 2 || num65 == 3 || num65 == 7)
                            ? 0.05f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Donkey", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Giraffes", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num62 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature25 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity14 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num63 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num62 > 20f && temperature25 > 8 && humidity14 > 7 && point.Y < 75 && (num63 == 8 || num63 == 2 || num63 == 3)
                            ? 0.03f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Giraffe", point, m_random.Int(1, 2)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Rhinos", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num60 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature24 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity13 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num61 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num60 > 40f && temperature24 > 8 && humidity13 > 7 && point.Y < 75 && (num61 == 8 || num61 == 2 || num61 == 3)
                            ? 0.03f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Rhino", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Tigers", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num58 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int humidity12 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num59 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num58 > 40f && humidity12 > 8 && point.Y < 80 && (num59 == 8 || num59 == 2 || num59 == 3 || num59 == 7) ? 0.025f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Tiger", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("White Tigers", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num56 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature23 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num57 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num56 > 40f
                            && temperature23 < 2
                            && point.Y < 90
                            && (num57 == 8 || num57 == 2 || num57 == 3 || num57 == 7 || num57 == 62)
                                ? 0.02f
                                : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Tiger_White", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Lions", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num54 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature22 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num55 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num54 > 40f && temperature22 > 8 && point.Y < 80 && (num55 == 8 || num55 == 2 || num55 == 3 || num55 == 7)
                            ? 0.04f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Lion", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Jaguars", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num52 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature21 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity11 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num53 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num52 > 40f
                            && humidity11 > 8
                            && temperature21 > 8
                            && point.Y < 100
                            && (num53 == 8 || num53 == 2 || num53 == 3 || num53 == 7 || num53 == 12)
                                ? 0.03f
                                : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Jaguar", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Leopards", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num50 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature20 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num51 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num50 > 40f
                            && temperature20 > 8
                            && point.Y < 120
                            && (num51 == 8 || num51 == 2 || num51 == 3 || num51 == 7 || num51 == 12)
                                ? 0.03f
                                : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Leopard", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Zebras", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num48 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature19 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity10 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num49 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num48 > 20f && temperature19 > 8 && humidity10 > 7 && point.Y < 80 && (num49 == 8 || num49 == 2 || num49 == 3)
                            ? 0.05f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Zebra", point, m_random.Int(1, 2)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Gnus", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num46 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature18 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num47 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num46 > 20f && temperature18 > 8 && point.Y < 80 && (num47 == 8 || num47 == 2 || num47 == 3) ? 0.05f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Gnu", point, m_random.Int(1, 2)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Reindeers", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        int temperature17 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num45 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return temperature17 < 3 && point.Y < 90 && (num45 == 8 || num45 == 2 || num45 == 3 || num45 == 62) ? 0.05f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Reindeer", point, m_random.Int(1, 3)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Mooses", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        int temperature16 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num44 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return temperature16 < 7 && point.Y < 90 && (num44 == 8 || num44 == 2 || num44 == 3 || num44 == 62) ? 0.1f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Moose", point, m_random.Int(1, 1)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Bisons", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        int temperature15 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity9 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num43 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return temperature15 < 10 && humidity9 < 12 && point.Y < 80 && (num43 == 8 || num43 == 2 || num43 == 3 || num43 == 62)
                            ? 0.1f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Bison", point, m_random.Int(1, 4)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Ostriches", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num41 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature14 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity8 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num42 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num41 > 20f && temperature14 > 8 && humidity8 < 8 && point.Y < 75 && (num42 == 8 || num42 == 2 || num42 == 7)
                            ? 0.05f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Ostrich", point, m_random.Int(1, 2)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Cassowaries", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num39 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature13 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity7 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num40 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num39 > 20f && temperature13 > 8 && humidity7 < 12 && point.Y < 75 && (num40 == 8 || num40 == 2 || num40 == 7)
                            ? 0.05f
                            : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Cassowary", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Hyenas", SpawnLocationType.Surface, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num37 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature12 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num38 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num37 > 40f && temperature12 > 8 && point.Y < 80 && (num38 == 8 || num38 == 2 || num38 == 7) ? 0.05f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Hyena", point, m_random.Int(1, 2)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Cave Bears", SpawnLocationType.Cave, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        int num36 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num36 == 3 || num36 == 67 || num36 == 4 || num36 == 66 || num36 == 2 || num36 == 7 ? 1f : 0f;
                    },
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName11 = m_random.Int(0, 1) == 0 ? "Bear_Black" : "Bear_Brown";
                        return SpawnCreatures(creatureType, templateName11, point, 1).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Cave Tigers", SpawnLocationType.Cave, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        int num35 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num35 == 3 || num35 == 67 || num35 == 4 || num35 == 66 || num35 == 2 || num35 == 7 ? 0.25f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Tiger", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Cave Lions", SpawnLocationType.Cave, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        int temperature11 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int humidity6 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num34 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return (num34 == 3 || num34 == 67 || num34 == 4 || num34 == 66 || num34 == 2 || num34 == 7)
                            && temperature11 > 8
                            && humidity6 < 8
                                ? 0.25f
                                : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Lion", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Cave Jaguars", SpawnLocationType.Cave, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        int num33 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num33 == 3 || num33 == 67 || num33 == 4 || num33 == 66 || num33 == 2 || num33 == 7 ? 0.5f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Jaguar", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Cave Leopards", SpawnLocationType.Cave, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        int num32 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return num32 == 3 || num32 == 67 || num32 == 4 || num32 == 66 || num32 == 2 || num32 == 7 ? 0.25f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Leopard", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Cave Hyenas", SpawnLocationType.Cave, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        int temperature10 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        int num31 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        return (num31 == 3 || num31 == 67 || num31 == 4 || num31 == 66 || num31 == 2 || num31 == 7) && temperature10 > 8 ? 1f : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Hyena", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Bull Sharks", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction =
                        (_, point) => !(m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -2f) ? 0f : 0.4f,
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName10 = "Shark_Bull";
                        return SpawnCreatures(creatureType, templateName10, point, 1).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Tiger Sharks", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction =
                        (_, point) => !(m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -5f) ? 0f : 0.3f,
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName9 = "Shark_Tiger";
                        return SpawnCreatures(creatureType, templateName9, point, 1).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Great White Sharks", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction =
                        (_, point) => !(m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -20f) ? 0f : 0.2f,
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName8 = "Shark_GreatWhite";
                        return SpawnCreatures(creatureType, templateName8, point, 1).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Barracudas", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction =
                        (_, point) => !(m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -2f) ? 0f : 0.5f,
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName7 = "Barracuda";
                        return SpawnCreatures(creatureType, templateName7, point, 1).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Bass_Sea", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction =
                        (_, point) => !(m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < -2f) ? 0f : 1f,
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName6 = "Bass_Sea";
                        return SpawnCreatures(creatureType, templateName6, point, 1).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Bass_Freshwater", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num30 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature9 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        return num30 > 10f && temperature9 >= 4 ? 1f : 0f;
                    },
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName5 = "Bass_Freshwater";
                        return SpawnCreatures(creatureType, templateName5, point, m_random.Int(1, 2)).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Rays", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction =
                        (_, point) => !(m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z) < 10f) ? 1f : 0.5f,
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        int num27 = 0;
                        int num28 = 0;
                        for (int i = point.X - 2; i <= point.X + 2; i++) {
                            for (int j = point.Z - 2; j <= point.Z + 2; j++) {
                                if (m_subsystemTerrain.Terrain.GetCellContents(point.X, point.Y, point.Z) == 18) {
                                    for (int num29 = point.Y - 1; num29 > 0; num29--) {
                                        switch (m_subsystemTerrain.Terrain.GetCellContents(point.X, num29, point.Z)) {
                                            case 2: num27++; break;
                                            case 7: num28++; break;
                                            default: continue;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        string templateName4 = num27 >= num28 ? "Ray_Brown" : "Ray_Yellow";
                        return SpawnCreatures(creatureType, templateName4, point, 1).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Piranhas", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num26 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int humidity5 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int temperature8 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        return num26 > 10f && humidity5 >= 4 && temperature8 >= 7 ? 1f : 0f;
                    },
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName3 = "Piranha";
                        return SpawnCreatures(creatureType, templateName3, point, m_random.Int(2, 4)).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Orcas", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num25 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        if (num25 < -100f) {
                            return 0.05f;
                        }
                        return num25 < -20f ? 0.01f : 0f;
                    },
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName2 = "Orca";
                        return SpawnCreatures(creatureType, templateName2, point, 1).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Belugas", SpawnLocationType.Water, false, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num24 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        if (num24 < -100f) {
                            return 0.05f;
                        }
                        return num24 < -20f ? 0.01f : 0f;
                    },
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        string templateName = "Beluga";
                        return SpawnCreatures(creatureType, templateName, point, 1).Count;
                    }
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Gray Wolves", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num21 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            int humidity4 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                            float num22 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                            int num23 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast10 = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (((num21 > 20f && humidity4 >= 8) || (num22 <= 8f && point.Y < 90 && cellLightFast10 <= 7))
                                && (num23 == 8 || num23 == 2)) {
                                return 2f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Wolf_Gray", point, m_random.Int(1, 3)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Coyotes", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num17 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            float num18 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                            float num19 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                            int num20 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast9 = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (num17 > 20f
                                && num19 > 8f
                                && num18 < 8f
                                && point.Y < 90
                                && cellLightFast9 <= 7
                                && num20 == 7) {
                                return 2f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Wolf_Coyote", point, m_random.Int(1, 3)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Brown Bears", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num15 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            int temperature7 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                            int humidity3 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                            int num16 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast8 = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (num15 > 20f
                                && humidity3 >= 4
                                && temperature7 >= 8
                                && point.Y < 100
                                && cellLightFast8 <= 7
                                && (num16 == 8 || num16 == 2 || num16 == 3)) {
                                return 0.5f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Bear_Brown", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Black Bears", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num13 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            int temperature6 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                            m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                            int num14 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast7 = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (num13 > 20f
                                && temperature6 < 8
                                && point.Y < 110
                                && cellLightFast7 <= 7
                                && (num14 == 8 || num14 == 2 || num14 == 3)) {
                                return 0.5f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Bear_Black", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Polar Bears", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num11 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            int temperature5 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                            int num12 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast6 = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (num11 > -40f
                                && temperature5 < 8
                                && point.Y < 90
                                && cellLightFast6 <= 7
                                && num12 == 62) {
                                return 0.25f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Bear_Black", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Tigers", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num9 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            int humidity2 = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                            int num10 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast5 = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (num9 > 20f
                                && humidity2 > 8
                                && point.Y < 90
                                && cellLightFast5 <= 7
                                && (num10 == 8 || num10 == 2 || num10 == 3)) {
                                return 0.05f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Tiger", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Lions", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num7 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            int temperature4 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                            int num8 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast4 = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (num7 > 20f
                                && temperature4 > 8
                                && point.Y < 90
                                && cellLightFast4 <= 7
                                && (num8 == 8 || num8 == 2 || num8 == 3 || num8 == 7)) {
                                return 0.25f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Lion", point, m_random.Int(1, 2)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Jaguars", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num5 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            int temperature3 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                            int humidity = m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                            int num6 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast3 = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (num5 > 20f
                                && temperature3 > 8
                                && humidity > 8
                                && point.Y < 100
                                && cellLightFast3 <= 7
                                && (num6 == 8 || num6 == 2 || num6 == 3 || num6 == 12)) {
                                return 0.25f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Jaguar", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Leopards", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num3 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            int temperature2 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                            int num4 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast2 = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (num3 > 20f
                                && temperature2 > 8
                                && point.Y < 110
                                && cellLightFast2 <= 7
                                && (num4 == 8 || num4 == 2 || num4 == 3 || num4 == 12)) {
                                return 0.25f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Leopard", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Constant Hyenas", SpawnLocationType.Surface, false, true) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        if (m_subsystemSky.SkyLightIntensity < 0.1f) {
                            float num = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                            int temperature = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                            int num2 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                            int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(point.X, point.Y + 1, point.Z);
                            if (num > 20f
                                && temperature > 8
                                && point.Y < 100
                                && cellLightFast <= 7
                                && (num2 == 8 || num2 == 2 || num2 == 3 || num2 == 7)) {
                                return 1f;
                            }
                        }
                        return 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Hyena", point, m_random.Int(1, 2)).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Pigeon", SpawnLocationType.Surface, true, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num95 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature38 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num96 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        int topHeight2 = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
                        return temperature38 > 3
                            && num95 > 30f
                            && point.Y >= topHeight2
                            && (BlocksManager.Blocks[num96] is LeavesBlock || num96 == 8 || num96 == 2 || num96 == 7)
                                ? 1.5f
                                : 0f;
                    },
                    SpawnFunction = (creatureType, point) => SpawnCreatures(creatureType, "Pigeon", point, 1).Count
                }
            );
            m_creatureTypes.Add(
                new CreatureType("Sparrow", SpawnLocationType.Surface, true, false) {
                    SpawnSuitabilityFunction = delegate(CreatureType _, Point3 point) {
                        float num93 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(point.X, point.Z);
                        int temperature37 = m_subsystemTerrain.Terrain.GetTemperature(point.X, point.Z);
                        m_subsystemTerrain.Terrain.GetHumidity(point.X, point.Z);
                        int num94 = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValueFast(point.X, point.Y - 1, point.Z));
                        int topHeight = m_subsystemTerrain.Terrain.GetTopHeight(point.X, point.Z);
                        return temperature37 > 3
                            && num93 > 20f
                            && point.Y >= topHeight
                            && (BlocksManager.Blocks[num94] is LeavesBlock || num94 == 8 || num94 == 2 || num94 == 7)
                                ? 1.5f
                                : 0f;
                    },
                    SpawnFunction = delegate(CreatureType creatureType, Point3 point) {
                        int count3 = m_random.Int(1, 2);
                        return SpawnCreatures(creatureType, "Sparrow", point, count3).Count;
                    }
                }
            );
            ModsManager.HookAction(
                "InitializeCreatureTypes",
                modLoader => {
                    modLoader.InitializeCreatureTypes(this, m_creatureTypes);
                    return false;
                }
            );
        }

        public virtual void InitializeCreatureTypesFromDatabase(ValuesDictionary valuesDictionary) {
            if (valuesDictionary == null) {
                return;
            }
            foreach (object value in valuesDictionary.Values) {
                if (value is ValuesDictionary valuesDictionary1) {
                    string ruleTypeName = valuesDictionary1.GetValue<string>("Class", null);
                    if (string.IsNullOrEmpty(ruleTypeName)) {
                        continue;
                    }
                    if (m_creatureSpawnRules.TryGetValue(ruleTypeName, out Type ruleType)) {
                        try {
                            m_creatureTypes.Add((CreatureType)Activator.CreateInstance(ruleType, this, valuesDictionary1));
                        }
                        catch (Exception e) {
                            Log.Error($"An error occurred while initializing creature types from database. Reason: {e.Message}");
                        }
                    }
                }
            }
        }

        public virtual void SpawnRandomCreature() {
            if (CountCreatures(false) < m_totalLimit) {
                foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets) {
                    Vector2 v = new(gameWidget.ActiveCamera.ViewPosition.X, gameWidget.ActiveCamera.ViewPosition.Z);
                    if (CountCreaturesInArea(v - new Vector2(68f), v + new Vector2(68f), false) >= 52) {
                        break;
                    }
                    SpawnLocationType spawnLocationType = GetRandomSpawnLocationType();
                    Point3? spawnPoint = GetRandomSpawnPoint(gameWidget.ActiveCamera, spawnLocationType);
                    if (spawnPoint.HasValue) {
                        Vector2 c2 = new Vector2(spawnPoint.Value.X, spawnPoint.Value.Z) - new Vector2(16f);
                        Vector2 c3 = new Vector2(spawnPoint.Value.X, spawnPoint.Value.Z) + new Vector2(16f);
                        if (CountCreaturesInArea(c2, c3, false) >= 3) {
                            break;
                        }
                        IEnumerable<CreatureType> source = m_creatureTypes.Where(c => c.SpawnLocationType == spawnLocationType && c.RandomSpawn);
                        IEnumerable<CreatureType> creatureTypes = source as CreatureType[] ?? source.ToArray();
                        IEnumerable<float> items = creatureTypes.Select(c => CalculateSpawnSuitability(c, spawnPoint.Value));
                        int randomWeightedItem = GetRandomWeightedItem(items);
                        if (randomWeightedItem >= 0) {
                            CreatureType creatureType = creatureTypes.ElementAt(randomWeightedItem);
                            creatureType.SpawnFunction(creatureType, spawnPoint.Value);
                        }
                    }
                }
            }
        }

        public virtual void SpawnChunkCreatures(SpawnChunk chunk, int maxAttempts, bool constantSpawn) {
            int num = constantSpawn
                ? m_subsystemGameInfo.WorldSettings.GameMode >= GameMode.Challenging ? m_totalLimitConstantChallenging : m_totalLimitConstant
                : m_totalLimit;
            int num2 = constantSpawn ? m_areaLimitConstant : m_areaLimit;
            float v = constantSpawn ? m_areaRadiusConstant : m_areaRadius;
            int num3 = CountCreatures(constantSpawn);
            Vector2 c2 = new Vector2(chunk.Point.X * 16, chunk.Point.Y * 16) - new Vector2(v);
            Vector2 c3 = new Vector2((chunk.Point.X + 1) * 16, (chunk.Point.Y + 1) * 16) + new Vector2(v);
            int num4 = CountCreaturesInArea(c2, c3, constantSpawn);
            for (int i = 0; i < maxAttempts; i++) {
                if (num3 >= num) {
                    break;
                }
                if (num4 >= num2) {
                    break;
                }
                SpawnLocationType spawnLocationType = GetRandomSpawnLocationType();
                Point3? spawnPoint = GetRandomChunkSpawnPoint(chunk, spawnLocationType);
                if (spawnPoint.HasValue) {
                    IEnumerable<CreatureType> source = m_creatureTypes.Where(c => c.SpawnLocationType == spawnLocationType
                        && c.ConstantSpawn == constantSpawn
                    );
                    IEnumerable<CreatureType> creatureTypes = source as CreatureType[] ?? source.ToArray();
                    IEnumerable<float> items = creatureTypes.Select(c => CalculateSpawnSuitability(c, spawnPoint.Value));
                    int randomWeightedItem = GetRandomWeightedItem(items);
                    if (randomWeightedItem >= 0) {
                        CreatureType creatureType = creatureTypes.ElementAt(randomWeightedItem);
                        int num5 = creatureType.SpawnFunction(creatureType, spawnPoint.Value);
                        num3 += num5;
                        num4 += num5;
                    }
                }
            }
        }

        public virtual List<Entity> SpawnCreatures(CreatureType creatureType, string templateName, Point3 point, int count) {
            List<Entity> list = new();
            int num = 0;
            while (count > 0
                && num < 50) {
                Point3 spawnPoint = point;
                if (num > 0) {
                    spawnPoint.X += m_random.Int(-8, 8);
                    spawnPoint.Y += m_random.Int(-4, 8);
                    spawnPoint.Z += m_random.Int(-8, 8);
                }
                Point3? point2 = ProcessSpawnPoint(spawnPoint, creatureType.SpawnLocationType);
                if (point2.HasValue
                    && CalculateSpawnSuitability(creatureType, point2.Value) > 0f) {
                    Vector3 position = new(
                        point2.Value.X + m_random.Float(0.4f, 0.6f),
                        point2.Value.Y + 1.1f,
                        point2.Value.Z + m_random.Float(0.4f, 0.6f)
                    );
                    Entity entity = SpawnCreature(templateName, position, creatureType.ConstantSpawn);
                    if (entity != null) {
                        list.Add(entity);
                        count--;
                    }
                }
                num++;
            }
            return list;
        }

        public virtual Entity SpawnCreature(string templateName, Vector3 position, bool constantSpawn) {
            try {
                Entity entity = DatabaseManager.CreateEntity(Project, templateName, true);
                entity.FindComponent<ComponentBody>(true).Position = position;
                entity.FindComponent<ComponentBody>(true).Rotation = Quaternion.CreateFromAxisAngle(
                    Vector3.UnitY,
                    m_random.Float(0f, (float)Math.PI * 2f)
                );
                entity.FindComponent<ComponentCreature>(true).ConstantSpawn = constantSpawn;
                Project.AddEntity(entity);
                return entity;
            }
            catch (Exception ex) {
                Log.Error($"Unable to spawn creature with template \"{templateName}\". Reason: {ex}");
                return null;
            }
        }

        public virtual Point3? GetRandomChunkSpawnPoint(SpawnChunk chunk, SpawnLocationType spawnLocationType) {
            for (int i = 0; i < 5; i++) {
                int x = 16 * chunk.Point.X + m_random.Int(0, 15);
                int y = m_random.Int(10, 246);
                int z = 16 * chunk.Point.Y + m_random.Int(0, 15);
                Point3? result = ProcessSpawnPoint(new Point3(x, y, z), spawnLocationType);
                if (result.HasValue) {
                    return result;
                }
            }
            return null;
        }

        public virtual Point3? GetRandomSpawnPoint(Camera camera, SpawnLocationType spawnLocationType) {
            for (int i = 0; i < 10; i++) {
                int x = Terrain.ToCell(camera.ViewPosition.X) + m_random.Sign() * m_random.Int(24, 48);
                int y = Math.Clamp(Terrain.ToCell(camera.ViewPosition.Y) + m_random.Int(-30, 30), 2, 254);
                int z = Terrain.ToCell(camera.ViewPosition.Z) + m_random.Sign() * m_random.Int(24, 48);
                Point3? result = ProcessSpawnPoint(new Point3(x, y, z), spawnLocationType);
                if (result.HasValue) {
                    return result;
                }
            }
            return null;
        }

        public virtual Point3? ProcessSpawnPoint(Point3 spawnPoint, SpawnLocationType spawnLocationType) {
            int x = spawnPoint.X;
            int num = Math.Clamp(spawnPoint.Y, 1, 254);
            int z = spawnPoint.Z;
            TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
            if (chunkAtCell != null
                && chunkAtCell.State > TerrainChunkState.InvalidPropagatedLight) {
                for (int i = 0; i < 30; i++) {
                    Point3 point = new(x, num + i, z);
                    if (TestSpawnPoint(point, spawnLocationType)) {
                        return point;
                    }
                    Point3 point2 = new(x, num - i, z);
                    if (TestSpawnPoint(point2, spawnLocationType)) {
                        return point2;
                    }
                }
            }
            return null;
        }

        public virtual bool TestSpawnPoint(Point3 spawnPoint, SpawnLocationType spawnLocationType) {
            int x = spawnPoint.X;
            int y = spawnPoint.Y;
            int z = spawnPoint.Z;
            if (y <= 3
                || y >= TerrainChunk.Height - 3) {
                return false;
            }
            switch (spawnLocationType) {
                case SpawnLocationType.Surface: {
                    int cellLightFast2 = m_subsystemTerrain.Terrain.GetCellLightFast(x, y, z);
                    if (m_subsystemSky.SkyLightValue - cellLightFast2 > 3) {
                        return false;
                    }
                    int cellValueFast7 = m_subsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
                    int cellValueFast8 = m_subsystemTerrain.Terrain.GetCellValueFast(x, y, z);
                    int cellValueFast9 = m_subsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z);
                    Block block6 = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFast7)];
                    Block block7 = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFast8)];
                    Block block8 = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFast9)];
                    if ((block6.IsCollidable_(cellValueFast7) || block6 is WaterBlock)
                        && !block7.IsCollidable_(cellValueFast8)
                        && !(block7 is WaterBlock)
                        && !block8.IsCollidable_(cellValueFast9)) {
                        return !(block8 is WaterBlock);
                    }
                    return false;
                }
                case SpawnLocationType.Cave: {
                    int cellLightFast = m_subsystemTerrain.Terrain.GetCellLightFast(x, y, z);
                    if (m_subsystemSky.SkyLightValue - cellLightFast < 5) {
                        return false;
                    }
                    int cellValueFast4 = m_subsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
                    int cellValueFast5 = m_subsystemTerrain.Terrain.GetCellValueFast(x, y, z);
                    int cellValueFast6 = m_subsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z);
                    Block block3 = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFast4)];
                    Block block4 = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFast5)];
                    Block block5 = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFast6)];
                    if ((block3.IsCollidable_(cellValueFast4) || block3 is WaterBlock)
                        && !block4.IsCollidable_(cellValueFast5)
                        && !(block4 is WaterBlock)
                        && !block5.IsCollidable_(cellValueFast6)) {
                        return !(block5 is WaterBlock);
                    }
                    return false;
                }
                case SpawnLocationType.Water: {
                    int cellContentsFast = m_subsystemTerrain.Terrain.GetCellContentsFast(x, y, z);
                    int cellValueFast2 = m_subsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z);
                    int cellValueFast3 = m_subsystemTerrain.Terrain.GetCellValueFast(x, y + 2, z);
                    Block obj = BlocksManager.Blocks[Terrain.ExtractContents(cellContentsFast)];
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFast2)];
                    Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(cellValueFast3)];
                    if (obj is WaterBlock
                        && !block.IsCollidable_(cellValueFast2)) {
                        return !block2.IsCollidable_(cellValueFast3);
                    }
                    return false;
                }
                default: throw new InvalidOperationException("Unknown spawn location type.");
            }
        }

        public virtual float CalculateSpawnSuitability(CreatureType creatureType, Point3 spawnPoint) {
            float num = creatureType.SpawnSuitabilityFunction(creatureType, spawnPoint);
            if (CountCreatures(creatureType) > 8) {
                num *= 0.25f;
            }
            return num;
        }

        public virtual int CountCreatures(CreatureType creatureType) {
            int num = 0;
            foreach (ComponentBody body in m_subsystemBodies.Bodies) {
                if (body.Entity.ValuesDictionary.DatabaseObject.Name == creatureType.Name) {
                    num++;
                }
            }
            return num;
        }

        public virtual int CountCreatures(bool constantSpawn) {
            int num = 0;
            foreach (ComponentBody body in m_subsystemBodies.Bodies) {
                ComponentCreature componentCreature = body.Entity.FindComponent<ComponentCreature>();
                if (componentCreature != null
                    && componentCreature.ConstantSpawn == constantSpawn) {
                    num++;
                }
            }
            return num;
        }

        public virtual int CountCreaturesInArea(Vector2 c1, Vector2 c2, bool constantSpawn) {
            int num = 0;
            m_componentBodies.Clear();
            m_subsystemBodies.FindBodiesInArea(c1, c2, m_componentBodies);
            for (int i = 0; i < m_componentBodies.Count; i++) {
                ComponentBody componentBody = m_componentBodies.Array[i];
                ComponentCreature componentCreature = componentBody.Entity.FindComponent<ComponentCreature>();
                if (componentCreature != null
                    && componentCreature.ConstantSpawn == constantSpawn) {
                    Vector3 position = componentBody.Position;
                    if (position.X >= c1.X
                        && position.X <= c2.X
                        && position.Z >= c1.Y
                        && position.Z <= c2.Y) {
                        num++;
                    }
                }
            }
            Point2 point = Terrain.ToChunk(c1);
            Point2 point2 = Terrain.ToChunk(c2);
            for (int j = point.X; j <= point2.X; j++) {
                for (int k = point.Y; k <= point2.Y; k++) {
                    SpawnChunk spawnChunk = m_subsystemSpawn.GetSpawnChunk(new Point2(j, k));
                    if (spawnChunk != null) {
                        foreach (SpawnEntityData spawnsDatum in spawnChunk.SpawnsData) {
                            if (spawnsDatum.ConstantSpawn == constantSpawn) {
                                Vector3 position2 = spawnsDatum.Position;
                                if (position2.X >= c1.X
                                    && position2.X <= c2.X
                                    && position2.Z >= c1.Y
                                    && position2.Z <= c2.Y) {
                                    num++;
                                }
                            }
                        }
                    }
                }
            }
            return num;
        }

        public virtual int GetRandomWeightedItem(IEnumerable<float> items) {
            IEnumerable<float> enumerable = items as float[] ?? items.ToArray();
            float max = MathUtils.Max(enumerable.Sum(), 1f);
            float num = m_random.Float(0f, max);
            int num2 = 0;
            foreach (float item in enumerable) {
                if (num < item) {
                    return num2;
                }
                num -= item;
                num2++;
            }
            return -1;
        }

        public virtual SpawnLocationType GetRandomSpawnLocationType() {
            float num = m_random.Float();
            if (num <= 0.3f) {
                return SpawnLocationType.Surface;
            }
            if (num <= 0.6f) {
                return SpawnLocationType.Cave;
            }
            return SpawnLocationType.Water;
        }
    }
}