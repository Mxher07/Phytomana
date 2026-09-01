namespace Engine.Graphics {
    public class VertexDeclaration : IEquatable<VertexDeclaration> {
        public readonly VertexElement[] m_elements;

        public static List<VertexElement[]> m_allElements = [];

        public ReadOnlyList<VertexElement> VertexElements => new(m_elements);

        public int VertexStride { get; set; }

        public VertexDeclaration(params VertexElement[] elements) : this(true, elements) { }

        public VertexDeclaration(bool useCache = true, params VertexElement[] elements) {
            if (elements.Length == 0) {
                throw new ArgumentException("There must be at least one VertexElement.");
            }
            foreach (VertexElement vertexElement in elements) {
                if (vertexElement.Offset < 0) {
                    vertexElement.Offset = VertexStride;
                }
                VertexStride = MathUtils.Max(VertexStride, vertexElement.Offset + vertexElement.Format.GetSize());
            }
            if (!useCache) {
                m_elements = elements.ToArray();
                return;
            }
            foreach (VertexElement[] element in m_allElements) {
                if (elements.SequenceEqual(element)) {
                    m_elements = element;
                    break;
                }
            }
            if (m_elements == null) {
                m_elements = elements.ToArray();
                m_allElements.Add(m_elements);
            }
        }

        public override int GetHashCode() => m_elements.GetHashCode();

        public override bool Equals(object other) => other is VertexDeclaration && Equals((VertexDeclaration)other);

        public bool Equals(VertexDeclaration other) => (object)other != null && m_elements == other.m_elements;

        public static bool operator ==(VertexDeclaration vd1, VertexDeclaration vd2) => vd1?.Equals(vd2) ?? ((object)vd2 == null);

        public static bool operator !=(VertexDeclaration vd1, VertexDeclaration vd2) => !(vd1 == vd2);
    }
}