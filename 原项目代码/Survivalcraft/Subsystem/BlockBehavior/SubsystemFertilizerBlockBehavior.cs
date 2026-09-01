using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemFertilizerBlockBehavior : SubsystemBlockBehavior {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public override int[] HandledBlocks => [102];

        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) {
            TerrainRaycastResult? terrainRaycastResult = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
            if (terrainRaycastResult.HasValue
                && terrainRaycastResult.Value.CellFace.Face == 4) {
                int y = terrainRaycastResult.Value.CellFace.Y;
                for (int i = terrainRaycastResult.Value.CellFace.X - 1; i <= terrainRaycastResult.Value.CellFace.X + 1; i++) {
                    for (int j = terrainRaycastResult.Value.CellFace.Z - 1; j <= terrainRaycastResult.Value.CellFace.Z + 1; j++) {
                        int cellValue = m_subsystemTerrain.Terrain.GetCellValue(i, y, j);
                        if (Terrain.ExtractContents(cellValue) == 168) {
                            int data = SoilBlock.SetNitrogen(Terrain.ExtractData(cellValue), 3);
                            int value = Terrain.ReplaceData(cellValue, data);
                            m_subsystemTerrain.ChangeCell(i, y, j, value);
                        }
                    }
                }
                m_subsystemAudio.PlayRandomSound(
                    "Audio/Impacts/Dirt",
                    0.5f,
                    0f,
                    new Vector3(terrainRaycastResult.Value.CellFace.X, terrainRaycastResult.Value.CellFace.Y, terrainRaycastResult.Value.CellFace.Z),
                    3f,
                    true
                );
                Vector3 position = new(
                    terrainRaycastResult.Value.CellFace.X + 0.5f,
                    terrainRaycastResult.Value.CellFace.Y + 1.5f,
                    terrainRaycastResult.Value.CellFace.Z + 0.5f
                );
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(componentMiner.ActiveBlockValue)];
                m_subsystemParticles.AddParticleSystem(
                    block.CreateDebrisParticleSystem(m_subsystemTerrain, position, componentMiner.ActiveBlockValue, 1.25f)
                );
                componentMiner.RemoveActiveTool(1);
                return true;
            }
            return false;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
        }
    }
}