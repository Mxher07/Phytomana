using System.Text;

namespace Engine.Serialization {
    public class EngineBinaryReader : BinaryReader {
        public EngineBinaryReader(Stream stream, bool leaveOpen = false) : base(stream, Encoding.UTF8, leaveOpen) { }

        public new int Read7BitEncodedInt() => base.Read7BitEncodedInt();

        public virtual Color ReadColor() => new(ReadUInt32());

        public virtual Point2 ReadPoint2() => new(ReadInt32(), ReadInt32());

        public virtual Point3 ReadPoint3() => new(ReadInt32(), ReadInt32(), ReadInt32());

        public virtual Rectangle ReadRectangle() => new(ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32());

        public virtual Box ReadBox() => new(ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32());

        public virtual Vector2 ReadVector2() => new(ReadSingle(), ReadSingle());

        public virtual Vector3 ReadVector3() => new(ReadSingle(), ReadSingle(), ReadSingle());

        public virtual Vector4 ReadVector4() => new(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        public virtual BoundingRectangle ReadBoundingRectagle() => new(ReadVector2(), ReadVector2());

        public virtual BoundingBox ReadBoundingBox() => new(ReadVector3(), ReadVector3());

        public virtual Plane ReadPlane() => new(ReadVector3(), ReadSingle());

        public virtual Quaternion ReadQuaternion() => new(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        public virtual Matrix ReadMatrix() => new(
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle(),
            ReadSingle()
        );

        public virtual T ReadStruct<T>() where T : unmanaged => Utilities.ArrayToStructure<T>(ReadBytes(Utilities.SizeOf<T>()));
    }
}