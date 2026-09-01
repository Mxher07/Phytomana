using Engine;

namespace Game {
    public class SubsystemWaterBlockBehavior : SubsystemFluidBlockBehavior, IUpdateable {
        public Random m_random = new();

        public float m_soundVolume;

        public override int[] HandledBlocks => [18];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public SubsystemWaterBlockBehavior() : base(BlocksManager.FluidBlocks[18], true) { }

        public virtual void Update(float dt) {
            if (SubsystemTime.PeriodicGameTimeEvent(0.25, 0.0)) {
                SpreadFluid();
            }
            if (SubsystemTime.PeriodicGameTimeEvent(1.0, 0.25)) {
                float num = float.MaxValue;
                foreach (Vector3 listenerPosition in SubsystemAudio.ListenerPositions) {
                    float? num2 = CalculateDistanceToFluid(listenerPosition, 8, true);
                    if (num2.HasValue
                        && num2.Value < num) {
                        num = num2.Value;
                    }
                }
                m_soundVolume = 0.5f * SubsystemAudio.CalculateVolume(num, 2f, 3.5f);
            }
            SubsystemAmbientSounds.WaterSoundVolume = MathUtils.Max(SubsystemAmbientSounds.WaterSoundVolume, m_soundVolume);
        }

        public override bool OnFluidInteract(int interactValue, int x, int y, int z, int fluidValue) {
            if (BlocksManager.Blocks[Terrain.ExtractContents(interactValue)] is MagmaBlock) {
                SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.1f, 0.1f), new Vector3(x, y, z), 5f, true);
                SubsystemTerrain.DestroyCell(
                    0,
                    x,
                    y,
                    z,
                    0,
                    false,
                    false
                );
                Set(x, y, z, 3);
                return true;
            }
            return base.OnFluidInteract(interactValue, x, y, z, fluidValue);
        }

        public override void OnItemHarvested(int x, int y, int z, int blockValue, ref BlockDropValue dropValue, ref int newBlockValue) {
            if (y > 80
                && SubsystemWeather.IsPlaceFrozen(SubsystemTerrain.Terrain.GetSeasonalTemperature(x, z), y)) {
                dropValue.Value = Terrain.MakeBlockValue(62);
            }
            else {
                base.OnItemHarvested(x, y, z, blockValue, ref dropValue, ref newBlockValue);
            }
        }
    }
}