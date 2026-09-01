using System.Globalization;
using Engine;
using Engine.Audio;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentVitalStats : Component, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTime m_subsystemTime;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemMetersBlockBehavior m_subsystemMetersBlockBehavior;

        public SubsystemWeather m_subsystemWeather;

        public ComponentPlayer m_componentPlayer;

        public Random m_random = new();

        public Sound m_pantingSound;

        public float m_food;

        public float m_stamina;

        public float m_sleep;

        public float m_temperature;

        public float m_wetness;

        public float m_lastFood;

        public float m_lastStamina;

        public float m_lastSleep;

        public float m_lastTemperature;

        public float m_lastWetness;

        public Dictionary<int, float> m_satiation = [];

        public List<KeyValuePair<int, float>> m_satiationList = [];

        public float m_densityModifierApplied;

        public double? m_lastAttackedTime;

        public float m_sleepBlackoutFactor;

        public float m_sleepBlackoutDuration;

        public float m_environmentTemperature;

        public float m_targetTemperature;

        public float m_targetTemperatureFlux;

        public float m_temperatureBlackoutFactor;

        public float m_temperatureBlackoutDuration;
        public float EnvironmentTemperature => m_environmentTemperature;

        public static string fName = "ComponentVitalStats";

        public Action<int> FoodEaten { get; set; }

        /// <summary>
        ///     1.8.1.2添加：
        ///     用于多模组控制同一项参数
        ///     例如：VitalStatsForMods["Water"]表示水份值
        /// </summary>
        public ValuesDictionary VitalStatsForMods = new();

        public float Food {
            get => m_food;
            set => m_food = MathUtils.Saturate(value);
        }

        public float Stamina {
            get => m_stamina;
            set => m_stamina = MathUtils.Saturate(value);
        }

        public float Sleep {
            get => m_sleep;
            set => m_sleep = MathUtils.Saturate(value);
        }

        public float Temperature {
            get => m_temperature;
            set => m_temperature = Math.Clamp(value, 0f, 24f);
        }

        public float Wetness {
            get => m_wetness;
            set => m_wetness = MathUtils.Saturate(value);
        }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual bool Eat(int value) {
            bool skipVanilla = false;
            bool eatSuccess = false;
            int modifiedValue = value;

            ModsManager.HookAction(
                "OnVitalStatsEat",
                loader => {
                    loader.OnVitalStatsEat(this, ref modifiedValue, ref skipVanilla, out eatSuccess);
                    return false;
                }
            );

            // 如果Mod标记跳过原有逻辑，直接返回Mod的结果
            if (skipVanilla) {
                return eatSuccess;
            }

            int num = Terrain.ExtractContents(modifiedValue);
            Block obj = BlocksManager.Blocks[num];
            float num2 = obj.GetNutritionalValue(modifiedValue);
            float sicknessProbability = obj.GetSicknessProbability(modifiedValue);
            if (num2 > 0f) {
                if (m_componentPlayer.ComponentSickness.IsSick
                    && sicknessProbability > 0f) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, true, true);
                    return false;
                }
                if (Food >= 0.98f) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 2), Color.White, true, true);
                    return false;
                }
                m_subsystemAudio.PlayRandomSound(
                    "Audio/Creatures/HumanEat",
                    1f,
                    m_random.Float(-0.2f, 0.2f),
                    m_componentPlayer.ComponentBody.Position,
                    2f,
                    0f
                );
                if (m_componentPlayer.ComponentSickness.IsSick) {
                    num2 *= 0.75f;
                }
                Food += num2;
                FoodEaten?.Invoke(modifiedValue);
                m_satiation.TryGetValue(num, out float value2);
                value2 += MathF.Max(num2, 0.5f);
                m_satiation[num] = value2;
                if (m_componentPlayer.ComponentSickness.IsSick) {
                    m_componentPlayer.ComponentSickness.NauseaEffect();
                }
                else if (sicknessProbability >= 0.5f) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 3), Color.White, true, true);
                }
                else if (sicknessProbability > 0f) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 4), Color.White, true, true);
                }
                else if (value2 > 2.5f) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 5), Color.White, true, true);
                }
                else if (value2 > 2f) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 6), Color.White, true, true);
                }
                else if (Food > 0.85f) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 7), Color.White, true, true);
                }
                else {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 8), Color.White, true, false);
                }
                if (m_random.Bool(sicknessProbability)
                    || value2 > 3.5f) {
                    m_componentPlayer.ComponentSickness.StartSickness();
                }
                m_componentPlayer.PlayerStats.FoodItemsEaten++;
                return true;
            }
            return false;
        }

        public virtual void MakeSleepy(float sleepValue) {
            Sleep = MathF.Min(Sleep, sleepValue);
        }

        public virtual void Update(float dt) {
            if (m_componentPlayer.ComponentHealth.Health > 0f) {
                UpdateFood();
                UpdateStamina();
                UpdateSleep();
                UpdateTemperature();
                UpdateWetness();
            }
            else {
                m_pantingSound.Stop();
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemMetersBlockBehavior = Project.FindSubsystem<SubsystemMetersBlockBehavior>(true);
            m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_pantingSound = m_subsystemAudio.CreateSound("Audio/HumanPanting");
            m_pantingSound.IsLooped = true;
            Food = valuesDictionary.GetValue<float>("Food");
            Stamina = valuesDictionary.GetValue<float>("Stamina");
            Sleep = valuesDictionary.GetValue<float>("Sleep");
            Temperature = valuesDictionary.GetValue<float>("Temperature");
            Wetness = valuesDictionary.GetValue<float>("Wetness");
            m_lastFood = Food;
            m_lastStamina = Stamina;
            m_lastSleep = Sleep;
            m_lastTemperature = Temperature;
            m_lastWetness = Wetness;
            m_targetTemperature = Temperature;
            m_environmentTemperature = 8f;
            m_componentPlayer.ComponentBody.Attacked += delegate { m_lastAttackedTime = m_subsystemTime.GameTime; };
            foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Satiation")) {
                m_satiation[int.Parse(item.Key, CultureInfo.InvariantCulture)] = (float)item.Value;
            }
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("Food", Food);
            valuesDictionary.SetValue("Stamina", Stamina);
            valuesDictionary.SetValue("Sleep", Sleep);
            valuesDictionary.SetValue("Temperature", Temperature);
            valuesDictionary.SetValue("Wetness", Wetness);
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Satiation", valuesDictionary2);
            foreach (KeyValuePair<int, float> item in m_satiation) {
                if (item.Value > 0f) {
                    valuesDictionary2.SetValue(item.Key.ToString(CultureInfo.InvariantCulture), item.Value);
                }
            }
        }

        public override void OnEntityRemoved() {
            m_pantingSound.Stop();
        }

        public virtual void UpdateFood() {
            bool skipVanilla = false;
            float modifiedFood = Food;
            float gameTimeDelta = m_subsystemTime.GameTimeDelta;

            ModsManager.HookAction(
                "OnVitalStatsUpdateFood",
                loader => {
                    loader.OnVitalStatsUpdateFood(this, ref modifiedFood, ref gameTimeDelta, ref skipVanilla);
                    return false;
                }
            );

            if (skipVanilla) return;

            Food = modifiedFood;

            float num = m_componentPlayer.ComponentLocomotion.LastWalkOrder?.Length() ?? 0f;
            float lastJumpOrder = m_componentPlayer.ComponentLocomotion.LastJumpOrder;
            float num2 = m_componentPlayer.ComponentCreatureModel.EyePosition.Y - m_componentPlayer.ComponentBody.Position.Y;
            bool flag = m_componentPlayer.ComponentBody.ImmersionDepth > num2;
            bool flag2 = m_componentPlayer.ComponentBody.ImmersionFactor > 0.33f && !m_componentPlayer.ComponentBody.StandingOnValue.HasValue;
            bool flag3 = m_subsystemTime.PeriodicGameTimeEvent(240.0, 13.0) && !m_componentPlayer.ComponentSickness.IsSick;
            if (m_subsystemGameInfo.WorldSettings.GameMode != 0
                && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled) {
                float hungerFactor = m_componentPlayer.ComponentLevel.HungerFactor;
                Food -= hungerFactor * gameTimeDelta / 2880f;
                if (flag2 | flag) {
                    Food -= hungerFactor * gameTimeDelta * num / 1440f;
                }
                else {
                    Food -= hungerFactor * gameTimeDelta * num / 2880f;
                }
                Food -= hungerFactor * lastJumpOrder / 1200f;
                if (m_componentPlayer.ComponentMiner.DigCellFace.HasValue) {
                    Food -= hungerFactor * gameTimeDelta / 2880f;
                }
                if (!m_componentPlayer.ComponentSleep.IsSleeping) {
                    if (Food <= 0f) {
                        if (m_subsystemTime.PeriodicGameTimeEvent(50.0, 0.0)) {
                            m_componentPlayer.ComponentHealth.Injure(0.05f, null, false, LanguageControl.Get(fName, 9));
                            m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 10), Color.White, true, false);
                            m_componentPlayer.ComponentGui.FoodBarWidget.Flash(10);
                        }
                    }
                    else if (Food < 0.1f
                        && (m_lastFood >= 0.1f) | flag3) {
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 11), Color.White, true, true);
                    }
                    else if (Food < 0.25f
                        && (m_lastFood >= 0.25f) | flag3) {
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 12), Color.White, true, true);
                    }
                    else if (Food < 0.5f
                        && (m_lastFood >= 0.5f) | flag3) {
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 13), Color.White, true, false);
                    }
                }
            }
            else {
                Food = 0.9f;
            }
            if (m_subsystemTime.PeriodicGameTimeEvent(1.0, -0.01)) {
                m_satiationList.Clear();
                m_satiationList.AddRange(m_satiation);
                m_satiation.Clear();
                foreach (KeyValuePair<int, float> satiation in m_satiationList) {
                    float num3 = MathF.Max(satiation.Value - 0.000416666677f, 0f);
                    if (num3 > 0f) {
                        m_satiation.Add(satiation.Key, num3);
                    }
                }
            }
            m_lastFood = Food;
            m_componentPlayer.ComponentGui.FoodBarWidget.Value = Food;
        }

        public virtual void UpdateStamina() {
            bool skipVanilla = false;
            float modifiedStamina = Stamina;
            float gameTimeDelta = m_subsystemTime.GameTimeDelta;

            ModsManager.HookAction(
                "OnVitalStatsUpdateStamina",
                loader => {
                    loader.OnVitalStatsUpdateStamina(this, ref modifiedStamina, ref gameTimeDelta, ref skipVanilla);
                    return false;
                }
            );

            if (skipVanilla) return;

            Stamina = modifiedStamina;

            float lastWalkOrder = m_componentPlayer.ComponentLocomotion.LastWalkOrder?.Length() ?? 0f;
            float lastJumpOrder = m_componentPlayer.ComponentLocomotion.LastJumpOrder;
            float playerHeight = m_componentPlayer.ComponentCreatureModel.EyePosition.Y - m_componentPlayer.ComponentBody.Position.Y;
            bool IsImmersedCompletely = m_componentPlayer.ComponentBody.ImmersionDepth > playerHeight;
            bool IsImmersed = m_componentPlayer.ComponentBody.ImmersionFactor > 0.33f && !m_componentPlayer.ComponentBody.StandingOnValue.HasValue;
            if (m_subsystemGameInfo.WorldSettings.GameMode >= GameMode.Survival
                && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled) {
                float num3 = 1f / MathF.Max(m_componentPlayer.ComponentLevel.SpeedFactor, 0.75f);
                if (m_componentPlayer.ComponentSickness.IsSick
                    || m_componentPlayer.ComponentFlu.HasFlu) {
                    num3 *= 5f;
                }
                Stamina += gameTimeDelta * 0.07f;
                Stamina -= 0.025f * lastJumpOrder * num3;
                if (IsImmersed | IsImmersedCompletely) {
                    Stamina -= gameTimeDelta * (0.07f + 0.006f * num3 + 0.008f * lastWalkOrder);
                }
                else {
                    Stamina -= gameTimeDelta * (0.07f + 0.006f * num3) * lastWalkOrder;
                }
                if (!IsImmersed
                    && !IsImmersedCompletely
                    && Stamina < 0.33f
                    && m_lastStamina >= 0.33f) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 14), Color.White, true, false);
                }
                if (IsImmersed | IsImmersedCompletely
                    && Stamina < 0.4f
                    && m_lastStamina >= 0.4f) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 15), Color.White, true, true);
                }
                if (Stamina < 0.1f) {
                    if (IsImmersed | IsImmersedCompletely) {
                        if (m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0)) {
                            m_componentPlayer.ComponentHealth.Injure(0.05f, null, false, LanguageControl.Get(fName, 16));
                            m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 17), Color.White, true, false);
                        }
                        if (m_random.Float(0f, 1f) < 1f * gameTimeDelta) {
                            m_componentPlayer.ComponentLocomotion.JumpOrder = 1f;
                        }
                    }
                    else if (m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0)) {
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 18), Color.White, true, true);
                    }
                }
                m_lastStamina = Stamina;
                float num4 = MathUtils.Saturate(2f * (0.5f - Stamina));
                if (!IsImmersedCompletely
                    && num4 > 0f) {
                    float num5 = m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female ? 0.2f : 0f;
                    m_pantingSound.Volume = 1f
                        * SettingsManager.SoundsVolume
                        * MathUtils.Saturate(1f * num4)
                        * MathUtils.Lerp(0.8f, 1f, SimplexNoise.Noise((float)MathUtils.Remainder(3.0 * Time.RealTime + 100.0, 1000.0)));
                    m_pantingSound.Pitch = AudioManager.ToEnginePitch(
                        num5
                        + MathUtils.Lerp(-0.15f, 0.05f, num4)
                        * MathUtils.Lerp(0.8f, 1.2f, SimplexNoise.Noise((float)MathUtils.Remainder(3.0 * Time.RealTime + 200.0, 1000.0)))
                    );
                    m_pantingSound.Play();
                }
                else {
                    m_pantingSound.Stop();
                }
                float num6 = MathUtils.Saturate(3f * (0.33f - Stamina));
                if (num6 > 0f
                    && SimplexNoise.Noise((float)MathUtils.Remainder(Time.RealTime, 1000.0)) < num6) {
                    ApplyDensityModifier(0.6f);
                }
                else {
                    ApplyDensityModifier(0f);
                }
            }
            else {
                Stamina = 1f;
                ApplyDensityModifier(0f);
            }
        }

        public virtual void UpdateSleep() {
            bool skipVanilla = false;
            float modifiedSleep = Sleep;
            float gameTimeDelta = m_subsystemTime.GameTimeDelta;

            ModsManager.HookAction(
                "OnVitalStatsUpdateSleep",
                loader => {
                    loader.OnVitalStatsUpdateSleep(this, ref modifiedSleep, ref gameTimeDelta, ref skipVanilla);
                    return false;
                }
            );

            if (skipVanilla) return;

            Sleep = modifiedSleep;

            bool flag = m_componentPlayer.ComponentBody.ImmersionFactor > 0.05f;
            bool flag2 = m_subsystemTime.PeriodicGameTimeEvent(240.0, 9.0);
            if (m_subsystemGameInfo.WorldSettings.GameMode != 0
                && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled) {
                if (m_componentPlayer.ComponentSleep.SleepFactor == 1f) {
                    Sleep += 0.05f * gameTimeDelta;
                }
                else if (!flag
                    && (!m_lastAttackedTime.HasValue || m_subsystemTime.GameTime - m_lastAttackedTime > 10.0)) {
                    Sleep -= gameTimeDelta / 1800f;
                    if (Sleep < 0.075f
                        && (m_lastSleep >= 0.075f) | flag2) {
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 19), Color.White, true, true);
                        m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
                    }
                    else if (Sleep < 0.2f
                        && (m_lastSleep >= 0.2f) | flag2) {
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 20), Color.White, true, true);
                        m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
                    }
                    else if (Sleep < 0.33f
                        && (m_lastSleep >= 0.33f) | flag2) {
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 21), Color.White, true, false);
                    }
                    else if (Sleep < 0.5f
                        && (m_lastSleep >= 0.5f) | flag2) {
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 22), Color.White, true, false);
                    }
                    if (Sleep < 0.075f) {
                        float num = MathUtils.Lerp(0.05f, 0.2f, (0.075f - Sleep) / 0.075f);
                        float x = Sleep < 0.0375f ? m_random.Float(3f, 6f) : m_random.Float(2f, 4f);
                        if (m_random.Float(0f, 1f) < num * gameTimeDelta) {
                            m_sleepBlackoutDuration = MathF.Max(m_sleepBlackoutDuration, x);
                            m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
                        }
                    }
                    if (Sleep <= 0f
                        && !m_componentPlayer.ComponentSleep.IsSleeping) {
                        m_componentPlayer.ComponentSleep.Sleep(false);
                        m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 23), Color.White, true, true);
                        m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
                    }
                }
            }
            else {
                Sleep = 0.9f;
            }
            m_lastSleep = Sleep;
            m_sleepBlackoutDuration -= gameTimeDelta;
            float num2 = MathUtils.Saturate(0.5f * m_sleepBlackoutDuration);
            m_sleepBlackoutFactor = MathUtils.Saturate(m_sleepBlackoutFactor + 2f * gameTimeDelta * (num2 - m_sleepBlackoutFactor));
            if (!m_componentPlayer.ComponentSleep.IsSleeping) {
                m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathF.Max(
                    m_sleepBlackoutFactor,
                    m_componentPlayer.ComponentScreenOverlays.BlackoutFactor
                );
                if (m_sleepBlackoutFactor > 0.01) {
                    m_componentPlayer.ComponentScreenOverlays.FloatingMessage = LanguageControl.Get(fName, 24);
                    m_componentPlayer.ComponentScreenOverlays.FloatingMessageFactor = MathUtils.Saturate(10f * (m_sleepBlackoutFactor - 0.9f));
                }
            }
        }

        public virtual void UpdateTemperature() {
            bool skipVanilla = false;
            float modifiedTemperature = Temperature;
            float gameTimeDelta = m_subsystemTime.GameTimeDelta;

            ModsManager.HookAction(
                "OnVitalStatsUpdateTemperature",
                loader => {
                    loader.OnVitalStatsUpdateTemperature(this, ref modifiedTemperature, ref gameTimeDelta, ref skipVanilla);
                    return false;
                }
            );

            if (skipVanilla) return;

            Temperature = modifiedTemperature;

            bool flag = m_subsystemTime.PeriodicGameTimeEvent(300.0, 17.0);
            float num = m_componentPlayer.ComponentClothing.Insulation * MathUtils.Lerp(1f, 0.05f, MathUtils.Saturate(4f * Wetness));
            if (m_subsystemGameInfo.WorldSettings.GameMode <= GameMode.Survival) {
                num = num * 1.5f + 1f;
            }
            string arg;
            ClothingSlot leastInsulatedSlot = m_componentPlayer.ComponentClothing.LeastInsulatedSlot;
            arg = leastInsulatedSlot.MessageWhenLeastInsulated;
            if (m_subsystemTime.PeriodicGameTimeEvent(1.0, 1.0 * (GetHashCode() % 1000 / 1000.0))) {
                int x = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.X);
                int y = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.Y + 0.1f);
                int z = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.Z);
                m_subsystemMetersBlockBehavior.CalculateTemperature(
                    x,
                    y,
                    z,
                    12f,
                    num,
                    out m_targetTemperature,
                    out m_targetTemperatureFlux,
                    out m_environmentTemperature
                );
            }
            if (m_subsystemGameInfo.WorldSettings.GameMode != 0
                && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled) {
                float num2 = m_targetTemperature - Temperature;
                Temperature += MathUtils.Saturate(m_targetTemperatureFlux * gameTimeDelta) * num2;
            }
            else {
                Temperature = 12f;
            }
            if (Temperature <= 0f) {
                m_componentPlayer.ComponentHealth.Injure(1f, null, false, LanguageControl.Get(fName, 25));
            }
            else if (Temperature < 3f) {
                if (m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0)) {
                    m_componentPlayer.ComponentHealth.Injure(0.05f, null, false, LanguageControl.Get(fName, 26));
                    string text = Wetness > 0f ? string.Format(LanguageControl.Get(fName, 27), arg) :
                        !(num < 1f) ? string.Format(LanguageControl.Get(fName, 28), arg) : string.Format(LanguageControl.Get(fName, 29), arg);
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(text, Color.White, true, false);
                    m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
                }
            }
            else if (Temperature < 6f
                && (m_lastTemperature >= 6f) | flag) {
                string text2 = Wetness > 0f ? string.Format(LanguageControl.Get(fName, 30), arg) :
                    !(num < 1f) ? string.Format(LanguageControl.Get(fName, 31), arg) : string.Format(LanguageControl.Get(fName, 32), arg);
                m_componentPlayer.ComponentGui.DisplaySmallMessage(text2, Color.White, true, true);
                m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
            }
            else if (Temperature < 8f
                && (m_lastTemperature >= 8f) | flag) {
                m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 33), Color.White, false, false);
                m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
            }
            if (Temperature >= 24f) {
                if (m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0)) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 34), Color.White, true, false);
                    m_componentPlayer.ComponentHealth.Injure(0.05f, null, false, LanguageControl.Get(fName, 35));
                    m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
                }
                if (m_subsystemTime.PeriodicGameTimeEvent(8.0, 0.0)) {
                    m_temperatureBlackoutDuration = MathF.Max(m_temperatureBlackoutDuration, 6f);
                    m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
                }
            }
            else if (Temperature > 20f
                && m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0)) {
                m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 36), Color.White, true, false);
                m_temperatureBlackoutDuration = MathF.Max(m_temperatureBlackoutDuration, 3f);
                m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
                m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
            }
            m_lastTemperature = Temperature;
            m_componentPlayer.ComponentScreenOverlays.IceFactor = MathUtils.Saturate(1f - Temperature / 6f);
            m_temperatureBlackoutDuration -= gameTimeDelta;
            float num3 = MathUtils.Saturate(0.5f * m_temperatureBlackoutDuration);
            m_temperatureBlackoutFactor = MathUtils.Saturate(m_temperatureBlackoutFactor + 2f * gameTimeDelta * (num3 - m_temperatureBlackoutFactor));
            m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathF.Max(
                m_temperatureBlackoutFactor,
                m_componentPlayer.ComponentScreenOverlays.BlackoutFactor
            );
            if (m_temperatureBlackoutFactor > 0.01) {
                m_componentPlayer.ComponentScreenOverlays.FloatingMessage = LanguageControl.Get(fName, 37);
                m_componentPlayer.ComponentScreenOverlays.FloatingMessageFactor = MathUtils.Saturate(10f * (m_temperatureBlackoutFactor - 0.9f));
            }
            if (m_targetTemperature > 22f) {
                m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature6");
            }
            else if (m_targetTemperature > 18f) {
                m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature5");
            }
            else if (m_targetTemperature > 14f) {
                m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature4");
            }
            else if (m_targetTemperature > 10f) {
                m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature3");
            }
            else if (m_targetTemperature > 6f) {
                m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature2");
            }
            else {
                m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = m_targetTemperature > 2f
                    ? ContentManager.Get<Subtexture>("Textures/Atlas/Temperature1")
                    : ContentManager.Get<Subtexture>("Textures/Atlas/Temperature0");
            }
        }

        public virtual void UpdateWetness() {
            bool skipVanilla = false;
            float modifiedWetness = Wetness;
            float gameTimeDelta = m_subsystemTime.GameTimeDelta;

            ModsManager.HookAction(
                "OnVitalStatsUpdateWetness",
                loader => {
                    loader.OnVitalStatsUpdateWetness(this, ref modifiedWetness, ref gameTimeDelta, ref skipVanilla);
                    return false;
                }
            );

            if (skipVanilla) return;

            Wetness = modifiedWetness;

            Wetness += gameTimeDelta * m_componentPlayer.ComponentLevel.GetOtherFactorResult("Wetness");
            if (m_subsystemGameInfo.WorldSettings.GameMode != 0
                && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled) {
                if (Wetness > 0.8f
                    && m_lastWetness <= 0.8f) {
                    Time.QueueTimeDelayedExecution(
                        Time.FrameStartTime + 2.0,
                        delegate {
                            if (Wetness > 0.8f) {
                                m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 38), Color.White, true, true);
                            }
                        }
                    );
                }
                else if (Wetness > 0.2f
                    && m_lastWetness <= 0.2f) {
                    Time.QueueTimeDelayedExecution(
                        Time.FrameStartTime + 2.0,
                        delegate {
                            if (Wetness > 0.2f
                                && Wetness <= 0.8f
                                && Wetness > m_lastWetness) {
                                m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 39), Color.White, true, true);
                            }
                        }
                    );
                }
                else if (Wetness <= 0f
                    && m_lastWetness > 0f) {
                    Time.QueueTimeDelayedExecution(
                        Time.FrameStartTime + 2.0,
                        delegate {
                            if (Wetness <= 0f) {
                                m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 40), Color.White, true, true);
                            }
                        }
                    );
                }
            }
            m_lastWetness = Wetness;
        }

        public virtual void ApplyDensityModifier(float modifier) {
            float num = modifier - m_densityModifierApplied;
            if (num != 0f) {
                m_densityModifierApplied = modifier;
                m_componentPlayer.ComponentBody.Density += num;
            }
        }
    }
}