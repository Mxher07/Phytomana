namespace Engine.Serialization {
    public class MatrixSerializer : ISerializer<Matrix> {
        public void Serialize(InputArchive archive, ref Matrix value) {
            archive.Serialize("M11", ref value.M11);
            archive.Serialize("M12", ref value.M12);
            archive.Serialize("M13", ref value.M13);
            archive.Serialize("M14", ref value.M14);
            archive.Serialize("M21", ref value.M21);
            archive.Serialize("M22", ref value.M22);
            archive.Serialize("M23", ref value.M23);
            archive.Serialize("M24", ref value.M24);
            archive.Serialize("M31", ref value.M31);
            archive.Serialize("M32", ref value.M32);
            archive.Serialize("M33", ref value.M33);
            archive.Serialize("M34", ref value.M34);
            archive.Serialize("M41", ref value.M41);
            archive.Serialize("M42", ref value.M42);
            archive.Serialize("M43", ref value.M43);
            archive.Serialize("M44", ref value.M44);
        }

        public void Serialize(OutputArchive archive, Matrix value) {
            archive.Serialize("M11", value.M11);
            archive.Serialize("M12", value.M12);
            archive.Serialize("M13", value.M13);
            archive.Serialize("M14", value.M14);
            archive.Serialize("M21", value.M21);
            archive.Serialize("M22", value.M22);
            archive.Serialize("M23", value.M23);
            archive.Serialize("M24", value.M24);
            archive.Serialize("M31", value.M31);
            archive.Serialize("M32", value.M32);
            archive.Serialize("M33", value.M33);
            archive.Serialize("M34", value.M34);
            archive.Serialize("M41", value.M41);
            archive.Serialize("M42", value.M42);
            archive.Serialize("M43", value.M43);
            archive.Serialize("M44", value.M44);
        }
    }
}