namespace Engine.Serialization {
    public class ColorSerializer : ISerializer<Color> {
        public void Serialize(InputArchive archive, ref Color value) {
            byte value2 = 0;
            byte value3 = 0;
            byte value4 = 0;
            byte value5 = 0;
            archive.Serialize("R", ref value2);
            archive.Serialize("G", ref value3);
            archive.Serialize("B", ref value4);
            archive.Serialize("A", ref value5);
            value = new Color(value2, value3, value4, value5);
        }

        public void Serialize(OutputArchive archive, Color value) {
            archive.Serialize("R", value.R);
            archive.Serialize("G", value.G);
            archive.Serialize("B", value.B);
            archive.Serialize("A", value.A);
        }
    }
}