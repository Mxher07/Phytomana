using Engine;

namespace Game {
    public class SevenSegmentDisplayElectricElement : MountedElectricElement {
        public SubsystemGlow m_subsystemGlow;

        public float m_voltage = 1f / 0f;

        public GlowPoint[] m_glowPoints = new GlowPoint[7];

        public Color m_color;

        public Vector2[] m_centers = [
            new(0f, 6f),
            new(-4f, 3f),
            new(-4f, -3f),
            new(0f, -6f),
            new(4f, -3f),
            new(4f, 3f),
            new(0f, 0f)
        ];

        public Vector2[] m_sizes = [
            new(3.2f, 1f),
            new(1f, 2.3f),
            new(1f, 2.3f),
            new(3.2f, 1f),
            new(1f, 2.3f),
            new(1f, 2.3f),
            new(3.2f, 1f)
        ];

        public int[] m_patterns = [
            63,
            6,
            91,
            79,
            102,
            109,
            125,
            7,
            127,
            111,
            119,
            124,
            57,
            94,
            121,
            113
        ];

        public SevenSegmentDisplayElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) :
            base(subsystemElectricity, cellFace) => m_subsystemGlow = subsystemElectricity.Project.FindSubsystem<SubsystemGlow>(true);

        public override void OnAdded() {
            CellFace cellFace = CellFaces[0];
            int data = Terrain.ExtractData(SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z));
            int mountingFace = SevenSegmentDisplayBlock.GetMountingFace(data);
            m_color = LedBlock.LedColors[SevenSegmentDisplayBlock.GetColor(data)];
            for (int i = 0; i < 7; i++) {
                Vector3 v = new(cellFace.X + 0.5f, cellFace.Y + 0.5f, cellFace.Z + 0.5f);
                Vector3 vector = CellFace.FaceToVector3(mountingFace);
                Vector3 vector2 = mountingFace < 4 ? Vector3.UnitY : Vector3.UnitX;
                Vector3 v2 = Vector3.Cross(vector, vector2);
                m_glowPoints[i] = m_subsystemGlow.AddGlowPoint();
                m_glowPoints[i].Position = v
                    - 0.4375f * CellFace.FaceToVector3(mountingFace)
                    + m_centers[i].X * 0.0625f * v2
                    + m_centers[i].Y * 0.0625f * vector2;
                m_glowPoints[i].Forward = vector;
                m_glowPoints[i].Right = v2 * m_sizes[i].X * 0.0625f;
                m_glowPoints[i].Up = vector2 * m_sizes[i].Y * 0.0625f;
                m_glowPoints[i].Color = Color.Transparent;
                m_glowPoints[i].Size = 1.35f;
                m_glowPoints[i].FarSize = 1.35f;
                m_glowPoints[i].FarDistance = 1f;
                m_glowPoints[i].Type = m_sizes[i].X > m_sizes[i].Y ? GlowPointType.HorizontalRectangle : GlowPointType.VerticalRectangle;
            }
        }

        public override void OnRemoved() {
            for (int i = 0; i < 7; i++) {
                m_subsystemGlow.RemoveGlowPoint(m_glowPoints[i]);
            }
        }

        public override bool Simulate() {
            float voltage = m_voltage;
            m_voltage = 0f;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    m_voltage = MathUtils.Max(m_voltage, connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                }
            }
            if (m_voltage != voltage) {
                int num = (int)MathF.Round(m_voltage * 15f);
                for (int i = 0; i < 7; i++) {
                    m_glowPoints[i].Color = (m_patterns[num] & (1 << i)) != 0 ? m_color : Color.Transparent;
                }
            }
            return false;
        }
    }
}