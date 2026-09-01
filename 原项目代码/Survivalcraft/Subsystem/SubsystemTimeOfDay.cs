using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemTimeOfDay : Subsystem {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemSeasons m_subsystemSeasons;

        public bool TimeOfDayEnabled = true;

        public float DayDuration { get; set; } = 1200f;

        public float DawnStart { get; set; }

        public float DayStart { get; set; }

        public float DuskStart { get; set; }

        public float NightStart { get; set; }

        public float DayInterval => IntervalUtils.Interval(DayStart, DuskStart);

        public float DuskInterval => IntervalUtils.Interval(DuskStart, NightStart);

        public float NightInterval => IntervalUtils.Interval(NightStart, DawnStart);

        public float DawnInterval => IntervalUtils.Interval(DawnStart, DayStart);

        public float Midday => IntervalUtils.Midpoint(DayStart, DuskStart);

        public float Middusk => IntervalUtils.Midpoint(DuskStart, NightStart);

        public float Midnight => IntervalUtils.Midpoint(NightStart, DawnStart);

        public float Middawn => IntervalUtils.Midpoint(DawnStart, DayStart);

        public float TimeOfDay {
            get {
                if (TimeOfDayEnabled) {
                    if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Changing) {
                        return CalculateTimeOfDay(m_subsystemGameInfo.TotalElapsedGameTime);
                    }
                    if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Day) {
                        return Midday;
                    }
                    if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Night) {
                        return Midnight;
                    }
                    if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Sunrise) {
                        return Middawn;
                    }
                    if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Sunset) {
                        return Middusk;
                    }
                }
                return Midday;
            }
        }

        public double Day => CalculateDay(m_subsystemGameInfo.TotalElapsedGameTime);

        public double TimeOfDayOffset { get; set; }

        public double CalculateDay(double totalElapsedGameTime) => (totalElapsedGameTime + (TimeOfDayOffset + DayStart) * DayDuration) / DayDuration;

        public float CalculateTimeOfDay(double totalElapsedGameTime) {
            double num = CalculateDay(totalElapsedGameTime);
            return (float)(num - Math.Floor(num));
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemSeasons = Project.FindSubsystem<SubsystemSeasons>(true);
            TimeOfDayOffset = valuesDictionary.GetValue<double>("TimeOfDayOffset");
            UpdateStarts();
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            valuesDictionary.SetValue("TimeOfDayOffset", TimeOfDayOffset);
        }

        public virtual void UpdateStarts() {
            float num = IntervalUtils.Midpoint(SubsystemSeasons.SummerStart, SubsystemSeasons.AutumnStart);
            float num2 = MathUtils.Remainder(m_subsystemGameInfo.WorldSettings.TimeOfYear - num, 1f);
            float num3 = MathUtils.Lerp(0.2f, 0.4f, 0.5f + 0.5f * MathF.Cos((float)Math.PI * 2f * num2));
            float num4 = 0.4f;
            float num5 = (1f - (num3 + num4)) / 2f;
            DayStart = 0.3f;
            DawnStart = IntervalUtils.Add(DayStart, 0f - num5);
            DuskStart = IntervalUtils.Add(DayStart, num3);
            NightStart = IntervalUtils.Add(DuskStart, num5);
        }
    }
}