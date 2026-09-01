using System.Diagnostics;
using System.Globalization;
using System.Text;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemElectricity : Subsystem, IUpdateable {
        public static ElectricConnectionPath[] m_connectionPathsTable = [
            new(0, 1, -1, 4, 4, 0),
            new(0, 1, 0, 0, 4, 5),
            new(0, 1, -1, 2, 4, 5),
            new(0, 0, 0, 5, 4, 2),
            new(-1, 0, -1, 3, 3, 0),
            new(-1, 0, 0, 0, 3, 1),
            new(-1, 0, -1, 2, 3, 1),
            new(0, 0, 0, 1, 3, 2),
            new(0, -1, -1, 5, 5, 0),
            new(0, -1, 0, 0, 5, 4),
            new(0, -1, -1, 2, 5, 4),
            new(0, 0, 0, 4, 5, 2),
            new(1, 0, -1, 1, 1, 0),
            new(1, 0, 0, 0, 1, 3),
            new(1, 0, -1, 2, 1, 3),
            new(0, 0, 0, 3, 1, 2),
            new(0, 0, -1, 2, 2, 0),
            null,
            null,
            null,
            new(-1, 1, 0, 4, 4, 1),
            new(0, 1, 0, 1, 4, 5),
            new(-1, 1, 0, 3, 4, 5),
            new(0, 0, 0, 5, 4, 3),
            new(-1, 0, 1, 0, 0, 1),
            new(0, 0, 1, 1, 0, 2),
            new(-1, 0, 1, 3, 0, 2),
            new(0, 0, 0, 2, 0, 3),
            new(-1, -1, 0, 5, 5, 1),
            new(0, -1, 0, 1, 5, 4),
            new(-1, -1, 0, 3, 5, 4),
            new(0, 0, 0, 4, 5, 3),
            new(-1, 0, -1, 2, 2, 1),
            new(0, 0, -1, 1, 2, 0),
            new(-1, 0, -1, 3, 2, 0),
            new(0, 0, 0, 0, 2, 3),
            new(-1, 0, 0, 3, 3, 1),
            null,
            null,
            null,
            new(0, 1, 1, 4, 4, 2),
            new(0, 1, 0, 2, 4, 5),
            new(0, 1, 1, 0, 4, 5),
            new(0, 0, 0, 5, 4, 0),
            new(1, 0, 1, 1, 1, 2),
            new(1, 0, 0, 2, 1, 3),
            new(1, 0, 1, 0, 1, 3),
            new(0, 0, 0, 3, 1, 0),
            new(0, -1, 1, 5, 5, 2),
            new(0, -1, 0, 2, 5, 4),
            new(0, -1, 1, 0, 5, 4),
            new(0, 0, 0, 4, 5, 0),
            new(-1, 0, 1, 3, 3, 2),
            new(-1, 0, 0, 2, 3, 1),
            new(-1, 0, 1, 0, 3, 1),
            new(0, 0, 0, 1, 3, 0),
            new(0, 0, 1, 0, 0, 2),
            null,
            null,
            null,
            new(1, 1, 0, 4, 4, 3),
            new(0, 1, 0, 3, 4, 5),
            new(1, 1, 0, 1, 4, 5),
            new(0, 0, 0, 5, 4, 1),
            new(1, 0, -1, 2, 2, 3),
            new(0, 0, -1, 3, 2, 0),
            new(1, 0, -1, 1, 2, 0),
            new(0, 0, 0, 0, 2, 1),
            new(1, -1, 0, 5, 5, 3),
            new(0, -1, 0, 3, 5, 4),
            new(1, -1, 0, 1, 5, 4),
            new(0, 0, 0, 4, 5, 1),
            new(1, 0, 1, 0, 0, 3),
            new(0, 0, 1, 3, 0, 2),
            new(1, 0, 1, 1, 0, 2),
            new(0, 0, 0, 2, 0, 1),
            new(1, 0, 0, 1, 1, 3),
            null,
            null,
            null,
            new(0, -1, -1, 2, 2, 4),
            new(0, 0, -1, 4, 2, 0),
            new(0, -1, -1, 5, 2, 0),
            new(0, 0, 0, 0, 2, 5),
            new(-1, -1, 0, 3, 3, 4),
            new(-1, 0, 0, 4, 3, 1),
            new(-1, -1, 0, 5, 3, 1),
            new(0, 0, 0, 1, 3, 5),
            new(0, -1, 1, 0, 0, 4),
            new(0, 0, 1, 4, 0, 2),
            new(0, -1, 1, 5, 0, 2),
            new(0, 0, 0, 2, 0, 5),
            new(1, -1, 0, 1, 1, 4),
            new(1, 0, 0, 4, 1, 3),
            new(1, -1, 0, 5, 1, 3),
            new(0, 0, 0, 3, 1, 5),
            new(0, -1, 0, 5, 5, 4),
            null,
            null,
            null,
            new(0, 1, -1, 2, 2, 5),
            new(0, 0, -1, 5, 2, 0),
            new(0, 1, -1, 4, 2, 0),
            new(0, 0, 0, 0, 2, 4),
            new(1, 1, 0, 1, 1, 5),
            new(1, 0, 0, 5, 1, 3),
            new(1, 1, 0, 4, 1, 3),
            new(0, 0, 0, 3, 1, 4),
            new(0, 1, 1, 0, 0, 5),
            new(0, 0, 1, 5, 0, 2),
            new(0, 1, 1, 4, 0, 2),
            new(0, 0, 0, 2, 0, 4),
            new(-1, 1, 0, 3, 3, 5),
            new(-1, 0, 0, 5, 3, 1),
            new(-1, 1, 0, 4, 3, 1),
            new(0, 0, 0, 1, 3, 4),
            new(0, 1, 0, 4, 4, 5),
            null,
            null,
            null
        ];

        public static ElectricConnectorDirection?[] m_connectorDirectionsTable = [
            null,
            ElectricConnectorDirection.Right,
            ElectricConnectorDirection.In,
            ElectricConnectorDirection.Left,
            ElectricConnectorDirection.Top,
            ElectricConnectorDirection.Bottom,
            ElectricConnectorDirection.Left,
            null,
            ElectricConnectorDirection.Right,
            ElectricConnectorDirection.In,
            ElectricConnectorDirection.Top,
            ElectricConnectorDirection.Bottom,
            ElectricConnectorDirection.In,
            ElectricConnectorDirection.Left,
            null,
            ElectricConnectorDirection.Right,
            ElectricConnectorDirection.Top,
            ElectricConnectorDirection.Bottom,
            ElectricConnectorDirection.Right,
            ElectricConnectorDirection.In,
            ElectricConnectorDirection.Left,
            null,
            ElectricConnectorDirection.Top,
            ElectricConnectorDirection.Bottom,
            ElectricConnectorDirection.Bottom,
            ElectricConnectorDirection.Right,
            ElectricConnectorDirection.Top,
            ElectricConnectorDirection.Left,
            null,
            ElectricConnectorDirection.In,
            ElectricConnectorDirection.Top,
            ElectricConnectorDirection.Right,
            ElectricConnectorDirection.Bottom,
            ElectricConnectorDirection.Left,
            ElectricConnectorDirection.In,
            null
        ];

        public static int[] m_connectorFacesTable = [
            4,
            3,
            5,
            1,
            2,
            4,
            0,
            5,
            2,
            3,
            4,
            1,
            5,
            3,
            0,
            4,
            2,
            5,
            0,
            1,
            2,
            1,
            0,
            3,
            5,
            0,
            1,
            2,
            3,
            4
        ];

        public float m_remainingSimulationTime;

        public Dictionary<Point3, float> m_persistentElementsVoltages = [];

        public Dictionary<ElectricElement, bool> m_electricElements = [];

        public Dictionary<CellFace, ElectricElement> m_electricElementsByCellFace = [];

        public Dictionary<Point3, bool> m_pointsToUpdate = [];

        public Dictionary<Point3, ElectricElement> m_electricElementsToAdd = [];

        public Dictionary<ElectricElement, bool> m_electricElementsToRemove = [];

        public Dictionary<Point3, bool> m_wiresToUpdate = [];

        public List<Dictionary<ElectricElement, bool>> m_listsCache = [];

        public Dictionary<int, Dictionary<ElectricElement, bool>> m_futureSimulateLists = [];

        public Dictionary<ElectricElement, bool> m_nextStepSimulateList;

        public DynamicArray<ElectricConnectionPath> m_tmpConnectionPaths = [];

        public Dictionary<CellFace, bool> m_tmpVisited = [];

        public Dictionary<CellFace, bool> m_tmpResult = [];

        public static bool DebugDrawElectrics = false;

        public static int SimulatedElectricElements;

        public const float CircuitStepDuration = 0.01f;

        public Dictionary<Type, DebugInfo> m_debugInfos = [];
        public Stopwatch m_debugStopwatch = new();
        public bool UpdateTimeDebug = false;

        public SubsystemTime SubsystemTime { get; set; }

        public SubsystemTerrain SubsystemTerrain { get; set; }

        public SubsystemAudio SubsystemAudio { get; set; }

        public int FrameStartCircuitStep { get; set; }

        public int CircuitStep { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public void OnElectricElementBlockGenerated(int x, int y, int z) {
            m_pointsToUpdate[new Point3(x, y, z)] = false;
        }

        public void OnElectricElementBlockAdded(int x, int y, int z) {
            m_pointsToUpdate[new Point3(x, y, z)] = true;
        }

        public void OnElectricElementBlockRemoved(int x, int y, int z) {
            m_pointsToUpdate[new Point3(x, y, z)] = true;
        }

        public void OnElectricElementBlockModified(int x, int y, int z) {
            m_pointsToUpdate[new Point3(x, y, z)] = true;
        }

        public void OnChunkDiscarding(TerrainChunk chunk) {
            foreach (CellFace key in m_electricElementsByCellFace.Keys) {
                if (key.X >= chunk.Origin.X
                    && key.X < chunk.Origin.X + 16
                    && key.Z >= chunk.Origin.Y
                    && key.Z < chunk.Origin.Y + 16) {
                    m_pointsToUpdate[new Point3(key.X, key.Y, key.Z)] = false;
                }
            }
        }

        public static ElectricConnectorDirection? GetConnectorDirection(int mountingFace, int rotation, int connectorFace) {
            ElectricConnectorDirection? result = m_connectorDirectionsTable[6 * mountingFace + connectorFace];
            if (result.HasValue) {
                if (result.Value < ElectricConnectorDirection.In) {
                    return (ElectricConnectorDirection)((int)(result.Value + rotation) % 4);
                }
                return result;
            }
            return null;
        }

        public static int GetConnectorFace(int mountingFace, ElectricConnectorDirection connectionDirection) =>
            m_connectorFacesTable[(int)(5 * mountingFace + connectionDirection)];

        public void GetAllConnectedNeighbors(int x, int y, int z, int mountingFace, DynamicArray<ElectricConnectionPath> list) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue)] is not IElectricElementBlock electricElementBlock) {
                return;
            }
            for (ElectricConnectorDirection electricConnectorDirection = ElectricConnectorDirection.Top;
                electricConnectorDirection < (ElectricConnectorDirection)5;
                electricConnectorDirection++) {
                for (int i = 0; i < 4; i++) {
                    ElectricConnectionPath electricConnectionPath =
                        m_connectionPathsTable[20 * mountingFace + 4 * (int)electricConnectorDirection + i];
                    if (electricConnectionPath == null) {
                        break;
                    }
                    ElectricConnectorType? connectorType = electricElementBlock.GetConnectorType(
                        SubsystemTerrain,
                        cellValue,
                        mountingFace,
                        electricConnectionPath.ConnectorFace,
                        x,
                        y,
                        z
                    );
                    if (!connectorType.HasValue) {
                        break;
                    }
                    int x2 = x + electricConnectionPath.NeighborOffsetX;
                    int y2 = y + electricConnectionPath.NeighborOffsetY;
                    int z2 = z + electricConnectionPath.NeighborOffsetZ;
                    int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x2, y2, z2);
                    IElectricElementBlock electricElementBlock2 = BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)] as IElectricElementBlock;
                    if (electricElementBlock2 == null) {
                        continue;
                    }
                    ElectricConnectorType? connectorType2 = electricElementBlock2.GetConnectorType(
                        SubsystemTerrain,
                        cellValue2,
                        electricConnectionPath.NeighborFace,
                        electricConnectionPath.NeighborConnectorFace,
                        x2,
                        y2,
                        z2
                    );
                    if (connectorType2.HasValue
                        && ((connectorType.Value != 0 && connectorType2.Value != ElectricConnectorType.Output)
                            || (connectorType.Value != ElectricConnectorType.Output && connectorType2.Value != 0))) {
                        int connectionMask = electricElementBlock.GetConnectionMask(cellValue);
                        int connectionMask2 = electricElementBlock2.GetConnectionMask(cellValue2);
                        if ((connectionMask & connectionMask2) != 0) {
                            list.Add(electricConnectionPath);
                        }
                    }
                }
            }
        }

        public ElectricElement GetElectricElement(int x, int y, int z, int mountingFace) {
            m_electricElementsByCellFace.TryGetValue(new CellFace(x, y, z, mountingFace), out ElectricElement value);
            return value;
        }

        public void QueueElectricElementForSimulation(ElectricElement electricElement, int circuitStep) {
            if (circuitStep == CircuitStep + 1) {
                if (m_nextStepSimulateList == null
                    && !m_futureSimulateLists.TryGetValue(CircuitStep + 1, out m_nextStepSimulateList)) {
                    m_nextStepSimulateList = GetListFromCache();
                    m_futureSimulateLists.Add(CircuitStep + 1, m_nextStepSimulateList);
                }
                m_nextStepSimulateList[electricElement] = true;
            }
            else if (circuitStep > CircuitStep + 1) {
                if (!m_futureSimulateLists.TryGetValue(circuitStep, out Dictionary<ElectricElement, bool> value)) {
                    value = GetListFromCache();
                    m_futureSimulateLists.Add(circuitStep, value);
                }
                value[electricElement] = true;
            }
        }

        public void QueueElectricElementConnectionsForSimulation(ElectricElement electricElement, int circuitStep) {
            foreach (ElectricConnection connection in electricElement.Connections) {
                if (connection.ConnectorType != 0
                    && connection.NeighborConnectorType != ElectricConnectorType.Output) {
                    QueueElectricElementForSimulation(connection.NeighborElectricElement, circuitStep);
                }
            }
        }

        public float? ReadPersistentVoltage(Point3 point) {
            if (m_persistentElementsVoltages.TryGetValue(point, out float value)) {
                return value;
            }
            return null;
        }

        public void WritePersistentVoltage(Point3 point, float voltage) {
            m_persistentElementsVoltages[point] = voltage;
        }

        public virtual void Update(float dt) {
            FrameStartCircuitStep = CircuitStep;
            SimulatedElectricElements = 0;
            m_remainingSimulationTime = MathUtils.Min(m_remainingSimulationTime + dt, 0.1f);
            while (m_remainingSimulationTime >= 0.01f) {
                UpdateElectricElements();
                ++CircuitStep;
                m_remainingSimulationTime -= 0.01f;
                m_nextStepSimulateList = null;
                if (m_futureSimulateLists.Remove(CircuitStep, out Dictionary<ElectricElement, bool> value)) {
                    SimulatedElectricElements += value.Count;
                    if (UpdateTimeDebug) {
                        m_debugStopwatch.Start();
                    }
                    foreach (ElectricElement key in value.Keys) {
                        if (m_electricElements.ContainsKey(key)) {
                            long startTick = UpdateTimeDebug ? m_debugStopwatch.ElapsedTicks : 0;
                            Type type = key.GetType();
                            try {
                                SimulateElectricElement(key);
                            }
#pragma warning disable CS0168 // 声明了变量，但从未使用过
                            catch (Exception e) {
#pragma warning restore CS0168 // 声明了变量，但从未使用过
#if DEBUG
                                Console.WriteLine($"Error in simulating {type.Name}: {e}");
#endif
                                throw;
                            }
                            finally {
                                if (UpdateTimeDebug) {
                                    long ticksCosted = m_debugStopwatch.ElapsedTicks - startTick;
                                    if (!m_debugInfos.TryGetValue(type, out DebugInfo info)) {
                                        info = new DebugInfo();
                                        m_debugInfos.Add(type, info);
                                    }
                                    info.Counter++;
                                    info.TotalTicksCosted += ticksCosted;
                                    if (ticksCosted > info.MaxTicksCosted1) {
                                        info.MaxTicksCosted1 = ticksCosted;
                                    }
                                    else if (ticksCosted > info.MaxTicksCosted2) {
                                        info.MaxTicksCosted2 = ticksCosted;
                                    }
                                }
                            }
                        }
                    }
                    if (UpdateTimeDebug) {
                        m_debugStopwatch.Reset();
                    }
                    ReturnListToCache(value);
                }
            }
            if (DebugDrawElectrics) {
                DebugDraw();
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            SubsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            SubsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            string[] array = valuesDictionary.GetValue<string>("VoltagesByCell").Split([';'], StringSplitOptions.RemoveEmptyEntries);
            int num = 0;
            while (true) {
                if (num < array.Length) {
                    string[] array2 = array[num].Split([","], StringSplitOptions.None);
                    if (array2.Length != 4) {
                        break;
                    }
                    int x = int.Parse(array2[0], CultureInfo.InvariantCulture);
                    int y = int.Parse(array2[1], CultureInfo.InvariantCulture);
                    int z = int.Parse(array2[2], CultureInfo.InvariantCulture);
                    float value = float.Parse(array2[3], CultureInfo.InvariantCulture);
                    m_persistentElementsVoltages[new Point3(x, y, z)] = value;
                    num++;
                    continue;
                }
                return;
            }
            throw new InvalidOperationException("Invalid number of tokens.");
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            int num = 0;
            StringBuilder stringBuilder = new();
            foreach (KeyValuePair<Point3, float> persistentElementsVoltage in m_persistentElementsVoltages) {
                if (num > 500) {
                    break;
                }
                stringBuilder.Append(persistentElementsVoltage.Key.X.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append(persistentElementsVoltage.Key.Y.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append(persistentElementsVoltage.Key.Z.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(',');
                stringBuilder.Append(persistentElementsVoltage.Value.ToString(CultureInfo.InvariantCulture));
                stringBuilder.Append(';');
                num++;
            }
            valuesDictionary.SetValue("VoltagesByCell", stringBuilder.ToString());
            if (UpdateTimeDebug) {
                int maxTypeNameLength = 1;
                if (m_debugInfos.Keys.Count > 0) {
                    maxTypeNameLength = m_debugInfos.Keys.Max(type => type.FullName?.Length ?? 0) + 1;
                }
                StringBuilder stringBuilder2 = new();
                stringBuilder2.AppendLine("====== SubsystemElectricity Performance Analyze ======");
                stringBuilder2.Append("TypeName".PadRight(maxTypeNameLength));
                stringBuilder2.Append("    Counter   TotalTime AverageTime    MaxTime1    MaxTime2");
                foreach ((Type type, DebugInfo info) in m_debugInfos.OrderByDescending(pair => pair.Value.TotalTicksCosted)) {
                    stringBuilder2.AppendLine();
                    stringBuilder2.Append(type.FullName?.PadRight(maxTypeNameLength));
                    stringBuilder2.Append(info.Counter.ToString().PadLeft(11));
                    stringBuilder2.Append($"{(float)info.TotalTicksCosted / Stopwatch.Frequency * 1000:F}ms".PadLeft(12));
                    stringBuilder2.Append($"{(float)info.TotalTicksCosted / info.Counter / Stopwatch.Frequency * 1000000f:F}μs".PadLeft(12));
                    stringBuilder2.Append($"{(float)info.MaxTicksCosted1 / Stopwatch.Frequency * 1000000f:F}μs".PadLeft(12));
                    stringBuilder2.Append($"{(float)info.MaxTicksCosted2 / Stopwatch.Frequency * 1000000f:F}μs".PadLeft(12));
                }
                Log.Information(stringBuilder2.ToString());
                m_debugInfos.Clear();
            }
        }

        public static ElectricConnectionPath GetConnectionPath(int mountingFace, ElectricConnectorDirection localConnector, int neighborIndex) =>
            m_connectionPathsTable[16 * mountingFace + 4 * (int)localConnector + neighborIndex];

        public void SimulateElectricElement(ElectricElement electricElement) {
            if (electricElement.Simulate()) {
                QueueElectricElementConnectionsForSimulation(electricElement, CircuitStep + 1);
            }
        }

        public void AddElectricElement(ElectricElement electricElement) {
            m_electricElements.Add(electricElement, true);
            foreach (CellFace cellFace2 in electricElement.CellFaces) {
                m_electricElementsByCellFace.Add(cellFace2, electricElement);
                m_tmpConnectionPaths.Clear();
                GetAllConnectedNeighbors(cellFace2.X, cellFace2.Y, cellFace2.Z, cellFace2.Face, m_tmpConnectionPaths);
                foreach (ElectricConnectionPath tmpConnectionPath in m_tmpConnectionPaths) {
                    CellFace cellFace = new(
                        cellFace2.X + tmpConnectionPath.NeighborOffsetX,
                        cellFace2.Y + tmpConnectionPath.NeighborOffsetY,
                        cellFace2.Z + tmpConnectionPath.NeighborOffsetZ,
                        tmpConnectionPath.NeighborFace
                    );
                    if (m_electricElementsByCellFace.TryGetValue(cellFace, out ElectricElement value)
                        && value != electricElement) {
                        int cellValue = SubsystemTerrain.Terrain.GetCellValue(cellFace2.X, cellFace2.Y, cellFace2.Z);
                        int num = Terrain.ExtractContents(cellValue);
                        ElectricConnectorType value2 = ((IElectricElementBlock)BlocksManager.Blocks[num]).GetConnectorType(
                            SubsystemTerrain,
                            cellValue,
                            cellFace2.Face,
                            tmpConnectionPath.ConnectorFace,
                            cellFace2.X,
                            cellFace2.Y,
                            cellFace2.Z
                        )!.Value;
                        int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
                        int num2 = Terrain.ExtractContents(cellValue2);
                        ElectricConnectorType value3 = ((IElectricElementBlock)BlocksManager.Blocks[num2]).GetConnectorType(
                            SubsystemTerrain,
                            cellValue2,
                            cellFace.Face,
                            tmpConnectionPath.NeighborConnectorFace,
                            cellFace.X,
                            cellFace.Y,
                            cellFace.Z
                        )!.Value;
                        electricElement.Connections.Add(
                            new ElectricConnection {
                                CellFace = cellFace2,
                                ConnectorFace = tmpConnectionPath.ConnectorFace,
                                ConnectorType = value2,
                                NeighborElectricElement = value,
                                NeighborCellFace = cellFace,
                                NeighborConnectorFace = tmpConnectionPath.NeighborConnectorFace,
                                NeighborConnectorType = value3
                            }
                        );
                        value.Connections.Add(
                            new ElectricConnection {
                                CellFace = cellFace,
                                ConnectorFace = tmpConnectionPath.NeighborConnectorFace,
                                ConnectorType = value3,
                                NeighborElectricElement = electricElement,
                                NeighborCellFace = cellFace2,
                                NeighborConnectorFace = tmpConnectionPath.ConnectorFace,
                                NeighborConnectorType = value2
                            }
                        );
                    }
                }
            }
            QueueElectricElementForSimulation(electricElement, CircuitStep + 1);
            QueueElectricElementConnectionsForSimulation(electricElement, CircuitStep + 2);
            electricElement.OnAdded();
        }

        public void RemoveElectricElement(ElectricElement electricElement) {
            electricElement.OnRemoved();
            QueueElectricElementConnectionsForSimulation(electricElement, CircuitStep + 1);
            m_electricElements.Remove(electricElement);
            foreach (CellFace cellFace in electricElement.CellFaces) {
                m_electricElementsByCellFace.Remove(cellFace);
            }
            foreach (ElectricConnection connection in electricElement.Connections) {
                int num = connection.NeighborElectricElement.Connections.FirstIndex(c => c.NeighborElectricElement == electricElement);
                if (num >= 0) {
                    connection.NeighborElectricElement.Connections.RemoveAt(num);
                }
            }
        }

        public virtual void UpdateElectricElements() {
            foreach (KeyValuePair<Point3, bool> item in m_pointsToUpdate) {
                Point3 key = item.Key;
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z);
                for (int i = 0; i < 6; i++) {
                    ElectricElement electricElement = GetElectricElement(key.X, key.Y, key.Z, i);
                    if (electricElement != null) {
                        if (electricElement is WireDomainElectricElement) {
                            m_wiresToUpdate[key] = true;
                        }
                        else {
                            m_electricElementsToRemove[electricElement] = true;
                        }
                    }
                }
                if (item.Value) {
                    m_persistentElementsVoltages.Remove(key);
                }
                int num = Terrain.ExtractContents(cellValue);
                if (BlocksManager.Blocks[num] is IElectricWireElementBlock) {
                    m_wiresToUpdate[key] = true;
                }
                else {
                    if (BlocksManager.Blocks[num] is IElectricElementBlock electricElementBlock) {
                        ElectricElement electricElement2 = electricElementBlock.CreateElectricElement(this, cellValue, key.X, key.Y, key.Z);
                        if (electricElement2 != null) {
                            m_electricElementsToAdd[key] = electricElement2;
                        }
                    }
                }
            }
            RemoveWireDomains();
            foreach (KeyValuePair<ElectricElement, bool> item2 in m_electricElementsToRemove) {
                RemoveElectricElement(item2.Key);
            }
            AddWireDomains();
            foreach (ElectricElement value in m_electricElementsToAdd.Values) {
                AddElectricElement(value);
            }
            m_pointsToUpdate.Clear();
            m_wiresToUpdate.Clear();
            m_electricElementsToAdd.Clear();
            m_electricElementsToRemove.Clear();
        }

        public void AddWireDomains() {
            m_tmpVisited.Clear();
            foreach (Point3 key in m_wiresToUpdate.Keys) {
                for (int i = key.X - 1; i <= key.X + 1; i++) {
                    for (int j = key.Y - 1; j <= key.Y + 1; j++) {
                        for (int k = key.Z - 1; k <= key.Z + 1; k++) {
                            for (int l = 0; l < 6; l++) {
                                m_tmpResult.Clear();
                                ScanWireDomain(new CellFace(i, j, k, l), m_tmpVisited, m_tmpResult);
                                if (m_tmpResult.Count > 0) {
                                    WireDomainElectricElement electricElement = new(this, m_tmpResult.Keys);
                                    AddElectricElement(electricElement);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RemoveWireDomains() {
            foreach (Point3 key in m_wiresToUpdate.Keys) {
                for (int i = key.X - 1; i <= key.X + 1; i++) {
                    for (int j = key.Y - 1; j <= key.Y + 1; j++) {
                        for (int k = key.Z - 1; k <= key.Z + 1; k++) {
                            for (int l = 0; l < 6; l++) {
                                if (m_electricElementsByCellFace.TryGetValue(new CellFace(i, j, k, l), out ElectricElement value)
                                    && value is WireDomainElectricElement) {
                                    RemoveElectricElement(value);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ScanWireDomain(CellFace startCellFace, Dictionary<CellFace, bool> visited, Dictionary<CellFace, bool> result) {
            DynamicArray<CellFace> dynamicArray = [startCellFace];
            while (dynamicArray.Count > 0) {
                CellFace key = dynamicArray.Array[--dynamicArray.Count];
                if (visited.ContainsKey(key)) {
                    continue;
                }
                TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(key.X, key.Z);
                if (chunkAtCell == null
                    || !chunkAtCell.AreBehaviorsNotified) {
                    continue;
                }
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z);
                int num = Terrain.ExtractContents(cellValue);
                if (BlocksManager.Blocks[num] is not IElectricWireElementBlock electricWireElementBlock) {
                    continue;
                }
                int connectedWireFacesMask = electricWireElementBlock.GetConnectedWireFacesMask(cellValue, key.Face);
                if (connectedWireFacesMask == 0) {
                    continue;
                }
                for (int i = 0; i < 6; i++) {
                    if ((connectedWireFacesMask & (1 << i)) != 0) {
                        CellFace key2 = new(key.X, key.Y, key.Z, i);
                        visited.Add(key2, true);
                        result.Add(key2, true);
                        m_tmpConnectionPaths.Clear();
                        GetAllConnectedNeighbors(key2.X, key2.Y, key2.Z, key2.Face, m_tmpConnectionPaths);
                        foreach (ElectricConnectionPath tmpConnectionPath in m_tmpConnectionPaths) {
                            int x = key2.X + tmpConnectionPath.NeighborOffsetX;
                            int y = key2.Y + tmpConnectionPath.NeighborOffsetY;
                            int z = key2.Z + tmpConnectionPath.NeighborOffsetZ;
                            dynamicArray.Add(new CellFace(x, y, z, tmpConnectionPath.NeighborFace));
                        }
                    }
                }
            }
        }

        public Dictionary<ElectricElement, bool> GetListFromCache() {
            if (m_listsCache.Count > 0) {
                Dictionary<ElectricElement, bool> result = m_listsCache[^1];
                m_listsCache.RemoveAt(m_listsCache.Count - 1);
                return result;
            }
            return [];
        }

        public void ReturnListToCache(Dictionary<ElectricElement, bool> list) {
            list.Clear();
            m_listsCache.Add(list);
        }

        public void DebugDraw() { }
    }
}