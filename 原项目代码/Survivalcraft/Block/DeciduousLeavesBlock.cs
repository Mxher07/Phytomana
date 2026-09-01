using Engine;

namespace Game {
    public abstract class DeciduousLeavesBlock : LeavesBlock {
        public Random m_random1 = new();

        public readonly float SummerStart;

        public readonly float AutumnStart;

        public readonly float WinterStart;

        public readonly float SpringStart;

        public readonly float SummerInterval;

        public readonly float AutumnInterval;

        public readonly float WinterInterval;

        public readonly float SpringInterval;

        public readonly float SummerIntervalInv;

        public readonly float AutumnIntervalInv;

        public readonly float WinterIntervalInv;

        public readonly float SpringIntervalInv;

        public readonly BlockColorsMap BlockColorsMap;

        public readonly Color AutumnColor1;

        public readonly Color AutumnColor2;

        public readonly float AutumnTransitionLightening;

        public readonly float AutumnSpeedupFactor;

        public readonly Color SpringColor;

        public const string fName = "DeciduousLeavesBlock";

        public DeciduousLeavesBlock(float summerStart,
            float autumnStart,
            float winterStart,
            float springStart,
            BlockColorsMap blockColorsMap,
            Color autumnColor1,
            Color autumnColor2,
            float autumnTransitionLightening) {
            SummerStart = summerStart;
            AutumnStart = autumnStart;
            WinterStart = winterStart;
            SpringStart = springStart;
            SummerInterval = IntervalUtils.Interval(summerStart, autumnStart);
            AutumnInterval = IntervalUtils.Interval(autumnStart, winterStart);
            WinterInterval = IntervalUtils.Interval(winterStart, springStart);
            SpringInterval = IntervalUtils.Interval(springStart, summerStart);
            SummerIntervalInv = 1f / SummerInterval;
            AutumnIntervalInv = 1f / AutumnInterval;
            WinterIntervalInv = 1f / WinterInterval;
            SpringIntervalInv = 1f / SpringInterval;
            BlockColorsMap = blockColorsMap;
            AutumnColor1 = autumnColor1;
            AutumnColor2 = autumnColor2;
            AutumnTransitionLightening = autumnTransitionLightening;
            AutumnSpeedupFactor = 1.33f;
            SpringColor = new Color(160, 255, 90);
        }

        public override Color GetLeavesBlockColor(int value, Terrain terrain, int x, int y, int z) {
            int data = Terrain.ExtractData(value);
            switch (GetSeason(data)) {
                case Season.Spring: {
                    Color springColor = SpringColor;
                    Color c3 = BlockColorsMap.Lookup(terrain, x, y, z);
                    float timeOfSeason = GetTimeOfSeason(data);
                    return Color.LerpNotSaturated(springColor, c3, timeOfSeason);
                }
                case Season.Autumn: {
                    Color c = BlockColorsMap.Lookup(terrain, x, y, z);
                    Color c2 = Color.LerpNotSaturated(
                        f: MathUtils.Hash((uint)(x + 59 * y + 2497 * z)) / 4.2949673E+09f,
                        c1: AutumnColor1,
                        c2: AutumnColor2
                    );
                    float f2 = MathUtils.Min(GetTimeOfSeason(data) * AutumnSpeedupFactor, 1f);
                    return Color.MultiplyColorOnly(s: MathUtils.Lerp(1f, AutumnTransitionLightening, Hat(f2)), c: Color.LerpNotSaturated(c, c2, f2));
                }
                case Season.Winter: return Color.White;
                default: return BlockColorsMap.Lookup(terrain, x, y, z);
            }
        }

        public override Color GetLeavesItemColor(int value, DrawBlockEnvironmentData environmentData) {
            int data = Terrain.ExtractData(value);
            switch (GetSeason(data)) {
                case Season.Spring: {
                    Color springColor = SpringColor;
                    Color c3 = BlockColorsMap.Lookup(environmentData);
                    float timeOfSeason = GetTimeOfSeason(data);
                    return Color.LerpNotSaturated(springColor, c3, timeOfSeason);
                }
                case Season.Autumn: {
                    Color c = BlockColorsMap.Lookup(environmentData);
                    Color c2 = Color.Lerp(AutumnColor1, AutumnColor2, 0.5f);
                    float f = MathUtils.Min(GetTimeOfSeason(data) * AutumnSpeedupFactor, 1f);
                    return Color.MultiplyColorOnly(s: MathUtils.Lerp(1f, AutumnTransitionLightening, Hat(f)), c: Color.LerpNotSaturated(c, c2, f));
                }
                case Season.Winter: return Color.White;
                default: return BlockColorsMap.Lookup(environmentData);
            }
        }

        public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain,
            int face,
            int value,
            int neighborValue,
            int x,
            int y,
            int z) {
            if (!base.ShouldGenerateFace(
                    subsystemTerrain,
                    face,
                    value,
                    neighborValue,
                    x,
                    y,
                    z
                )) {
                return false;
            }
            if (Terrain.ExtractContents(value) != Terrain.ExtractContents(neighborValue)) {
                return true;
            }
            int data = Terrain.ExtractData(value);
            int data2 = Terrain.ExtractData(neighborValue);
            bool num = GetSeason(data) == Season.Winter;
            bool flag = GetSeason(data2) == Season.Winter;
            if (num != flag) {
                return true;
            }
            return false;
        }

