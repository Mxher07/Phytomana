using Engine;

namespace Game {
    public class ButtonFurnitureElectricElement : FurnitureElectricElement {
        public float m_voltage;

        public bool m_wasPressed;

        public ButtonFurnitureElectricElement(SubsystemElectricity subsystemElectricity, Point3 point) : base(subsystemElectricity, point) { }

        public void Press() {
            if (!m_wasPressed
                && !IsSignalHigh(m_voltage)) {
                m_wasPressed = true;
                CellFace cellFace = CellFaces[0];
                SubsystemElectricity.SubsystemAudio.PlaySound("Audio/Click", 1f, 0f, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f, true);
                SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 1);
            }
        }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            if (m_wasPressed) {
                m_wasPressed = false;
                m_voltage = 1f;
                SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 10);
            }
            else {
                m_voltage = 0f;
            }
            return m_voltage != voltage;
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            Press();
            return true;
        }

        public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem) {
            Press();
        }
    }
}