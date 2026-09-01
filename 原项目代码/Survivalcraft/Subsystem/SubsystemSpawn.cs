using System.Globalization;
using System.Text;
using Engine;
using Engine.Serialization;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemSpawn : Subsystem, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemPlayers m_subsystemPlayers;

        public SubsystemGameWidgets m_subsystemViews;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemTime m_subsystemTime;

        public Random m_random = new();

        public double m_nextDiscardOldChunksTime = 1.0;

        public double m_nextVisitedTime = 1.0;

        public double m_nextChunkSpawnTime = 1.0;

        public double m_nextDespawnTime = 1.0;

        public Dictionary<Point2, SpawnChunk> m_chunks = [];

        public Dictionary<ComponentSpawn, bool> m_spawns = [];

        // ReSharper disable CollectionNeverQueried.Global
        public Dictionary<int, SpawnEntityData> m_spawnEntityDatas = new();
        // ReSharper restore CollectionNeverQueried.Global

        public float MaxChunkAge = 76800f;

        public float VisitedRadius = 8f;

        public float SpawnRadius = 48f;

        public float DespawnRadius = 60f;

        public Dictionary<ComponentSpawn, bool>.KeyCollection Spawns => m_spawns.Keys;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual Action<SpawnChunk> SpawningChunk { get; set; }

        public virtual SpawnChunk GetSpawnChunk(Point2 point) {
            m_chunks.TryGetValue(point, out SpawnChunk value);
            return value;
        }

        public virtual void Update(float dt) {
            if (m_subsystemTime.GameTime >= m_nextDiscardOldChunksTime) {
                m_nextDiscardOldChunksTime = m_subsystemTime.GameTime + 60.0;
                DiscardOldChunks();
            }
            if (m_subsystemTime.GameTime >= m_nextVisitedTime) {
                m_nextVisitedTime = m_subsystemTime.GameTime + 5.0;
                UpdateLastVisitedTime();
            }
            if (m_subsystemTime.GameTime >= m_nextChunkSpawnTime) {
                m_nextChunkSpawnTime = m_subsystemTime.GameTime + 4.0;
                SpawnChunks();
            }
            if (m_subsystemTime.GameTime >= m_nextDespawnTime) {
                m_nextDespawnTime = m_subsystemTime.GameTime + 2.0;
                DespawnChunks();
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Chunks")) {
                ValuesDictionary valuesDictionary2 = (ValuesDictionary)item.Value;
                SpawnChunk spawnChunk = new() {
                    Point = HumanReadableConverter.ConvertFromString<Point2>(item.Key),
                    IsSpawned = valuesDictionary2.GetValue<bool>("IsSpawned"),
                    LastVisitedTime = valuesDictionary2.GetValue<double>("LastVisitedTime")
                };
                object obj = valuesDictionary2.GetValue("SpawnsData", new object());
                if (obj is string str) {
                    LoadSpawnsData(str, spawnChunk.SpawnsData);
                }
                else if (obj is ValuesDictionary data) {
#pragma warning disable CS0612 // 类型或成员已过时
                    LoadSpawnsData(data, spawnChunk.SpawnsData);
#pragma warning restore CS0612 // 类型或成员已过时
                }
                m_chunks[spawnChunk.Point] = spawnChunk;
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Chunks", valuesDictionary2);
            foreach (SpawnChunk value2 in m_chunks.Values) {
                if (value2.LastVisitedTime.HasValue) {
                    ValuesDictionary valuesDictionary3 = new();
                    valuesDictionary2.SetValue(HumanReadableConverter.ConvertToString(value2.Point), valuesDictionary3);
                    valuesDictionary3.SetValue("IsSpawned", value2.IsSpawned);
                    valuesDictionary3.SetValue("LastVisitedTime", value2.LastVisitedTime.Value);
                    /*ValuesDictionary v = [];
                    SaveSpawnsData(v, value2.SpawnsData);
                    valuesDictionary3.SetValue("SpawnsData", v);*/
                    string text = SaveSpawnsData(value2.SpawnsData);
                    if (!string.IsNullOrEmpty(text)) {
                        valuesDictionary3.SetValue("SpawnsData", text);
                    }
                }
            }
        }

        public override void OnEntityAdded(Entity entity) {
            foreach (ComponentSpawn item in entity.FindComponents<ComponentSpawn>()) {
                m_spawns.Add(item, true);
            }
        }

        public override void OnEntityRemoved(Entity entity) {
            foreach (ComponentSpawn item in entity.FindComponents<ComponentSpawn>()) {
                m_spawns.Remove(item);
            }
        }

        public virtual SpawnChunk GetOrCreateSpawnChunk(Point2 point) {
            SpawnChunk spawnChunk = GetSpawnChunk(point);
            if (spawnChunk == null) {
                spawnChunk = new SpawnChunk { Point = point };
                m_chunks.Add(point, spawnChunk);
            }
            return spawnChunk;
        }

        public virtual void DiscardOldChunks() {
            List<Point2> list = new();
            foreach (SpawnChunk value in m_chunks.Values) {
                if (!value.LastVisitedTime.HasValue
                    || m_subsystemGameInfo.TotalElapsedGameTime - value.LastVisitedTime.Value > MaxChunkAge) {
                    list.Add(value.Point);
                }
            }
            foreach (Point2 item in list) {
                m_chunks.Remove(item);
            }
        }

        public virtual void UpdateLastVisitedTime() {
            foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers) {
                Vector2 v = new(componentPlayer.ComponentBody.Position.X, componentPlayer.ComponentBody.Position.Z);
                Vector2 p = v - new Vector2(VisitedRadius);
                Vector2 p2 = v + new Vector2(VisitedRadius);
                Point2 point = Terrain.ToChunk(p);
                Point2 point2 = Terrain.ToChunk(p2);
                for (int i = point.X; i <= point2.X; i++) {
                    for (int j = point.Y; j <= point2.Y; j++) {
                        SpawnChunk spawnChunk = GetSpawnChunk(new Point2(i, j));
                        if (spawnChunk != null) {
                            spawnChunk.LastVisitedTime = m_subsystemGameInfo.TotalElapsedGameTime;
                        }
                    }
                }
            }
        }

        public virtual void SpawnChunks() {
            //List<SpawnChunk> list = new();
            foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets) {
                Vector2 v = new(gameWidget.ActiveCamera.ViewPosition.X, gameWidget.ActiveCamera.ViewPosition.Z);
                Vector2 p = v - new Vector2(SpawnRadius);
                Vector2 p2 = v + new Vector2(SpawnRadius);
                Point2 point = Terrain.ToChunk(p);
                Point2 point2 = Terrain.ToChunk(p2);
                for (int i = point.X; i <= point2.X; i++) {
                    for (int j = point.Y; j <= point2.Y; j++) {
                        Vector2 v2 = new((i + 0.5f) * 16f, (j + 0.5f) * 16f);
                        if (Vector2.DistanceSquared(v, v2) < SpawnRadius * SpawnRadius) {
                            TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(v2.X), Terrain.ToCell(v2.Y));
                            if (chunkAtCell != null
                                && chunkAtCell.State > TerrainChunkState.InvalidPropagatedLight) {
                                Point2 point3 = new(i, j);
                                SpawnChunk orCreateSpawnChunk = GetOrCreateSpawnChunk(point3);
                                foreach (SpawnEntityData spawnsDatum in orCreateSpawnChunk.SpawnsData) {
                                    SpawnEntity(spawnsDatum);
                                }
                                orCreateSpawnChunk.SpawnsData.Clear();
                                SpawningChunk?.Invoke(orCreateSpawnChunk);
                                orCreateSpawnChunk.IsSpawned = true;
                            }
                        }
                    }
                }
            }
            /*foreach (SpawnChunk item in list)
            {
                foreach (SpawnEntityData spawnsDatum2 in item.SpawnsData)
                {
                    SpawnEntity(spawnsDatum2);
                }
                item.SpawnsData.Clear();
            }*/
        }

        public virtual void DespawnChunks() {
            List<ComponentSpawn> list = new(0);
            foreach (ComponentSpawn key in m_spawns.Keys) {
                if (key.AutoDespawn
                    && !key.IsDespawning) {
                    bool flag = true;
                    Vector3 position = key.ComponentFrame.Position;
                    Vector2 v = new(position.X, position.Z);
                    foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets) {
                        Vector3 viewPosition = gameWidget.ActiveCamera.ViewPosition;
                        Vector2 v2 = new(viewPosition.X, viewPosition.Z);
                        if (Vector2.DistanceSquared(v, v2) <= DespawnRadius * DespawnRadius) {
                            flag = false;
                            break;
                        }
                    }
                    if (flag) {
                        list.Add(key);
                    }
                }
            }
            foreach (ComponentSpawn item in list) {
                Point2 point = Terrain.ToChunk(item.ComponentFrame.Position.XZ);
                SpawnEntityData data = new() {
                    TemplateName = item.Entity.ValuesDictionary.DatabaseObject.Name,
                    Position = item.ComponentFrame.Position,
                    ConstantSpawn = item.ComponentCreature?.ConstantSpawn ?? false,
                    Data = string.Empty,
                    EntityId = item.Entity.Id
                };
                ModsManager.HookAction(
                    "OnSaveSpawnData",
                    loader => {
                        loader.OnSaveSpawnData(item, data);
                        return false;
                    }
                );
                GetOrCreateSpawnChunk(point).SpawnsData.Add(data);
                m_spawnEntityDatas[data.EntityId] = data;
                item.Despawn();
            }
        }

        public virtual Entity SpawnEntity(SpawnEntityData data) {
            try {
                ValuesDictionary valuesDictionary = DatabaseManager.FindEntityValuesDictionary(data.TemplateName, true);
                Entity entity = Project.CreateEntity(valuesDictionary, data.EntityId);
                ModsManager.HookAction(
                    "OnReadSpawnData",
                    loader => {
                        loader.OnReadSpawnData(entity, data);
                        return false;
                    }
                );
                entity.FindComponent<ComponentBody>(true).Position = data.Position;
                entity.FindComponent<ComponentBody>(true).Rotation = Quaternion.CreateFromAxisAngle(
                    Vector3.UnitY,
                    m_random.Float(0f, (float)Math.PI * 2f)
                );
                ComponentCreature componentCreature = entity.FindComponent<ComponentCreature>();
                if (componentCreature != null) {
                    componentCreature.ConstantSpawn = data.ConstantSpawn;
                }
                Project.AddEntity(entity);
                return entity;
            }
            catch (Exception ex) {
                Log.Error($"Unable to spawn entity with template \"{data.TemplateName}\". Reason: {ex}");
                return null;
            }
        }

        [Obsolete]
        public virtual void LoadSpawnsData(ValuesDictionary loadData, List<SpawnEntityData> creaturesData) {
            foreach ((ValuesDictionary item, SpawnEntityData data) in from ValuesDictionary item in loadData.Values
                let data = new SpawnEntityData()
                select (item, data)) {
                data.ConstantSpawn = item.GetValue<bool>("c");
                data.Position = item.GetValue<Vector3>("p");
                data.TemplateName = item.GetValue<string>("n");
                object obj = item.GetValue("d", new object());
                data.Data = obj is string str ? str : string.Empty;
                creaturesData.Add(data);
            }
        }

        public virtual void LoadSpawnsData(string data, List<SpawnEntityData> creaturesData) {
            string[] array = data.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < array.Length; i++) {
                string[] array2 = array[i].Split(new[] { ',' });
                if (array2.Length < 4) {
                    throw new InvalidOperationException("Invalid spawn data string.");
                }
                SpawnEntityData spawnEntityData = new() {
                    TemplateName = array2[0],
                    Position = new Vector3 {
                        X = float.Parse(array2[1], CultureInfo.InvariantCulture),
                        Y = float.Parse(array2[2], CultureInfo.InvariantCulture),
                        Z = float.Parse(array2[3], CultureInfo.InvariantCulture)
                    }
                };
                if (array2.Length >= 5) {
                    spawnEntityData.ConstantSpawn = bool.Parse(array2[4]);
                }
                spawnEntityData.Data = array2.Length >= 6 ? array2[5] : string.Empty;
                if (array2.Length >= 7) {
                    spawnEntityData.EntityId = int.Parse(array2[6]);
                }
                creaturesData.Add(spawnEntityData);
                m_spawnEntityDatas[spawnEntityData.EntityId] = spawnEntityData;
            }
        }

        [Obsolete]
        public virtual void SaveSpawnsData(ValuesDictionary saveData, List<SpawnEntityData> spawnsData) {
            int i = 0;
            foreach (SpawnEntityData d in spawnsData) {
                ValuesDictionary v2 = [];
                v2.SetValue("c", d.ConstantSpawn);
                v2.SetValue("p", d.Position);
                v2.SetValue("n", d.TemplateName);
                v2.SetValue("d", d.Data);
                saveData.SetValue($"{i++}", v2);
            }
        }

        public virtual string SaveSpawnsData(List<SpawnEntityData> spawnsData) {
            StringBuilder stringBuilder = new();
            foreach (SpawnEntityData spawnEntityData in spawnsData) {
                stringBuilder.Append(spawnEntityData.TemplateName);
                stringBuilder.Append(',');
                stringBuilder.Append((MathF.Round(spawnEntityData.Position.X * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append((MathF.Round(spawnEntityData.Position.Y * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append((MathF.Round(spawnEntityData.Position.Z * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append(spawnEntityData.ConstantSpawn.ToString());
                stringBuilder.Append(',');
                if (spawnEntityData.Data?.Length > 0) {
                    stringBuilder.Append(spawnEntityData.Data);
                }
                stringBuilder.Append(',');
                stringBuilder.Append(spawnEntityData.EntityId.ToString());
                stringBuilder.Append(';');
            }
            return stringBuilder.ToString();
        }
    }
}