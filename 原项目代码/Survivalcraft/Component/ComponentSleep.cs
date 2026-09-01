using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentSleep : Component, IUpdateable {
        public Dictionary<string, Func<string>> m_conditionsToSleep = [];

        public SubsystemPlayers m_subsystemPlayers;

        public SubsystemSky m_subsystemSky;

        public SubsystemTime m_subsystemTime;

        public SubsystemUpdate m_subsystemUpdate;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTimeOfDay m_subsystemTimeOfDay;

        public SubsystemTerrain m_subsystemTerrain;

        public ComponentPlayer m_componentPlayer;

        public double? m_sleepStartTime;

        public float m_sleepFactor;

        public bool m_allowManualWakeUp;
        public static string fName = "ComponentSleep";
        public float m_minWetness;

        public float m_messageFactor;

        public bool IsSleeping => m_sleepStartTime.HasValue;

        public float SleepFactor => m_sleepFactor;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public double m_minAutoSleepTime = 180;
        public bool m_wakeUpWhenWet = true;
        public bool m_wakeUpWhenInjured = true;
        public bool m_wakeUpWhenAttacked = true;
        public float MaxSleepBlackoutFactor = 1f;

        /// <summary>
        ///     为了模组间兼容性，该方法不再建议覆盖。
        ///     控制睡觉需要的条件，需要通过控制m_conditionsToSleep更改，具体查看Load()中的添加。
        ///     模组作者可以在自己的Component中添加对应的m_conditionsToSleep
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool CanSleep(out string reason) {
            foreach (string condition in m_conditionsToSleep.Keys) {
                string reasonUnableToSleep = m_conditionsToSleep[condition]();
                if (!string.IsNullOrEmpty(reasonUnableToSleep)) {
                    reason = reasonUnableToSleep;
                    return false;
                }
            }
            reason = string.Empty;
            return true;
        }

        public virtual void Sleep(bool allowManualWakeup) {
            if (!IsSleeping) {
                m_sleepStartTime = m_subsystemGameInfo.TotalElapsedGameTime;
                m_allowManualWakeUp = allowManualWakeup;
                m_minWetness = float.MaxValue;
                m_messageFactor = 0f;
                if (m_componentPlayer.PlayerStats != null) {
                    m_componentPlayer.PlayerStats.TimesWentToSleep++;
                }
            }
        }

        public virtual void WakeUp() {
            if (m_sleepStartTime.HasValue) {
                m_sleepStartTime = null;
                m_componentPlayer.PlayerData.SpawnPosition = m_componentPlayer.ComponentBody.Position + new Vector3(0f, 0.1f, 0f);
            }
        }

        public virtual void Update(float dt) {
            if (IsSleeping && m_componentPlayer.ComponentHealth.Health > 0f) {
                m_sleepFactor = MathUtils.Min(m_sleepFactor + 0.33f * Time.FrameDuration, 1f);
                m_minWetness = MathUtils.Min(m_minWetness, m_componentPlayer.ComponentVitalStats.Wetness);
                m_componentPlayer.PlayerStats.TimeSlept += m_subsystemGameInfo.TotalElapsedGameTimeDelta;
                if ((m_componentPlayer.ComponentVitalStats.Sleep >= 1f || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
                    && IntervalUtils.IsBetween(
                        m_subsystemTimeOfDay.TimeOfDay,
                        m_subsystemTimeOfDay.DayStart,
                        IntervalUtils.Add(m_subsystemTimeOfDay.DuskStart, -0.1f)
                    )
                    && m_sleepStartTime.HasValue
                    && m_subsystemGameInfo.TotalElapsedGameTime > m_sleepStartTime + m_minAutoSleepTime) {
                    WakeUp();
                }
                if (m_wakeUpWhenInjured
                    && m_componentPlayer.ComponentHealth.HealthChange < 0f
                    && (m_componentPlayer.ComponentHealth.Health < 0.5f || m_componentPlayer.ComponentVitalStats.Sleep > 0.5f)) {
                    WakeUp();
                }
                if (m_wakeUpWhenWet
                    && m_componentPlayer.ComponentVitalStats.Wetness > m_minWetness + 0.05f
                    && m_componentPlayer.ComponentVitalStats.Sleep > 0.2f) {
                    WakeUp();
                    m_subsystemTime.QueueGameTimeDelayedExecution(
                        m_subsystemTime.GameTime + 1.0,
                        delegate { m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 6), Color.White, true, true); }
                    );
                }
                if (m_sleepStartTime.HasValue) {
                    float num = (float)(m_subsystemGameInfo.TotalElapsedGameTime - m_sleepStartTime.Value);
                    if (m_allowManualWakeUp && num > 10f) {
                        if (m_componentPlayer.GameWidget.Input.Any
                            && !DialogsManager.HasDialogs(m_componentPlayer.GameWidget)) {
                            m_componentPlayer.GameWidget.Input.Clear();
                            WakeUp();
                            m_subsystemTime.QueueGameTimeDelayedExecution(
                                m_subsystemTime.GameTime + 2.0,
                                delegate {
                                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 7), Color.White, true, false);
                                }
                            );
                        }
                        m_messageFactor = MathUtils.Min(m_messageFactor + 0.5f * Time.FrameDuration, 1f);
                        m_componentPlayer.ComponentScreenOverlays.Message = LanguageControl.Get(fName, 8);
                        m_componentPlayer.ComponentScreenOverlays.MessageFactor = m_messageFactor;
                    }
                    if (!m_allowManualWakeUp
                        && num > 5f) {
                        m_messageFactor = MathUtils.Min(m_messageFactor + 1f * Time.FrameDuration, 1f);
                        m_componentPlayer.ComponentScreenOverlays.Message = LanguageControl.Get(fName, 9);
                        m_componentPlayer.ComponentScreenOverlays.MessageFactor = m_messageFactor;
                    }
                }
            }
            else {
                m_sleepFactor = MathUtils.Max(m_sleepFactor - 1f * Time.FrameDuration, 0f);
            }
            float sleepBlackoutFactor = MathUtils.Min(m_sleepFactor, MaxSleepBlackoutFactor);
            m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathUtils.Max(
                m_componentPlayer.ComponentScreenOverlays.BlackoutFactor,
                sleepBlackoutFactor
            );
            if (m_sleepFactor > 0.01f) {
                m_componentPlayer.ComponentScreenOverlays.FloatingMessage = LanguageControl.Get(fName, 10);
                m_componentPlayer.ComponentScreenOverlays.FloatingMessageFactor = MathUtils.Saturate(10f * (m_sleepFactor - 0.9f));
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemUpdate = Project.FindSubsystem<SubsystemUpdate>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTimeOfDay = Project.FindSubsystem<SubsystemTimeOfDay>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_sleepStartTime = valuesDictionary.GetValue<double>("SleepStartTime");
            m_allowManualWakeUp = valuesDictionary.GetValue<bool>("AllowManualWakeUp");
            m_componentPlayer.ComponentBody.Attacked += delegate {
                if (m_wakeUpWhenAttacked
                    && IsSleeping
                    && m_componentPlayer.ComponentVitalStats.Sleep > 0.25f) {
                    WakeUp();
                }
            };
            if (m_sleepStartTime == 0.0) {
                m_sleepStartTime = null;
            }
            if (m_sleepStartTime.HasValue) {
                m_sleepFactor = 1f;
                m_minWetness = float.MaxValue;
            }
            m_conditionsToSleep.Add(
                "OnDryLand",
                delegate {
                    Block block = m_componentPlayer.ComponentBody.StandingOnValue.HasValue
                        ? BlocksManager.Blocks[Terrain.ExtractContents(m_componentPlayer.ComponentBody.StandingOnValue.Value)]
                        : null;
                    if (block == null
                        || m_componentPlayer.ComponentBody.ImmersionDepth > 0f) {
                        return LanguageControl.Get(fName, 1);
                    }
                    return string.Empty;
                }
            );
            m_conditionsToSleep.Add(
                "BlockIsComfortable",
                delegate {
                    Block block = m_componentPlayer.ComponentBody.StandingOnValue.HasValue
                        ? BlocksManager.Blocks[Terrain.ExtractContents(m_componentPlayer.ComponentBody.StandingOnValue.Value)]
                        : null;
                    if (block != null
                        && block.SleepSuitability == 0f) {
                        return LanguageControl.Get(fName, 2);
                    }
                    return string.Empty;
                }
            );
            m_conditionsToSleep.Add(
                "TiredEnough",
                delegate {
                    if (m_componentPlayer.ComponentVitalStats.Sleep > 0.99f) {
                        return LanguageControl.Get(fName, 3);
                    }
                    return string.Empty;
                }
            );
            m_conditionsToSleep.Add(
                "NotTooWet",
                delegate {
                    if (m_componentPlayer.ComponentVitalStats.Wetness > 0.95f) {
                        return LanguageControl.Get(fName, 4);
                    }
                    return string.Empty;
                }
            );
            m_conditionsToSleep.Add(
                "Ceiling",
                delegate {
                    for (int i = -1; i <= 1; i++) {
                        for (int j = -1; j <= 1; j++) {
                            Vector3 start = m_componentPlayer.ComponentBody.Position + new Vector3(i, 1f, j);
                            Vector3 end = new(start.X, 255f, start.Z);
                            if (!m_subsystemTerrain.Raycast(start, end, false, true, (value, _) => Terrain.ExtractContents(value) != 0).HasValue) {
                                return LanguageControl.Get(fName, 5);
                            }
                        }
                    }
                    return string.Empty;
                }
            );
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("SleepStartTime", m_sleepStartTime ?? 0.0);
            valuesDictionary.SetValue("AllowManualWakeUp", m_allowManualWakeUp);
        }
    }
}