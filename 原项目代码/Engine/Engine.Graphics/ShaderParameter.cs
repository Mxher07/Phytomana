using System.Numerics;

namespace Engine.Graphics {
    public class ShaderParameter {
        public object Resource;
        public bool IsChanged = true;
        public readonly Shader Shader;
        public readonly string Name;
        public readonly ShaderParameterType Type;
        public readonly int Count;
        public int Location;
        public float[] Value;
        public int[] IntValue;

        public ShaderParameter(string name, ShaderParameterType type) {
            Name = name;
            Type = type;
        }

        public ShaderParameter(Shader shader, string name, ShaderParameterType type, int count) {
            Shader = shader;
            Name = name;
            Type = type;
            Count = count;
            switch (type) {
                case ShaderParameterType.Texture2D:
                case ShaderParameterType.Texture2DArray:
                case ShaderParameterType.Sampler2D:
                case ShaderParameterType.SamplerCube: break;
                case ShaderParameterType.Float: Value = new float[count]; break;
                case ShaderParameterType.Vector2: Value = new float[2 * count]; break;
                case ShaderParameterType.Vector3: Value = new float[3 * count]; break;
                case ShaderParameterType.Vector4: Value = new float[4 * count]; break;
                case ShaderParameterType.Matrix3: Value = new float[9 * count]; break;
                case ShaderParameterType.Matrix: Value = new float[16 * count]; break;
                case ShaderParameterType.Int: IntValue = new int[count]; break;
                case ShaderParameterType.IntVec2: IntValue = new int[2 * count]; break;
                case ShaderParameterType.IntVec3: IntValue = new int[3 * count]; break;
                case ShaderParameterType.IntVec4: IntValue = new int[4 * count]; break;
                default: throw new ArgumentException("type");
            }
        }

        public void SetValue(float value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Float
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (value != Value[0]) {
                Value[0] = value;
                IsChanged = true;
            }
        }

        public void SetValue(float[] value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            switch (value.Length) {
                case 1:
                    if (Type != ShaderParameterType.Float) {
                        throw new InvalidOperationException("Shader parameter type mismatch.");
                    }
                    if (value[0] != Value[0]) {
                        Value[0] = value[0];
                        IsChanged = true;
                    }
                    break;
                case 2:
                    if (Type != ShaderParameterType.Vector2) {
                        throw new InvalidOperationException("Shader parameter type mismatch.");
                    }
                    if (!value.SequenceEqual(Value)) {
                        Array.Copy(value, Value, 2);
                        IsChanged = true;
                    }
                    break;
                case 3:
                    if (Type != ShaderParameterType.Vector3) {
                        throw new InvalidOperationException("Shader parameter type mismatch.");
                    }
                    if (!value.SequenceEqual(Value)) {
                        Array.Copy(value, Value, 3);
                        IsChanged = true;
                    }
                    break;
                case 4:
                    if (Type != ShaderParameterType.Vector4) {
                        throw new InvalidOperationException("Shader parameter type mismatch.");
                    }
                    if (!value.SequenceEqual(Value)) {
                        Array.Copy(value, Value, 4);
                        IsChanged = true;
                    }
                    break;
                case 16:
                    if (Type != ShaderParameterType.Matrix) {
                        throw new InvalidOperationException("Shader parameter type mismatch.");
                    }
                    if (!value.SequenceEqual(Value)) {
                        Array.Copy(value, Value, 16);
                        IsChanged = true;
                    }
                    break;
                default:
                    throw new ArgumentException("value");
            }
        }

