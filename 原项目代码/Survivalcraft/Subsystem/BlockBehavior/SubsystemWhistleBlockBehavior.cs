using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemWhistleBlockBehavior : SubsystemBlockBehavior {
        public SubsystemBodies m_subsystemBodies;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemNoise m_subsystemNoise;

        public Random m_random = new();

        public override int[] HandledBlocks => [160];

        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) {
            m_subsystemAudio.PlayRandomSound("Audio/Whistle", 1f, m_random.Float(-0.2f, 0f), ray.Position, 4f, true);
            m_subsystemNoise.MakeNoise(componentMiner.ComponentCreature.ComponentBody, 0.5f, 30f);
            DynamicArray<ComponentBody> dynamicArray = new();
            m_subsystemBodies.FindBodiesAroundPoint(
                new Vector2(componentMiner.ComponentCreature.ComponentBody.Position.X, componentMiner.ComponentCreature.ComponentBody.Position.Z),
                64f,
                dynamicArray
            );
            float num = float.PositiveInfinity;
            List<ComponentBody> list = new();
            foreach (ComponentBody item in dynamicArray) {
                ComponentSummonBehavior componentSummonBehavior = item.Entity.FindComponent<ComponentSummonBehavior>();
                if (componentSummonBehavior != null
                    && componentSummonBehavior.IsEnabled) {
                    float num2 = Vector3.Distance(item.Position, componentMiner.ComponentCreature.ComponentBody.Position);
                    if (num2 > 4f
                        && componentSummonBehavior.SummonTarget == null) {
                        list.Add(item);
                        num = MathUtils.Min(num, num2);
                    }
                    else {
                        componentSummonBehavior.SummonTarget = componentMiner.ComponentCreature.ComponentBody;
                    }
                }
            }
            foreach (ComponentBody item2 in list) {
                ComponentSummonBehavior componentSummonBehavior2 = item2.Entity.FindComponent<ComponentSummonBehavior>();
                if (componentSummonBehavior2 != null
                    && Vector3.Distance(item2.Position, componentMiner.ComponentCreature.ComponentBody.Position) < num + 4f) {
                    componentSummonBehavior2.SummonTarget = componentMiner.ComponentCreature.ComponentBody;
                }
            }
            componentMiner.DamageActiveTool(1);
            return true;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(true);
        }
    }
}