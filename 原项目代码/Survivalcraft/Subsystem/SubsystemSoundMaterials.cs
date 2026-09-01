using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemSoundMaterials : Subsystem {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemAudio m_subsystemAudio;

        public Random m_random = new();

        public ValuesDictionary m_impactsSoundsValuesDictionary;

        public ValuesDictionary m_footstepSoundsValuesDictionary;

        public void PlayImpactSound(int value, Vector3 position, float loudnessMultiplier) {
            int num = Terrain.ExtractContents(value);
            string soundMaterialName = BlocksManager.Blocks[num].GetSoundMaterialName(m_subsystemTerrain, value);
            if (!string.IsNullOrEmpty(soundMaterialName)) {
                string value2 = m_impactsSoundsValuesDictionary.GetValue<string>(soundMaterialName, null);
                if (!string.IsNullOrEmpty(value2)) {
                    float pitch = m_random.Float(-0.2f, 0.2f);
                    m_subsystemAudio.PlayRandomSound(value2, 0.5f * loudnessMultiplier, pitch, position, 5f * loudnessMultiplier, true);
                }
            }
        }

        public bool PlayFootstepSound(ComponentCreature componentCreature, float loudnessMultiplier) {
            string footstepSoundMaterialName = GetFootstepSoundMaterialName(componentCreature);
            if (!string.IsNullOrEmpty(footstepSoundMaterialName)) {
                string value = componentCreature.ComponentCreatureSounds.ValuesDictionary.GetValue<ValuesDictionary>("CustomFootstepSounds")
                    .GetValue<string>(footstepSoundMaterialName, null);
                if (string.IsNullOrEmpty(value)) {
                    value = m_footstepSoundsValuesDictionary.GetValue<string>(footstepSoundMaterialName, null);
                }
                if (!string.IsNullOrEmpty(value)) {
                    float pitch = m_random.Float(-0.2f, 0.2f);
                    m_subsystemAudio.PlayRandomSound(
                        value,
                        0.75f * loudnessMultiplier,
                        pitch,
                        componentCreature.ComponentBody.Position,
                        2f * loudnessMultiplier,
                        true
                    );
                    if (componentCreature is ComponentPlayer componentPlayer
                        && componentPlayer.ComponentVitalStats.Wetness > 0f) {
                        string value2 = m_footstepSoundsValuesDictionary.GetValue<string>("Squishy", null);
                        if (!string.IsNullOrEmpty(value2)) {
                            float volume = 0.7f * loudnessMultiplier * MathF.Pow(componentPlayer.ComponentVitalStats.Wetness, 4f);
                            m_subsystemAudio.PlayRandomSound(
                                value2,
                                volume,
                                pitch,
                                componentCreature.ComponentBody.Position,
                                2f * loudnessMultiplier,
                                true
                            );
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public string GetFootstepSoundMaterialName(ComponentCreature componentCreature) {
            Vector3 position = componentCreature.ComponentBody.Position;
            if (componentCreature.ComponentBody.ImmersionDepth > 0.2f
                && componentCreature.ComponentBody.ImmersionFluidBlock is WaterBlock) {
                return "Water";
            }
            if (componentCreature.ComponentLocomotion.LadderValue.HasValue) {
                if (Terrain.ExtractContents(componentCreature.ComponentLocomotion.LadderValue.Value) == 59) {
                    return "WoodenLadder";
                }
                return "MetalLadder";
            }
            int cellValue = m_subsystemTerrain.Terrain.GetCellValue(
                Terrain.ToCell(position.X),
                Terrain.ToCell(position.Y + 0.1f),
                Terrain.ToCell(position.Z)
            );
            int num = Terrain.ExtractContents(cellValue);
            string soundMaterialName = BlocksManager.Blocks[num].GetSoundMaterialName(m_subsystemTerrain, cellValue);
            if (string.IsNullOrEmpty(soundMaterialName)
                && componentCreature.ComponentBody.StandingOnValue.HasValue) {
                soundMaterialName = BlocksManager.Blocks[Terrain.ExtractContents(componentCreature.ComponentBody.StandingOnValue.Value)]
                    .GetSoundMaterialName(m_subsystemTerrain, componentCreature.ComponentBody.StandingOnValue.Value);
            }
            if (!string.IsNullOrEmpty(soundMaterialName)) {
                return soundMaterialName;
            }
            return string.Empty;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_impactsSoundsValuesDictionary = valuesDictionary.GetValue<ValuesDictionary>("ImpactSounds");
            m_footstepSoundsValuesDictionary = valuesDictionary.GetValue<ValuesDictionary>("FootstepSounds");
        }
    }
}