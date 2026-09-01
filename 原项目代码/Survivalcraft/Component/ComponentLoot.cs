using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentLoot : Component, IUpdateable {
        public struct Loot {
            public Loot() { }
            public int Value;

            public int MinCount;

            public int MaxCount;

            public float Probability;

            /// <summary>
            ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
            /// </summary>
            public ValuesDictionary ValuesDictionaryForMods = new();
        }

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemPickables m_subsystemPickables;

        public ComponentCreature m_componentCreature;

        public List<Loot> m_lootList;

        public List<Loot> m_lootOnFireList;

        public Random m_random = new();

        public bool m_lootDropped;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public static List<Loot> ParseLootList(ValuesDictionary lootVd) {
            List<Loot> list = new();
            foreach (string value in lootVd.Values) {
                list.Add(ParseLoot(value));
            }
            list.Sort((l1, l2) => l1.Value - l2.Value);
            return list;
        }

        public virtual void Update(float dt) {
            if (!m_lootDropped
                && m_componentCreature.ComponentHealth.DeathTime.HasValue
                && m_subsystemGameInfo.TotalElapsedGameTime
                >= m_componentCreature.ComponentHealth.DeathTime.Value + m_componentCreature.ComponentHealth.CorpseDuration) {
                bool num = m_componentCreature.Entity.FindComponent<ComponentOnFire>()?.IsOnFire ?? false;
                m_lootDropped = true;
                List<BlockDropValue> blockDropValues = [];
                foreach (Loot item in num ? m_lootOnFireList : m_lootList) {
                    if (m_random.Float(0f, 1f) < item.Probability) {
                        int num2 = m_random.Int(item.MinCount, item.MaxCount);
                        for (int i = 0; i < num2; i++) {
                            blockDropValues.Add(new BlockDropValue { Value = item.Value, Count = 1 });
                        }
                    }
                }
                ModsManager.HookAction(
                    "DecideLoot",
                    loader => {
                        loader.DecideLoot(this, blockDropValues);
                        return false;
                    }
                );
                Vector3 position = (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max) / 2f;
                foreach (BlockDropValue blockDropValue in blockDropValues) {
                    m_subsystemPickables.AddPickable(blockDropValue.Value, blockDropValue.Count, position, null, null, Entity);
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_lootDropped = valuesDictionary.GetValue<bool>("LootDropped");
            m_lootList = ParseLootList(valuesDictionary.GetValue<ValuesDictionary>("Loot"));
            m_lootOnFireList = ParseLootList(valuesDictionary.GetValue<ValuesDictionary>("LootOnFire"));
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("LootDropped", m_lootDropped);
        }

        public static Loot ParseLoot(string lootString) {
            string[] array = lootString.Split([";"], StringSplitOptions.None);
            if (array.Length >= 3) {
                try {
                    int v = CraftingRecipesManager.DecodeResult(array[0]);
                    Loot result = default;
                    result.Value = v;
                    result.MinCount = int.Parse(array[1]);
                    result.MaxCount = int.Parse(array[2]);
                    result.Probability = array.Length >= 4 ? float.Parse(array[3]) : 1f;
                    return result;
                }
                catch {
                    return default;
                }
            }
            throw new InvalidOperationException("Invalid loot string.");
        }
    }
}