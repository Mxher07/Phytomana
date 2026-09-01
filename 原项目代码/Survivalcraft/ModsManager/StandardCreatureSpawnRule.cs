using Engine;
using TemplatesDatabase;

namespace Game {
    // CreatureType 的方便封装，方便从 XML 直接编写生物生成规则
    // 你也能参考此类实现一个自定义规则类，需要具体实现参数为 (SubsystemCreatureSpawn subsystem, ValuesDictionary valuesDictionary) 的构造方法
    // 也可以选择继承此类，覆写 GetSpawnSuitability 方法来添加更多规则，覆写 Spawn 方法来生成更多生物
    public class StandardCreatureSpawnRule : SubsystemCreatureSpawn.CreatureType {
        public SubsystemCreatureSpawn m_subsystem;

        //以下是生成条件，满足时 SpawnSuitabilityFunction 将返回 Suitability
        public int MinTemperature;
        public int MaxTemperature;
        public int MinHumidity;
        public int MaxHumidity;
        public bool AboveTopBlock;
        public float MinShoreDistance;
        public float MaxShoreDistance;
        public List<Type> BlockTypes = [];

        /// <summary>
        /// 条件满足时 SpawnSuitabilityFunction 的返回值
        /// </summary>
        public float Suitability;

        /// <summary>
        /// 要生成的生物数量
        /// </summary>
        public int Count;

        readonly bool m_ignoreShaftValue;
        readonly bool m_ignoreShoreDistance;

        public StandardCreatureSpawnRule(SubsystemCreatureSpawn subsystem, ValuesDictionary valuesDictionary): base(subsystem, valuesDictionary) {
            m_subsystem = subsystem;
            Name = valuesDictionary.GetValue<string>("Name");
            SpawnLocationType = valuesDictionary.GetValue("SpawnLocationType", SpawnLocationType.Surface);
            RandomSpawn = valuesDictionary.GetValue("RandomSpawn", false);
            ConstantSpawn = valuesDictionary.GetValue("ConstantSpawn", false);
            MinTemperature = valuesDictionary.GetValue("MinTemperature", 0);
            MaxTemperature = valuesDictionary.GetValue("MaxTemperature", 15);
            MinHumidity = valuesDictionary.GetValue("MinHumidity", 0);
            MaxHumidity = valuesDictionary.GetValue("MaxHumidity", 15);
            AboveTopBlock = valuesDictionary.GetValue("AboveTopBlock", false);
            m_ignoreShaftValue = !AboveTopBlock && MinTemperature <= 0 && MaxTemperature >= 15 && MinHumidity <= 0 && MaxHumidity >= 15;
            MinShoreDistance = valuesDictionary.GetValue("MinShoreDistance", float.NegativeInfinity);
            MaxShoreDistance = valuesDictionary.GetValue("MaxShoreDistance", float.PositiveInfinity);
            m_ignoreShoreDistance = MinShoreDistance == float.NegativeInfinity && MaxShoreDistance == float.PositiveInfinity;
            Suitability = valuesDictionary.GetValue("Suitability", 1f);
            Count = valuesDictionary.GetValue("Count", 1);
            string blocksString = valuesDictionary.GetValue("Blocks", string.Empty);
            foreach (string typeName in blocksString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                if (BlocksManager.BlockNameToIndex.TryGetValue(typeName, out int index)) {
                    BlockTypes.Add(BlocksManager.Blocks[index].GetType());
                }
            }
            SpawnSuitabilityFunction = GetSpawnSuitability;
            SpawnFunction = Spawn;
        }

        public virtual float GetSpawnSuitability(SubsystemCreatureSpawn.CreatureType _, Point3 position) {
            TerrainChunk chunk = null;
            if (!m_ignoreShaftValue) {
                chunk = m_subsystem.m_subsystemTerrain.Terrain.GetChunkAtCell(position);
                if (chunk == null) {
                    return 0f;
                }
                int shaft = chunk.GetShaftValueFast(position.X & 15, position.Z & 15);
                int temperature = Terrain.ExtractTemperature(shaft);
                if (temperature < MinTemperature
                    || temperature > MaxTemperature) {
                    return 0f;
                }
                int humidity = Terrain.ExtractHumidity(shaft);
                if (humidity < MinHumidity
                    || humidity > MaxHumidity) {
                    return 0f;
                }
                int topHeight = Terrain.ExtractTopHeight(shaft);
                if (AboveTopBlock && position.Y < topHeight) {
                    return 0f;
                }
            }
            if (BlockTypes.Count > 0) {
                chunk ??= m_subsystem.m_subsystemTerrain.Terrain.GetChunkAtCell(position);
                if (chunk == null) {
                    return 0f;
                }
                Type blockType = BlocksManager.Blocks[chunk.GetCellContentsFast(position.X & 15, position.Y - 1, position.Z & 15)].GetType();
                bool notIncluded = true;
                foreach (Type type in BlockTypes) {
                    if (blockType == type
                        || blockType.IsSubclassOf(type)) {
                        notIncluded = false;
                        break;
                    }
                }
                if (notIncluded) {
                    return 0f;
                }
            }
            if (!m_ignoreShoreDistance) {
                float distance = m_subsystem.m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(position.X, position.Z);
                return distance >= MinShoreDistance && distance <= MaxShoreDistance ? Suitability : 0f;
            }
            return Suitability;
        }

        public virtual int Spawn(SubsystemCreatureSpawn.CreatureType creatureType, Point3 position) =>
            m_subsystem.SpawnCreatures(creatureType, Name, position, Count).Count;
    }
}