        public override BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            int toolValue,
            TerrainRaycastResult raycastResult) {
            int data = Terrain.ExtractData(value);
            if (GetSeason(data) == Season.Autumn
                && GetTimeOfSeason(data) > 0.5f) {
                subsystemTerrain.Project.FindSubsystem<SubsystemParticles>(true)
                    .AddParticleSystem(new LeavesParticleSystem(subsystemTerrain, raycastResult.CellFace.Point, 8, false, true, value));
                BlockPlacementData result = default;
                result.Value = Terrain.ReplaceData(value, SetSeason(SetIsShaken(data, true), Season.Winter));
                result.CellFace = raycastResult.CellFace;
                return result;
            }
            return base.GetDigValue(subsystemTerrain, componentMiner, value, toolValue, raycastResult);
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            if (Terrain.ExtractContents(newValue) == Terrain.ExtractContents(oldValue)) {
                showDebris = false;
            }
            else if (m_random1.Bool(0.25f)) {
                dropValues.Add(new BlockDropValue { Value = 23, Count = 1 });
                showDebris = true;
            }
            else {
                int data = Terrain.ExtractData(oldValue);
                int data2 = SetTimeOfSeason(data, GetSeason(data) == Season.Autumn ? 0.999f : 0f);
                int value = Terrain.MakeBlockValue(DefaultDropContent, 0, data2);
                dropValues.Add(new BlockDropValue { Value = value, Count = 1 });
                showDebris = true;
            }
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) =>
            string.Format(LanguageControl.Get(fName, (int)GetSeason(Terrain.ExtractData(value))), base.GetDisplayName(subsystemTerrain, value));

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetSeason(SetTimeOfSeason(0, 0f), Season.Spring));
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetSeason(SetTimeOfSeason(0, 0f), Season.Summer));
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetSeason(SetTimeOfSeason(0, 0.999f), Season.Autumn));
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetSeason(SetTimeOfSeason(0, 0f), Season.Winter));
        }

        public override int GetShadowStrength(int value) {
            return GetSeason(Terrain.ExtractData(value)) switch {
                Season.Winter => base.GetShadowStrength(value) / 3,
                Season.Spring => base.GetShadowStrength(value) / 2,
                _ => base.GetShadowStrength(value)
            };
        }

        public virtual float GetLeafDropProbability(int value) {
            int data = Terrain.ExtractData(value);
            return GetSeason(data) switch {
                Season.Summer => 0.015f,
                Season.Autumn => MathUtils.Lerp(0.04f, 0.16f, GetTimeOfSeason(data)),
                _ => 0f
            };
        }

        public virtual int SetTimeOfYear(int data, float timeOfYear) {
            float num = IntervalUtils.Interval(SummerStart, timeOfYear);
            int num2;
            if (num < SummerInterval) {
                num2 = SetSeason(SetTimeOfSeason(data, num * SummerIntervalInv), Season.Summer);
            }
            else {
                float num3 = IntervalUtils.Interval(AutumnStart, timeOfYear);
                if (num3 < AutumnInterval) {
                    num2 = SetSeason(SetTimeOfSeason(data, num3 * AutumnIntervalInv), Season.Autumn);
                }
                else {
                    float num4 = IntervalUtils.Interval(WinterStart, timeOfYear);
                    if (num4 < WinterInterval) {
                        num2 = SetSeason(SetTimeOfSeason(data, num4 * WinterIntervalInv), Season.Winter);
                    }
                    else {
                        float num5 = IntervalUtils.Interval(SpringStart, timeOfYear);
                        num2 = SetSeason(SetTimeOfSeason(data, num5 * SpringIntervalInv), Season.Spring);
                    }
                }
            }
            if (GetIsShaken(data)) {
                if (GetSeason(num2) == Season.Autumn) {
                    return data;
                }
                if (GetSeason(num2) != Season.Winter) {
                    num2 = SetIsShaken(num2, false);
                }
            }
            return num2;
        }

        public static Season GetSeason(int data) => (Season)((data >> 3) & 3);

        public static int SetSeason(int data, Season season) => (data & -25) | (((int)season & 3) << 3);

        public static float GetTimeOfSeason(int data) => (data & 7) / 7f;

        public static int SetTimeOfSeason(int data, float timeOfSeason) {
            int num = (int)(Math.Clamp(timeOfSeason, 0f, 0.999f) * 8f);
            return (data & -8) | (num & 7);
        }

        public static bool GetIsShaken(int data) => (data & 0x20) != 0;

        public static int SetIsShaken(int data, bool isManuallyCleared) => (data & -33) | (isManuallyCleared ? 32 : 0);

        public static float Hat(float f) => 1f - 2f * MathF.Abs(f - 0.5f);
    }
}