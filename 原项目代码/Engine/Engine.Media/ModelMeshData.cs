namespace Engine.Media {
    public class ModelMeshData {
        public string Name;

        public int ParentBoneIndex;

        public bool IsVisible = true;

        public List<ModelMeshPartData> MeshParts = [];

        public BoundingBox BoundingBox;
    }
}