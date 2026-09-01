using System.Text;

namespace Engine.Serialization {
    public class EngineBinaryWriter : BinaryWriter {
        public EngineBinaryWriter(Stream stream, bool leaveOpen = false) : base(stream, Encoding.UTF8, leaveOpen) { }

        public new void Write7BitEncodedInt(int value) {
            base.Write7BitEncodedInt(value);
        }

        public virtual void Write(Color value) {
            Write(value.PackedValue);
        }

        public virtual void Write(Point2 value) {
            Write(value.X);
            Write(value.Y);
        }

        public virtual void Write(Point3 value) {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
        }

        public virtual void Write(Rectangle value) {
            Write(value.Left);
            Write(value.Top);
            Write(value.Width);
            Write(value.Height);
        }

        public virtual void Write(Box value) {
            Write(value.Left);
            Write(value.Top);
            Write(value.Near);
            Write(value.Width);
            Write(value.Height);
            Write(value.Depth);
        }

        public virtual void Write(Vector2 value) {
            Write(value.X);
            Write(value.Y);
        }

        public virtual void Write(Vector3 value) {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
        }

        public virtual void Write(Vector4 value) {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
            Write(value.W);
        }

        public virtual void Write(BoundingRectangle value) {
            Write(value.Min);
            Write(value.Max);
        }

        public virtual void Write(BoundingBox value) {
            Write(value.Min);
            Write(value.Max);
        }

        public virtual void Write(Plane value) {
            Write(value.Normal);
            Write(value.D);
        }

        public virtual void Write(Quaternion value) {
            Write(value.X);
            Write(value.Y);
            Write(value.Z);
            Write(value.W);
        }

        public virtual void Write(Matrix value) {
            Write(value.M11);
            Write(value.M12);
            Write(value.M13);
            Write(value.M14);
            Write(value.M21);
            Write(value.M22);
            Write(value.M23);
            Write(value.M24);
            Write(value.M31);
            Write(value.M32);
            Write(value.M33);
            Write(value.M34);
            Write(value.M41);
            Write(value.M42);
            Write(value.M43);
            Write(value.M44);
        }

        public virtual void WriteStruct<T>(T structure) where T : unmanaged {
            byte[] array = Utilities.StructureToArray(structure);
            Write(array, 0, array.Length);
        }
    }
}