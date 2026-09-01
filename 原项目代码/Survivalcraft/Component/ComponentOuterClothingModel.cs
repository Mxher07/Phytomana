using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentOuterClothingModel : ComponentModel {
        public ComponentHumanModel m_componentHumanModel;

        public ComponentCreature m_componentCreature;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_componentHumanModel = Entity.FindComponent<ComponentHumanModel>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
        }

        public override void Animate() {
            base.Animate();
            if (Animated) {
                return;
            }
            Opacity = m_componentHumanModel.Opacity;
            foreach (ModelBone bone in Model.Bones) {
                ModelBone modelBone = m_componentHumanModel.Model.FindBone(bone.Name);
                SetBoneTransform(bone.Index, m_componentHumanModel.GetBoneTransform(modelBone.Index));
            }
            if (Opacity.HasValue
                && Opacity.Value < 1f) {
                bool num = m_componentCreature.ComponentBody.ImmersionFactor >= 1f;
                bool flag = m_subsystemSky.ViewUnderWaterDepth > 0f;
                RenderingMode = num == flag ? ModelRenderingMode.TransparentAfterWater : ModelRenderingMode.TransparentBeforeWater;
            }
            else {
                RenderingMode = ModelRenderingMode.AlphaThreshold;
            }
        }

        public override void SetModel(Model model) {
            base.SetModel(model);
            if (IsSet) {
                return;
            }
            if (MeshDrawOrders.Length != 4) {
                throw new InvalidOperationException("Invalid number of meshes in OuterClothing model.");
            }
            MeshDrawOrders[0] = model.Meshes.IndexOf(model.FindMesh("Leg1"));
            MeshDrawOrders[1] = model.Meshes.IndexOf(model.FindMesh("Leg2"));
            MeshDrawOrders[2] = model.Meshes.IndexOf(model.FindMesh("Body"));
            MeshDrawOrders[3] = model.Meshes.IndexOf(model.FindMesh("Head"));
        }
    }
}