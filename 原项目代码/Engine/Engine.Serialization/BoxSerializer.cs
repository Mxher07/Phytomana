namespace Engine.Serialization {
    public class BoxSerializer : ISerializer<Box> {
        public void Serialize(InputArchive archive, ref Box value) {
            archive.Serialize("Left", ref value.Left);
            archive.Serialize("Top", ref value.Top);
            archive.Serialize("Near", ref value.Near);
            archive.Serialize("Width", ref value.Width);
            archive.Serialize("Height", ref value.Height);
            archive.Serialize("Depth", ref value.Depth);
        }

        public void Serialize(OutputArchive archive, Box value) {
            archive.Serialize("Left", value.Left);
            archive.Serialize("Top", value.Top);
            archive.Serialize("Near", value.Near);
            archive.Serialize("Width", value.Width);
            archive.Serialize("Height", value.Height);
            archive.Serialize("Depth", value.Depth);
        }
    }
}