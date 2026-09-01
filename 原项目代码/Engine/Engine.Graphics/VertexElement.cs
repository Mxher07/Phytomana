namespace Engine.Graphics {
    public class VertexElement : IEquatable<VertexElement> {
        readonly int m_hashCode;

        public int Offset { get; set; }

        public VertexElementFormat Format { get; }

        public string Semantic { get; }

        public string SemanticName { get; }

        public int SemanticIndex { get; }

        public VertexElement(VertexElementFormat format, string semantic) : this(-1, format, semantic) { }

        public VertexElement(VertexElementFormat format, VertexElementSemantic semantic) : this(-1, format, semantic) { }

        public VertexElement(int offset, VertexElementFormat format, string semantic) {
            if (string.IsNullOrEmpty(semantic)) {
                throw new ArgumentException("semantic cannot be empty or null.");
            }
            int num = semantic.Length;
            while (num > 0
                && char.IsDigit(semantic[num - 1])) {
                num--;
            }
            if (num == 0) {
                throw new ArgumentException("semantic cannot start with a digit.");
            }
            Offset = offset;
            Format = format;
            Semantic = semantic;
            SemanticName = semantic.Substring(0, num);
            SemanticIndex = num < semantic.Length ? int.Parse(semantic.Substring(num)) : 0;
            m_hashCode = Offset.GetHashCode() + Format.GetHashCode() + Semantic.GetHashCode();
        }

        public VertexElement(int offset, VertexElementFormat format, VertexElementSemantic semantic) : this(
            offset,
            format,
            semantic.GetSemanticString()
        ) { }

        public override int GetHashCode() => m_hashCode;

        public override bool Equals(object other) => other is VertexElement && Equals((VertexElement)other);

        public bool Equals(VertexElement other) => other != null && other.Offset == Offset && other.Format == Format && other.Semantic == Semantic;

        public static bool operator ==(VertexElement ve1, VertexElement ve2) => ve1?.Equals(ve2) ?? ((object)ve2 == null);

        public static bool operator !=(VertexElement ve1, VertexElement ve2) => !(ve1 == ve2);
    }
}