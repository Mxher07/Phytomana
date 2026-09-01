using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFrame : Component {
        public Vector3 m_position;

        public Quaternion m_rotation;

        public bool m_cachedMatrixValid;

        public Matrix m_cachedMatrix;

        public Vector3 Position {
            get => m_position;
            set {
                if (value != m_position) {
                    m_cachedMatrixValid = false;
                    m_position = value;
                    PositionChanged?.Invoke(this);
                }
            }
        }

        public Quaternion Rotation {
            get => m_rotation;
            set {
                value = Quaternion.Normalize(value);
                if (value != m_rotation) {
                    m_cachedMatrixValid = false;
                    m_rotation = value;
                    RotationChanged?.Invoke(this);
                }
            }
        }

        public Matrix Matrix {
            get {
                if (!m_cachedMatrixValid) {
                    m_cachedMatrix = Matrix.CreateFromQuaternion(Rotation);
                    m_cachedMatrix.Translation = Position;
                }
                return m_cachedMatrix;
            }
        }

        public virtual Action<ComponentFrame> PositionChanged { get; set; }
        public virtual Action<ComponentFrame> RotationChanged { get; set; }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            Position = valuesDictionary.GetValue<Vector3>("Position").FixNaN();
            Rotation = valuesDictionary.GetValue<Quaternion>("Rotation").FixNaN();
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("Position", Position.FixNaN());
            valuesDictionary.SetValue("Rotation", Rotation.FixNaN());
        }
    }
}