        public void SetValue(float[] value, int count) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Float) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0
                || count > value.Length
                || count > Count) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (!IsChanged) {
                for (int i = 0; i < count; i++) {
                    if (Value[i] != value[i]) {
                        IsChanged = true;
                        break;
                    }
                }
            }
            if (IsChanged) {
                for (int j = 0; j < count; j++) {
                    Value[j] = value[j];
                }
                IsChanged = true;
            }
        }

        public void SetValue(Vector2 value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Vector2
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (IsChanged
                || value.X != Value[0]
                || value.Y != Value[1]) {
                Value[0] = value.X;
                Value[1] = value.Y;
                IsChanged = true;
            }
        }

        public void SetValue(Vector2[] value, int count) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Vector2) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0
                || count > value.Length
                || count > Count) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (!IsChanged) {
                int i = 0;
                int num = 0;
                for (; i < count; i++) {
                    if (Value[num++] != value[i].X
                        || Value[num++] != value[i].Y) {
                        IsChanged = true;
                        break;
                    }
                }
            }
            if (IsChanged) {
                int j = 0;
                int num2 = 0;
                for (; j < count; j++) {
                    Value[num2++] = value[j].X;
                    Value[num2++] = value[j].Y;
                }
            }
        }

        public void SetValue(Vector3 value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Vector3
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (IsChanged
                || value.X != Value[0]
                || value.Y != Value[1]
                || value.Z != Value[2]) {
                Value[0] = value.X;
                Value[1] = value.Y;
                Value[2] = value.Z;
                IsChanged = true;
            }
        }

        public void SetValue(Vector3[] value, int count) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Vector3) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0
                || count > value.Length
                || count > Count) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (!IsChanged) {
                int i = 0;
                int num = 0;
                for (; i < count; i++) {
                    if (Value[num++] != value[i].X
                        || Value[num++] != value[i].Y
                        || Value[num++] != value[i].Z) {
                        IsChanged = true;
                        break;
                    }
                }
            }
            if (IsChanged) {
                int j = 0;
                int num2 = 0;
                for (; j < count; j++) {
                    Value[num2++] = value[j].X;
                    Value[num2++] = value[j].Y;
                    Value[num2++] = value[j].Z;
                }
            }
        }

        public void SetValue(Vector4 value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Vector4
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (IsChanged
                || value.X != Value[0]
                || value.Y != Value[1]
                || value.Z != Value[2]
                || value.W != Value[3]) {
                Value[0] = value.X;
                Value[1] = value.Y;
                Value[2] = value.Z;
                Value[3] = value.W;
                IsChanged = true;
            }
        }

        public void SetValue(Vector4[] value, int count) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Vector4) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0
                || count > value.Length
                || count > Count) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (!IsChanged) {
                int i = 0;
                int num = 0;
                for (; i < count; i++) {
                    if (Value[num++] != value[i].X
                        || Value[num++] != value[i].Y
                        || Value[num++] != value[i].Z
                        || Value[num++] != value[i].W) {
                        IsChanged = true;
                        break;
                    }
                }
            }
            if (IsChanged) {
                int j = 0;
                int num2 = 0;
                for (; j < count; j++) {
                    Value[num2++] = value[j].X;
                    Value[num2++] = value[j].Y;
                    Value[num2++] = value[j].Z;
                    Value[num2++] = value[j].W;
                }
            }
        }

        public void SetValue(Matrix value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Matrix
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (IsChanged
                || value.M11 != Value[0]
                || value.M12 != Value[1]
                || value.M13 != Value[2]
                || value.M14 != Value[3]
                || value.M21 != Value[4]
                || value.M22 != Value[5]
                || value.M23 != Value[6]
                || value.M24 != Value[7]
                || value.M31 != Value[8]
                || value.M32 != Value[9]
                || value.M33 != Value[10]
                || value.M34 != Value[11]
                || value.M41 != Value[12]
                || value.M42 != Value[13]
                || value.M43 != Value[14]
                || value.M44 != Value[15]) {
                Value[0] = value.M11;
                Value[1] = value.M12;
                Value[2] = value.M13;
                Value[3] = value.M14;
                Value[4] = value.M21;
                Value[5] = value.M22;
                Value[6] = value.M23;
                Value[7] = value.M24;
                Value[8] = value.M31;
                Value[9] = value.M32;
                Value[10] = value.M33;
                Value[11] = value.M34;
                Value[12] = value.M41;
                Value[13] = value.M42;
                Value[14] = value.M43;
                Value[15] = value.M44;
                IsChanged = true;
            }
        }

        public void SetValue(Matrix[] value, int count) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Matrix) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (count < 0
                || count > value.Length
                || count > Count) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (!IsChanged) {
                int i = 0;
                int num = 0;
                for (; i < count; i++) {
                    if (Value[num++] != value[i].M11
                        || Value[num++] != value[i].M12
                        || Value[num++] != value[i].M13
                        || Value[num++] != value[i].M14
                        || Value[num++] != value[i].M21
                        || Value[num++] != value[i].M22
                        || Value[num++] != value[i].M23
                        || Value[num++] != value[i].M24
                        || Value[num++] != value[i].M31
                        || Value[num++] != value[i].M32
                        || Value[num++] != value[i].M33
                        || Value[num++] != value[i].M34
                        || Value[num++] != value[i].M41
                        || Value[num++] != value[i].M42
                        || Value[num++] != value[i].M43
                        || Value[num++] != value[i].M44) {
                        IsChanged = true;
                        break;
                    }
                }
            }
            if (IsChanged) {
                int j = 0;
                int num2 = 0;
                for (; j < count; j++) {
                    Value[num2++] = value[j].M11;
                    Value[num2++] = value[j].M12;
                    Value[num2++] = value[j].M13;
                    Value[num2++] = value[j].M14;
                    Value[num2++] = value[j].M21;
                    Value[num2++] = value[j].M22;
                    Value[num2++] = value[j].M23;
                    Value[num2++] = value[j].M24;
                    Value[num2++] = value[j].M31;
                    Value[num2++] = value[j].M32;
                    Value[num2++] = value[j].M33;
                    Value[num2++] = value[j].M34;
                    Value[num2++] = value[j].M41;
                    Value[num2++] = value[j].M42;
                    Value[num2++] = value[j].M43;
                    Value[num2++] = value[j].M44;
                }
            }
        }

        /// <summary>
        /// 设置 mat3 uniform 值（从 System.Numerics.Matrix3x2 转换）
        /// Matrix3x2 转换为 mat3：
        /// | M11 M12 0 |
        /// | M21 M22 0 |
        /// | M31 M32 1 |
        /// </summary>
        public void SetValue(Matrix3x2 value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Matrix3
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            // Matrix3x2 到 mat3 的转换（列主序）
            // mat3: col0=[M11, M21, 0], col1=[M12, M22, 0], col2=[M31, M32, 1]
            if (IsChanged
                || value.M11 != Value[0]
                || value.M21 != Value[1]
                || value.M12 != Value[3]
                || value.M22 != Value[4]
                || value.M31 != Value[6]
                || value.M32 != Value[7]) {
                // 第一列
                Value[0] = value.M11;
                Value[1] = value.M21;
                Value[2] = 0f;
                // 第二列
                Value[3] = value.M12;
                Value[4] = value.M22;
                Value[5] = 0f;
                // 第三列
                Value[6] = value.M31;
                Value[7] = value.M32;
                Value[8] = 1f;
                IsChanged = true;
            }
        }

        public void SetValue(Texture2D value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if ((Type != ShaderParameterType.Texture2D
                 && Type != ShaderParameterType.Texture2DArray)
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (value != Resource) {
                Resource = value;
                IsChanged = true;
            }
        }

        public void SetValue(CubemapTexture value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.SamplerCube
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (value != Resource) {
                Resource = value;
                IsChanged = true;
            }
        }

        public void SetValue(SamplerState value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Sampler2D
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (value != Resource) {
                Resource = value;
                IsChanged = true;
            }
        }

        public void SetValue(int value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Int
                || Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (value != IntValue[0]) {
                IntValue[0] = value;
                IsChanged = true;
            }
        }

        public void SetValue(int[] value) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Count != 1) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            switch (value.Length) {
                case 1:
                    if (Type != ShaderParameterType.Int) {
                        throw new InvalidOperationException("Shader parameter type mismatch.");
                    }
                    if (value[0] != IntValue[0]) {
                        IntValue[0] = value[0];
                        IsChanged = true;
                    }
                    break;
                case 2:
                    if (Type != ShaderParameterType.IntVec2) {
                        throw new InvalidOperationException("Shader parameter type mismatch.");
                    }
                    if (!value.SequenceEqual(IntValue)) {
                        Array.Copy(value, IntValue, 2);
                        IsChanged = true;
                    }
                    break;
                case 3:
                    if (Type != ShaderParameterType.IntVec3) {
                        throw new InvalidOperationException("Shader parameter type mismatch.");
                    }
                    if (!value.SequenceEqual(IntValue)) {
                        Array.Copy(value, IntValue, 3);
                        IsChanged = true;
                    }
                    break;
                case 4:
                    if (Type != ShaderParameterType.IntVec4) {
                        throw new InvalidOperationException("Shader parameter type mismatch.");
                    }
                    if (!value.SequenceEqual(IntValue)) {
                        Array.Copy(value, IntValue, 4);
                        IsChanged = true;
                    }
                    break;
                default:
                    throw new InvalidOperationException("Shader parameter type mismatch.");
            }
        }

        public void SetValue(int[] value, int count) {
            if (Type == ShaderParameterType.Null) {
                return;
            }
            if (Type != ShaderParameterType.Int
                || Count != count) {
                throw new InvalidOperationException("Shader parameter type mismatch.");
            }
            if (!IsChanged) {
                for (int i = 0; i < count; i++) {
                    if (IntValue[i] != value[i]) {
                        IsChanged = true;
                        break;
                    }
                }
            }
            if (IsChanged) {
                for (int j = 0; j < count; j++) {
                    IntValue[j] = value[j];
                }
            }
        }

        public static unsafe bool Compare(Vector2* a, Vector2* b) => *(long*)a == *(long*)b;

        public static unsafe bool Compare(Vector3* a, Vector3* b) {
            if (*(long*)a != *(long*)b) {
                return false;
            }
            if (*(int*)((byte*)a + 2 * (nint)4) != *(int*)((byte*)b + 2 * (nint)4)) {
                return false;
            }
            return true;
        }

        public static unsafe bool Compare(Vector4* a, Vector4* b) {
            if (*(long*)a != *(long*)b) {
                return false;
            }
            if (*(long*)((byte*)a + 8) != *(long*)((byte*)b + 8)) {
                return false;
            }
            return true;
        }

        public static unsafe bool Compare(Matrix* a, Matrix* b) {
            if (*(long*)a != *(long*)b) {
                return false;
            }
            if (*(long*)((byte*)a + 8) != *(long*)((byte*)b + 8)) {
                return false;
            }
            if (*(long*)((byte*)a + 2 * (nint)8) != *(long*)((byte*)b + 2 * (nint)8)) {
                return false;
            }
            if (*(long*)((byte*)a + 3 * (nint)8) != *(long*)((byte*)b + 3 * (nint)8)) {
                return false;
            }
            if (*(long*)((byte*)a + 4 * (nint)8) != *(long*)((byte*)b + 4 * (nint)8)) {
                return false;
            }
            if (*(long*)((byte*)a + 5 * (nint)8) != *(long*)((byte*)b + 5 * (nint)8)) {
                return false;
            }
            if (*(long*)((byte*)a + 6 * (nint)8) != *(long*)((byte*)b + 6 * (nint)8)) {
                return false;
            }
            if (*(long*)((byte*)a + 7 * (nint)8) != *(long*)((byte*)b + 7 * (nint)8)) {
                return false;
            }
            return true;
        }
    }
}