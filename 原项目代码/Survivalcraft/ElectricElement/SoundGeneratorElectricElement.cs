using Engine;

namespace Game {
    public class SoundGeneratorElectricElement : RotateableElectricElement {
        public SubsystemNoise m_subsystemNoise;

        public SubsystemParticles m_subsystemParticles;

        public SoundParticleSystem m_particleSystem;

        public Random m_random = new();

        public int m_lastToneInput;

        public double m_playAllowedTime;

        public string[] m_tones = [
            "",
            "Bell",
            "Organ",
            "Ping",
            "String",
            "Trumpet",
            "Voice",
            "Piano",
            "PianoLong",
            "Drums",
            "Bass",
            "",
            "",
            "",
            "",
            "Piano"
        ];

        public int[] m_maxOctaves = [
            0,
            6,
            5,
            6,
            6,
            6,
            6,
            6,
            6,
            0,
            6,
            0,
            0,
            0,
            0,
            6
        ];

        public string[] m_drums = [
            "Snare",
            "BassDrum",
            "ClosedHiHat",
            "PedalHiHat",
            "OpenHiHat",
            "LowTom",
            "HighTom",
            "CrashCymbal",
            "RideCymbal",
            "HandClap"
        ];

        public SoundGeneratorElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) {
            m_subsystemNoise = subsystemElectricity.Project.FindSubsystem<SubsystemNoise>(true);
            m_subsystemParticles = subsystemElectricity.Project.FindSubsystem<SubsystemParticles>(true);
            Vector3 vector = CellFace.FaceToVector3(cellFace.Face);
            Vector3 position = new Vector3(cellFace.Point) + new Vector3(0.5f) - 0.2f * vector;
            m_particleSystem = new SoundParticleSystem(subsystemElectricity.SubsystemTerrain, position, vector);
        }

        public override bool Simulate() {
            int num = 0;
            int num2 = 15;
            int num3 = 2;
            int num4 = 0;
            int rotation = Rotation;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(
                        CellFaces[0].Face,
                        rotation,
                        connection.ConnectorFace
                    );
                    if (connectorDirection.HasValue) {
                        if (connectorDirection.Value == ElectricConnectorDirection.In
                            || connectorDirection.Value == ElectricConnectorDirection.Bottom) {
                            num4 = (int)MathF.Round(15f * connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                        }
                        else if (connectorDirection.Value == ElectricConnectorDirection.Left) {
                            num = (int)MathF.Round(15f * connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                        }
                        else if (connectorDirection.Value == ElectricConnectorDirection.Right) {
                            num3 = (int)MathF.Round(15f * connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                        }
                        else if (connectorDirection.Value == ElectricConnectorDirection.Top) {
                            num2 = (int)MathF.Round(15f * connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                        }
                    }
                }
            }
            if (m_lastToneInput == 0
                && num4 != 0
                && num != 15
                && SubsystemElectricity.SubsystemTime.GameTime >= m_playAllowedTime) {
                m_playAllowedTime = SubsystemElectricity.SubsystemTime.GameTime + 0.079999998211860657;
                string text = m_tones[num4];
                float num5 = 0f;
                string text2 = null;
                if (text == "Drums") {
                    num5 = 1f;
                    if (num >= 0
                        && num < m_drums.Length) {
                        text2 = $"Audio/SoundGenerator/Drums{m_drums[num]}";
                    }
                }
                else if (!string.IsNullOrEmpty(text)) {
                    float num6 = 130.8125f * MathF.Pow(1.05946314f, num + 12 * num3);
                    int num7 = 0;
                    for (int i = 4; i <= m_maxOctaves[num4]; i++) {
                        float num8 = num6 / (523.25f * MathF.Pow(2f, i - 5));
                        if (num7 == 0
                            || (num8 >= 0.5f && num8 < num5)) {
                            num7 = i;
                            num5 = num8;
                        }
                    }
                    text2 = $"Audio/SoundGenerator/{text}C{num7}";
                }
                if (num5 != 0f
                    && !string.IsNullOrEmpty(text2)) {
                    CellFace cellFace = CellFaces[0];
                    Vector3 position = new(cellFace.X, cellFace.Y, cellFace.Z);
                    float volume = num2 / 15f;
                    float pitch = Math.Clamp(MathF.Log(num5) / MathF.Log(2f), -1f, 1f);
                    float minDistance = 0.5f + 5f * num2 / 15f;
                    SubsystemElectricity.SubsystemAudio.PlaySound(text2, volume, pitch, position, minDistance, true);
                    float loudness = num2 < 8 ? 0.25f : 0.5f;
                    float range = MathUtils.Lerp(2f, 20f, num2 / 15f);
                    m_subsystemNoise.MakeNoise(position, loudness, range);
                    if (m_particleSystem.SubsystemParticles == null) {
                        m_subsystemParticles.AddParticleSystem(m_particleSystem);
                    }
                    Vector3 hsv = new(22.5f * num + m_random.Float(0f, 22f), 0.5f + num2 / 30f, 1f);
                    m_particleSystem.AddNote(new Color(Color.HsvToRgb(hsv)));
                }
            }
            m_lastToneInput = num4;
            return false;
        }
    }
}