using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemSaddleBlockBehavior : SubsystemBlockBehavior {
        public SubsystemAudio m_subsystemAudio;

        public Random m_random = new();

        public override int[] HandledBlocks => [158];

        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) {
            BodyRaycastResult? bodyRaycastResult = componentMiner.Raycast<BodyRaycastResult>(ray, RaycastMode.Interaction);
            if (bodyRaycastResult.HasValue) {
                ComponentHealth componentHealth = bodyRaycastResult.Value.ComponentBody.Entity.FindComponent<ComponentHealth>();
                if (componentHealth == null
                    || componentHealth.Health > 0f) {
                    string entityTemplateName = $"{bodyRaycastResult.Value.ComponentBody.Entity.ValuesDictionary.DatabaseObject.Name}_Saddled";
                    Entity entity = DatabaseManager.CreateEntity(Project, entityTemplateName, false);
                    if (entity != null) {
                        ComponentBody componentBody = entity.FindComponent<ComponentBody>(true);
                        componentBody.Position = bodyRaycastResult.Value.ComponentBody.Position;
                        componentBody.Rotation = bodyRaycastResult.Value.ComponentBody.Rotation;
                        componentBody.Velocity = bodyRaycastResult.Value.ComponentBody.Velocity;
                        entity.FindComponent<ComponentSpawn>(true).SpawnDuration = 0f;
                        Project.RemoveEntity(bodyRaycastResult.Value.ComponentBody.Entity, true);
                        Project.AddEntity(entity);
                        m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, m_random.Float(-0.1f, 0.1f), ray.Position, 1f, true);
                        componentMiner.RemoveActiveTool(1);
                    }
                }
                return true;
            }
            return false;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
        }
    }
}