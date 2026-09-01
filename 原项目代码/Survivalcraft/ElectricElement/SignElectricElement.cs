using Engine;

namespace Game {
    public class SignElectricElement : ElectricElement {
        public bool m_isMessageAllowed = true;

        public double? m_lastMessageTime;

        public SignElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override bool Simulate() {
            bool flag = CalculateHighInputsCount() > 0;
            if (flag
                && m_isMessageAllowed
                && (!m_lastMessageTime.HasValue || SubsystemElectricity.SubsystemTime.GameTime - m_lastMessageTime.Value > 0.5)) {
                m_isMessageAllowed = false;
                m_lastMessageTime = SubsystemElectricity.SubsystemTime.GameTime;
                SignData signData = SubsystemElectricity.Project.FindSubsystem<SubsystemSignBlockBehavior>(true)
                    .GetSignData(new Point3(CellFaces[0].X, CellFaces[0].Y, CellFaces[0].Z));
                if (signData != null) {
                    string text = string.Join("\n", signData.Lines);
                    text = text.Trim('\n');
                    text = text.Replace("\\\n", "");
                    Color color = signData.Colors[0] == Color.Black ? Color.White : signData.Colors[0];
                    color *= 255f / MathUtils.Max(color.R, color.G, color.B);
                    foreach (ComponentPlayer componentPlayer in SubsystemElectricity.Project.FindSubsystem<SubsystemPlayers>(true).ComponentPlayers) {
                        componentPlayer.ComponentGui.DisplaySmallMessage(text, color, true, true);
                    }
                }
            }
            if (!flag) {
                m_isMessageAllowed = true;
            }
            return false;
        }
    }
}