namespace Engine.Graphics {
    public class ModelBone {
        public List<ModelBone> m_childBones = [];

        public Matrix m_transform;

        public Model Model { get; set; }

        public int Index { get; set; }

        public string Name { get; set; }

        public Matrix Transform {
            get => m_transform;
            set => m_transform = value;
        }

        public ModelBone ParentBone { get; set; }

        public ReadOnlyList<ModelBone> ChildBones => new(m_childBones);
    }
}