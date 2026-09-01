namespace Engine.Serialization {
    public class RectangleSerializer : ISerializer<Rectangle> {
        public void Serialize(InputArchive archive, ref Rectangle value) {
            archive.Serialize("Left", ref value.Left);
            archive.Serialize("Top", ref value.Top);
            archive.Serialize("Width", ref value.Width);
            archive.Serialize("Height", ref value.Height);
        }

        public void Serialize(OutputArchive archive, Rectangle value) {
            archive.Serialize("Left", value.Left);
            archive.Serialize("Top", value.Top);
            archive.Serialize("Width", value.Width);
            archive.Serialize("Height", value.Height);
        }
    }
}