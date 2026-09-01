using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemTime : Subsystem {
        public struct DelayedExecutionRequest {
            public double GameTime;

            public Action Action;
        }

        public float? m_maxGameTimeDelta;
        public float? m_maxFixedGameTimeDelta;

        public float MaxGameTimeDelta {
            get => m_maxGameTimeDelta ?? (1f / SettingsManager.LowFPSToTimeDeceleration);
            set => m_maxGameTimeDelta = value;
        }

        public float MaxFixedGameTimeDelta {
            get => m_maxFixedGameTimeDelta ?? (1f / SettingsManager.LowFPSToTimeDeceleration);
            set => m_maxFixedGameTimeDelta = value;
        }

        public float DefaultFixedTimeStep = 0.05f;
        public int DefaultFixedUpdateStep = 20;

        public float GameMenuDialogTimeFactor = 0f;

        public double m_gameTime;

        public float m_gameTimeDelta;

        public float m_prevGameTimeDelta;

        public float m_gameTimeFactor = 1f;

        public float BasicGameTimeFactor { get; set; } = 1f;

        public float? m_gameTimeFactorSleep = 60f;

        public HashSet<DelayedExecutionRequest> m_delayedExecutionsRequests = [];

        public SubsystemPlayers m_subsystemPlayers;

        public SubsystemUpdate m_subsystemUpdate;

        public double GameTime => m_gameTime;

        public float GameTimeDelta => m_gameTimeDelta;

        public float PreviousGameTimeDelta => m_prevGameTimeDelta;

        public float GameTimeFactor {
            get => m_gameTimeFactor;
            set => m_gameTimeFactor = Math.Clamp(value, 0f, 256f);
        }

        public float? FixedTimeStep { get; private set; }

        public virtual float CalculateGameTimeDalta() {
            if (FixedTimeStep.HasValue) {
                return MathUtils.Min(FixedTimeStep.Value, MaxFixedGameTimeDelta) * m_gameTimeFactor;
            }
            return MathUtils.Min(Time.FrameDuration, MaxGameTimeDelta) * m_gameTimeFactor;
        }

        public virtual bool IsAllPlayerLivingSleeping() {
            try {
                int numSleepingPlayers = 0;
                int numDeadPlayers = 0;
                foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers) {
                    if (componentPlayer.ComponentHealth.Health == 0f) {
                        numDeadPlayers++;
                    }
                    else if (componentPlayer.ComponentSleep.SleepFactor == 1f) {
                        numSleepingPlayers++;
                    }
                }
                return numSleepingPlayers + numDeadPlayers == m_subsystemPlayers.ComponentPlayers.Count && numSleepingPlayers >= 1;
            }
            catch (Exception) {
                return false;
            }
        }

        public virtual void NextFrame() {
            m_prevGameTimeDelta = m_gameTimeDelta;
            m_gameTimeDelta = CalculateGameTimeDalta();
            ModsManager.HookAction(
                "ChangeGameTimeDelta",
                loader => {
                    loader.ChangeGameTimeDelta(this, ref m_gameTimeDelta);
                    return false;
                }
            );
            m_gameTime += m_gameTimeDelta;
            HashSet<DelayedExecutionRequest> toRemove = [];
            foreach (DelayedExecutionRequest delayedExecutionRequest in m_delayedExecutionsRequests) {
                if (delayedExecutionRequest.GameTime >= 0
                    && GameTime >= delayedExecutionRequest.GameTime) {
                    toRemove.Add(delayedExecutionRequest);
                    delayedExecutionRequest.Action();
                }
            }
            foreach (DelayedExecutionRequest delayedExecutionRequest in toRemove) {
                m_delayedExecutionsRequests.Remove(delayedExecutionRequest);
            }
            if (IsAllPlayerLivingSleeping()) {
                if (SettingsManager.UseAPISleepTimeAcceleration) {
                    if (m_gameTimeFactorSleep != null) {
                        m_gameTimeFactor = m_gameTimeFactorSleep.Value;
                    }
                }
                else {
                    FixedTimeStep = DefaultFixedTimeStep;
                    m_subsystemUpdate.UpdatesPerFrame = DefaultFixedUpdateStep;
                }
            }
            else {
                FixedTimeStep = null;
                m_subsystemUpdate.UpdatesPerFrame = 1;
                if (m_gameTimeFactorSleep != null) {
                    m_gameTimeFactor = BasicGameTimeFactor;
                }
            }
            bool flag = true;
            foreach (ComponentPlayer componentPlayer2 in m_subsystemPlayers.ComponentPlayers) {
                if (!componentPlayer2.ComponentGui.IsGameMenuDialogVisible()) {
                    flag = false;
                    break;
                }
            }
            if (flag) {
                GameTimeFactor = GameMenuDialogTimeFactor;
            }
            else if (GameTimeFactor == GameMenuDialogTimeFactor) {
                GameTimeFactor = BasicGameTimeFactor;
            }
        }

        public void QueueGameTimeDelayedExecution(double gameTime, Action action) {
            m_delayedExecutionsRequests.Add(new DelayedExecutionRequest { GameTime = gameTime, Action = action });
        }

        public bool PeriodicGameTimeEvent(double period, double offset) {
            double num = GameTime - offset;
            double num2 = Math.Floor(num / period) * period;
            if (num >= num2) {
                return num - GameTimeDelta < num2;
            }
            return false;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            m_subsystemUpdate = Project.FindSubsystem<SubsystemUpdate>(true);
        }
    }
}