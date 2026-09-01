using Engine;

namespace Game {
    public abstract class ElectricElement {
        public SubsystemElectricity SubsystemElectricity { get; set; }

        public ReadOnlyList<CellFace> CellFaces { get; set; }

        public List<ElectricConnection> Connections { get; set; }

        public ElectricElement(SubsystemElectricity subsystemElectricity, IEnumerable<CellFace> cellFaces) {
            SubsystemElectricity = subsystemElectricity;
            CellFaces = new ReadOnlyList<CellFace>(new List<CellFace>(cellFaces));
            Connections = [];
        }

        public ElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : this(
            subsystemElectricity,
            new List<CellFace> { cellFace }
        ) { }

        public virtual float GetOutputVoltage(int face) => 0f;

        public virtual bool Simulate() => false;

        public virtual void OnAdded() { }

        public virtual void OnRemoved() { }

        public virtual void OnNeighborBlockChanged(CellFace cellFace, int neighborX, int neighborY, int neighborZ) { }

        public virtual bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) => false;

        public virtual void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody) { }

        public virtual void OnHitByProjectile(CellFace cellFace, WorldItem worldItem) { }

        public virtual void OnConnectionsChanged() { }

        public static bool IsSignalHigh(float voltage) => voltage >= 0.5f;

        public int CalculateHighInputsCount() {
            int num = 0;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0
                    && IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace))) {
                    num++;
                }
            }
            return num;
        }
    }
}