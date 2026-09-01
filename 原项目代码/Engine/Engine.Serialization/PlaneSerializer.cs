namespace Engine.Serialization {
    public class PlaneSerializer : ISerializer<Plane> {
        public void Serialize(InputArchive archive, ref Plane value) {
            archive.Serialize("X", ref value.Normal.X);
            archive.Serialize("Y", ref value.Normal.Y);
            archive.Serialize("Z", ref value.Normal.Z);
            archive.Serialize("D", ref value.D);
        }

        public void Serialize(OutputArchive archive, Plane value) {
            archive.Serialize("X", value.Normal.X);
            archive.Serialize("Y", value.Normal.Y);
            archive.Serialize("Z", value.Normal.Z);
            archive.Serialize("D", value.D);
        }
    }
}