using System.Drawing;

namespace Engine.Serialization {
    public class SystemDrawingTypesConverters {
        [HumanReadableConverter(typeof(System.Drawing.Color))]
        public class ColorStringConverter : IHumanReadableConverter {
            public string ConvertToString(object value) {
                System.Drawing.Color color = (System.Drawing.Color)value;
                return color.A != byte.MaxValue
                    ? HumanReadableConverter.ValuesListToString(',', color.A, color.R, color.G, color.B)
                    : HumanReadableConverter.ValuesListToString(',', color.R, color.G, color.B);
            }

            public object ConvertFromString(Type type, string data) {
                data = data.Trim();
                if (data.Length > 0) {
                    if (!char.IsDigit(data[0])
                        && data[0] != '-'
                        && data[0] != '+') {
                        return System.Drawing.Color.FromName(data);
                    }
                    byte[] array = HumanReadableConverter.ValuesListFromString<byte>(',', data);
                    if (array.Length == 3) {
                        return System.Drawing.Color.FromArgb(array[0], array[1], array[2]);
                    }
                    if (array.Length == 4) {
                        return System.Drawing.Color.FromArgb(array[0], array[1], array[2], array[3]);
                    }
                }
                throw new InvalidOperationException($"Cannot convert string \"{data}\" to a value of type {type.FullName}.");
            }
        }

        [HumanReadableConverter(typeof(Point))]
        public class PointStringConverter : IHumanReadableConverter {
            public string ConvertToString(object value) {
                Point point = (Point)value;
                return HumanReadableConverter.ValuesListToString(',', point.X, point.Y);
            }

            public object ConvertFromString(Type type, string data) {
                int[] array = HumanReadableConverter.ValuesListFromString<int>(',', data);
                return array.Length == 2
                    ? (object)new Point(array[0], array[1])
                    : throw new InvalidOperationException($"Cannot convert string \"{data}\" to a value of type {type.FullName}.");
            }
        }

        [HumanReadableConverter(typeof(PointF))]
        public class PointFStringConverter : IHumanReadableConverter {
            public string ConvertToString(object value) {
                PointF pointF = (PointF)value;
                return HumanReadableConverter.ValuesListToString(',', pointF.X, pointF.Y);
            }

            public object ConvertFromString(Type type, string data) {
                float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
                return array.Length == 2
                    ? (object)new PointF(array[0], array[1])
                    : throw new InvalidOperationException($"Cannot convert string \"{data}\" to a value of type {type.FullName}.");
            }
        }

        [HumanReadableConverter(typeof(Size))]
        public class SizeStringConverter : IHumanReadableConverter {
            public string ConvertToString(object value) {
                Size size = (Size)value;
                return HumanReadableConverter.ValuesListToString(',', size.Width, size.Height);
            }

            public object ConvertFromString(Type type, string data) {
                int[] array = HumanReadableConverter.ValuesListFromString<int>(',', data);
                return array.Length == 2
                    ? (object)new Size(array[0], array[1])
                    : throw new InvalidOperationException($"Cannot convert string \"{data}\" to a value of type {type.FullName}.");
            }
        }

        [HumanReadableConverter(typeof(SizeF))]
        public class SizeFStringConverter : IHumanReadableConverter {
            public string ConvertToString(object value) {
                SizeF sizeF = (SizeF)value;
                return HumanReadableConverter.ValuesListToString(',', sizeF.Width, sizeF.Height);
            }

            public object ConvertFromString(Type type, string data) {
                float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
                return array.Length == 2
                    ? (object)new SizeF(array[0], array[1])
                    : throw new InvalidOperationException($"Cannot convert string \"{data}\" to a value of type {type.FullName}.");
            }
        }

        [HumanReadableConverter(typeof(System.Drawing.Rectangle))]
        public class RectangleStringConverter : IHumanReadableConverter {
            public string ConvertToString(object value) {
                System.Drawing.Rectangle rectangle = (System.Drawing.Rectangle)value;
                return HumanReadableConverter.ValuesListToString(',', rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
            }

            public object ConvertFromString(Type type, string data) {
                int[] array = HumanReadableConverter.ValuesListFromString<int>(',', data);
                return array.Length == 4
                    ? (object)new System.Drawing.Rectangle(array[0], array[1], array[2], array[3])
                    : throw new InvalidOperationException($"Cannot convert string \"{data}\" to a value of type {type.FullName}.");
            }
        }

        [HumanReadableConverter(typeof(RectangleF))]
        public class RectangleFStringConverter : IHumanReadableConverter {
            public string ConvertToString(object value) {
                RectangleF rectangleF = (RectangleF)value;
                return HumanReadableConverter.ValuesListToString(',', rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
            }

            public object ConvertFromString(Type type, string data) {
                float[] array = HumanReadableConverter.ValuesListFromString<float>(',', data);
                return array.Length == 4
                    ? (object)new RectangleF(array[0], array[1], array[2], array[3])
                    : throw new InvalidOperationException($"Cannot convert string \"{data}\" to a value of type {type.FullName}.");
            }
        }
    }
}