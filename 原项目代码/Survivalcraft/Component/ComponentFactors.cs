using GameEntitySystem;
using TemplatesDatabase;
using static Game.ComponentLevel;

namespace Game {
    public class ComponentFactors : Component, IUpdateable {
        public Random m_random = new();

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemTime m_subsystemTime;

        /// <summary>
        ///     模组如果有自定义的Factors，可以使用这个OtherFactors。例如使用OtherFactors["AttackRate"]来定义攻击频率。
        /// </summary>
        public Dictionary<string, List<Factor>> OtherFactors = new();

        public Dictionary<string, float> OtherFactorsResults = new();

        /// <summary>
        ///     这四个Factors是可以调整的影响因素
        /// </summary>
        public List<Factor> m_strengthFactors = [];

        public List<Factor> m_speedFactors = [];
        public List<Factor> m_hungerFactors = [];
        public List<Factor> m_resilienceFactors = [];
        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public static string fName = "ComponentFactors";

        public virtual float GetOtherFactorResult(string factorName, bool recalculate = false, bool throwIfNotFound = false) {
            if (!recalculate
                && OtherFactorsResults.TryGetValue(factorName, out float result)) {
                return result;
            }
            if (!recalculate && throwIfNotFound) {
                throw new KeyNotFoundException($"Required factor result with name {factorName} is not found.");
            }
            bool factorsGotten = OtherFactors.TryGetValue(factorName, out List<Factor> factors);
            if (!factorsGotten) {
                if (throwIfNotFound) {
                    throw new KeyNotFoundException($"Required factor key with name {factorName} is not found.");
                }
                return 1f;
            }
            return CalculateFactorsResult(factors);
        }

        public float StrengthFactor {
            get;
            [Obsolete("模组调整StrengthFactor的具体数值，需要通过m_strengthFactors里面增删改里面的Factor")]
            set;
        } = 1f;

        public float ResilienceFactor {
            get;
            [Obsolete("模组调整ResilienceFactor的具体数值，需要通过m_resilienceFactors里面增删改里面的Factor")]
            set;
        } = 1f;

        public float SpeedFactor {
            get;
            [Obsolete("模组调整SpeedFactor的具体数值，需要通过m_speedFactors里面增删改里面的Factor")]
            set;
        } = 1f;

        public float HungerFactor {
            get;
            [Obsolete("模组调整HungerFactor的具体数值，需要通过m_hungerFactors里面增删改里面的Factor")]
            set;
        } = 1f;

        /// <summary>
        ///     AttackSpeed: 生物攻速
        ///     DigSpeed: 挖掘速度
        ///     ChaseRange: 非玩家生物的仇恨距离
        /// </summary>
        /// <param name="valuesDictionary"></param>
        /// <param name="idToEntityMap"></param>
        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            OtherFactors["AttackSpeed"] = new List<Factor>();
            OtherFactors["DigSpeed"] = new List<Factor>();
            OtherFactors["ChaseRange"] = new List<Factor>();
            CalculateOtherFactorsResult();
        }

        public static float CalculateFactorsResult(ICollection<Factor> factors) {
            float ans = 1f;
            foreach (var factor in factors) {
                if (float.IsNaN(factor.Value) || float.IsInfinity(factor.Value)) {
                    continue;
                }
                switch (factor.FactorAdditionType) {
                    case FactorAdditionType.Multiply: {
                            ans *= factor.Value;
                            break;
                        }
                    case FactorAdditionType.Add: {
                            ans += factor.Value;
                            break;
                        }
                }
            }
            if (float.IsNaN(ans) || float.IsInfinity(ans)) {
                ans = 1f;
            }
            return ans;
        }

        public virtual void CalculateOtherFactorsResult() {
            foreach ((string key, List<Factor> factors) in OtherFactors) {
                OtherFactorsResults[key] = CalculateFactorsResult(factors);
            }
        }

        public virtual void GenerateStrengthFactors() {
            m_strengthFactors.Clear();
        }

        public virtual void GenerateResilienceFactors() {
            m_resilienceFactors.Clear();
        }

        public virtual void GenerateSpeedFactors() {
            m_speedFactors.Clear();
        }

        public virtual void GenerateHungerFactors() {
            m_hungerFactors.Clear();
        }

        public virtual void GenerateOtherFactors() {
            foreach (string key in OtherFactors.Keys) {
                OtherFactors[key].Clear();
            }
        }

        #region Obsolete CalculateFactor

        [Obsolete("Get m_strengthFactors and StrengthFactor instead.")]
        public virtual float CalculateStrengthFactor(ICollection<Factor> factors) {
            if (factors is List<Factor> factorsList) {
                factorsList.AddRange(m_strengthFactors);
            }
            return CalculateFactorsResult(m_strengthFactors);
        }

        [Obsolete("Get m_resilienceFactors and ResilienceFactor instead.")]
        public virtual float CalculateResilienceFactor(ICollection<Factor> factors) {
            if (factors is List<Factor> factorsList) {
                factorsList.AddRange(m_resilienceFactors);
            }
            return CalculateFactorsResult(m_resilienceFactors);
        }

        [Obsolete("Get m_speedFactors and SpeedFactor instead.")]
        public virtual float CalculateSpeedFactor(ICollection<Factor> factors) {
            if (factors is List<Factor> factorsList) {
                factorsList.AddRange(m_speedFactors);
            }
            return CalculateFactorsResult(m_speedFactors);
        }

        [Obsolete("Get m_hungerFactors and HungerFactor instead.")]
        public virtual float CalculateHungerFactor(ICollection<Factor> factors) {
            if (factors is List<Factor> factorsList) {
                factorsList.AddRange(m_hungerFactors);
            }
            return CalculateFactorsResult(m_hungerFactors);
        }

        #endregion

        /// <summary>
        ///     对等级系统的更新进行了调整。
        ///     第一步是计算上一帧Factors的最终结果，并进行赋值。此时已经经过了所有模组的修改。
        ///     第二步是GenerateFactors对四个属性进行生成，此时四个m_xxxFactors会拥有初始值。
        ///     再往后面则是各模组对Factors的增删改。
        /// </summary>
        /// <param name="dt"></param>
        public virtual void Update(float dt) {
#pragma warning disable CS0618
            StrengthFactor = CalculateFactorsResult(m_strengthFactors);
            SpeedFactor = CalculateFactorsResult(m_speedFactors);
            HungerFactor = CalculateFactorsResult(m_hungerFactors);
            ResilienceFactor = CalculateFactorsResult(m_resilienceFactors);
#pragma warning restore CS0618
            CalculateOtherFactorsResult();
            GenerateStrengthFactors();
            GenerateResilienceFactors();
            GenerateSpeedFactors();
            GenerateHungerFactors();
            GenerateOtherFactors();
            ModsManager.HookAction(
                "OnFactorsUpdate",
                Loader => {
#pragma warning disable CS0618
                    Loader.OnFactorsUpdate(this, dt);
#pragma warning restore CS0618
                    return false;
                }
            );
        }
    }
}