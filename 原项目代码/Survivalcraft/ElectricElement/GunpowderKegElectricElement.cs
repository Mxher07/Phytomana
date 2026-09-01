namespace Game {
    public class GunpowderKegElectricElement : ElectricElement {
        public GunpowderKegElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override bool Simulate() {
            if (CalculateHighInputsCount() > 0) {
                CellFace cellFace = CellFaces[0];
                SubsystemElectricity.Project.FindSubsystem<SubsystemExplosivesBlockBehavior>(true).IgniteFuse(cellFace.X, cellFace.Y, cellFace.Z);
            }
            return false;
        }
    }
}