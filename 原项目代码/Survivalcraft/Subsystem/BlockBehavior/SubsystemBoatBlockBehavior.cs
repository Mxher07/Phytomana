using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemBoatBlockBehavior : SubsystemBlockBehavior {
        public SubsystemAudio m_subsystemAudio;

        public SubsystemBodies m_subsystemBodies;

        public SubsystemGameInfo m_subsystemGameInfo;

        public Random m_random = new();

        public static string fName = "SubsystemBoatBlockBehavior";

        public override int[] HandledBlocks => [178];

        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) {
            _ = componentMiner.Inventory;
            if (Terrain.ExtractContents(componentMiner.ActiveBlockValue) == 178) {
                TerrainRaycastResult? terrainRaycastResult = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Digging);
                if (terrainRaycastResult.HasValue) {
                    Vector3 position = terrainRaycastResult.Value.HitPoint();
                    DynamicArray<ComponentBody> dynamicArray = new();
                    m_subsystemBodies.FindBodiesInArea(
                        new Vector2(position.X, position.Z) - new Vector2(8f),
                        new Vector2(position.X, position.Z) + new Vector2(8f),
                        dynamicArray
                    );
                    if (dynamicArray.Count(b => b.Entity.ValuesDictionary.DatabaseObject.Name == "Boat") < 6
                        || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) {
                        Entity entity = DatabaseManager.CreateEntity(Project, "Boat", true);
                        entity.FindComponent<ComponentFrame>(true).Position = position;
                        entity.FindComponent<ComponentFrame>(true).Rotation = Quaternion.CreateFromAxisAngle(
                            Vector3.UnitY,
                            m_random.Float(0f, (float)Math.PI * 2f)
                        );
                        entity.FindComponent<ComponentSpawn>(true).SpawnDuration = 0f;
                        Project.AddEntity(entity);
                        componentMiner.RemoveActiveTool(1);
                        m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, position, 3f, true);
                    }
                    else {
                        componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, true, false);
                    }
                    return true;
                }
            }
            return false;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
        }
    }
}