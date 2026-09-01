using System.Globalization;
using System.Text;
using Engine;
using Engine.Graphics;
using TemplatesDatabase;

namespace Game {
    public class FurnitureDesign {
        public struct Cell {
            public int Value;
        }

        public static string fName = "FurnitureDesign";

        public struct Subdivision {
            public int TotalVolume;

            public int MinVolume;

            public List<Box> Boxes;
        }

        public const int MinResolution = 2;

        public const int maxDesign = 8192; //家具上限数量

        public const int MaxResolution = 256; //家具格分辨率

        public const int MaxTriangles = 65535; //家具复杂度

        public const int MaxNameLength = 100;

        public int m_index = -1;

        public string m_name = string.Empty;

        public FurnitureSet m_furnitureSet;

        public SubsystemTerrain m_subsystemTerrain;

        public bool m_gcUsed;

        public int m_terrainUseCount;

        public int m_loadTimeLinkedDesignIndex = -1;

        public int m_resolution;

        public int[] m_values;

        public int? m_hash;

        public FurnitureGeometry m_geometry;

        public Box? m_box;

        public int? m_shadowStrengthFactor;

        public BoundingBox[][] m_collisionBoxesByRotation;

        public BoundingBox[][] m_interactionBoxesByRotation;

        public BoundingBox[][] m_torchPointsByRotation;

        public int m_mainValue;

        public int m_mountingFacesMask = -1;

        public int m_transparentFacesMask = -1;

        public FurnitureDesign m_linkedDesign;

        public FurnitureInteractionMode m_interactionMode;

        public int Resolution => m_resolution;

        public int Hash {
            get {
                if (!m_hash.HasValue) {
                    m_hash = m_resolution + ((int)m_interactionMode << 4);
                    for (int i = 0; i < m_values.Length; i++) {
                        m_hash += m_values[i] * (1 + 113 * i);
                    }
                }
                return m_hash.Value;
            }
        }

        public Box Box {
            get {
                if (!m_box.HasValue) {
                    m_box = CalculateBox(new Box(0, 0, 0, Resolution, Resolution, Resolution), CreatePrecedingEmptySpacesArray());
                }
                return m_box.Value;
            }
        }

        public int ShadowStrengthFactor {
            get {
                if (!m_shadowStrengthFactor.HasValue) {
                    CalculateShadowStrengthFactor();
                }
                return m_shadowStrengthFactor.Value;
            }
        }

        public bool IsLightEmitter => GetTorchPoints(0).Length != 0;

        public int MainValue {
            get {
                if (m_mainValue == 0) {
                    CalculateMainValue();
                }
                return m_mainValue;
            }
        }

        public int MountingFacesMask {
            get {
                if (m_mountingFacesMask < 0) {
                    CalculateFacesMasks();
                }
                return m_mountingFacesMask;
            }
        }

        public int TransparentFacesMask {
            get {
                if (m_transparentFacesMask < 0) {
                    CalculateFacesMasks();
                }
                return m_transparentFacesMask;
            }
        }

        public int Index {
            get => m_index;
            set => m_index = value;
        }

        public string Name {
            get => m_name;
            set {
                if (value.Length > 0) {
                    if (value[0] == ' '
                        || value[^1] == ' ') {
                        throw new InvalidOperationException(LanguageControl.Get(fName, 1));
                    }
                    /*string text = value;
                    foreach (char c in text)
                    {
                        if (c > '\u007f' || (!char.IsLetterOrDigit(c) && c != ' '))
                        {
                            throw new InvalidOperationException(LanguageControl.Get(fName, 1));
                        }
                    }*/
                    if (value.Length > 20) {
                        value = value.Substring(0, 20);
                    }
                }
                m_name = value;
            }
        }

        public FurnitureSet FurnitureSet {
            get => m_furnitureSet;
            set => m_furnitureSet = value;
        }

        public FurnitureDesign LinkedDesign {
            get => m_linkedDesign;
            set {
                if (value != m_linkedDesign) {
                    m_linkedDesign = value;
                    m_hash = null;
                }
            }
        }

        public FurnitureInteractionMode InteractionMode {
            get => m_interactionMode;
            set {
                if (value != m_interactionMode) {
                    m_interactionMode = value;
                    m_hash = null;
                }
            }
        }

        public FurnitureGeometry Geometry {
            get {
                if (m_geometry == null) {
                    CreateGeometry();
                }
                return m_geometry;
            }
        }

        public FurnitureDesign(SubsystemTerrain subsystemTerrain) => m_subsystemTerrain = subsystemTerrain;

        public FurnitureDesign(int index, SubsystemTerrain subsystemTerrain, ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = subsystemTerrain;
            m_index = index;
            Name = valuesDictionary.GetValue("Name", string.Empty);
            m_terrainUseCount = valuesDictionary.GetValue<int>("TerrainUseCount");
            int value = valuesDictionary.GetValue<int>("Resolution");
            InteractionMode = valuesDictionary.GetValue<FurnitureInteractionMode>("InteractionMode");
            m_loadTimeLinkedDesignIndex = valuesDictionary.GetValue("LinkedDesign", -1);
            string value2 = valuesDictionary.GetValue<string>("Values");
            int num = 0;
            int[] array = new int[value * value * value];
            string[] array2 = value2.Split([','], StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < array2.Length; i++) {
                string[] array3 = array2[i].Split(['*'], StringSplitOptions.None);
                if (array3.Length != 2) {
                    throw new InvalidOperationException(LanguageControl.Get(fName, 2));
                }
                int num2 = int.Parse(array3[0], CultureInfo.InvariantCulture);
                int num3 = int.Parse(array3[1], CultureInfo.InvariantCulture);
                int num4 = 0;
                while (num4 < num2) {
                    array[num] = num3;
                    num4++;
                    num++;
                }
            }
            SetValues(value, array);
        }

        public int GetValue(int index) => m_values[index];

        public void SetValues(int resolution, int[] values) {
            if (resolution < MinResolution
                || resolution > MaxResolution) {
                throw new ArgumentException(LanguageControl.Get(fName, 3));
            }
            if (values.Length != resolution * resolution * resolution) {
                throw new ArgumentException(LanguageControl.Get(fName, 4));
            }
            m_resolution = resolution;
            if (m_values == null
                || m_values.Length != resolution * resolution * resolution) {
                m_values = new int[resolution * resolution * resolution];
            }
            values.CopyTo(m_values, 0);
            m_hash = null;
            m_geometry = null;
            m_box = null;
            m_collisionBoxesByRotation = null;
            m_interactionBoxesByRotation = null;
            m_torchPointsByRotation = null;
            m_mainValue = 0;
            m_mountingFacesMask = -1;
            m_transparentFacesMask = -1;
        }

        public string GetDefaultName() {
            if (InteractionMode == FurnitureInteractionMode.Multistate) {
                int count = ListChain().Count;
                if (count > 1) {
                    return string.Format(LanguageControl.Get(fName, 5), count);
                }
            }
            else {
                if (InteractionMode == FurnitureInteractionMode.ElectricButton) {
                    return LanguageControl.Get(fName, 6);
                }
                if (InteractionMode == FurnitureInteractionMode.ElectricSwitch) {
                    return LanguageControl.Get(fName, 7);
                }
                if (InteractionMode == FurnitureInteractionMode.ConnectedMultistate) {
                    int count2 = ListChain().Count;
                    if (count2 > 1) {
                        return string.Format(LanguageControl.Get(fName, 8), count2);
                    }
                }
            }
            return LanguageControl.Get(fName, 9);
        }

        public BoundingBox[] GetCollisionBoxes(int rotation) {
            if (m_collisionBoxesByRotation == null) {
                CreateCollisionAndInteractionBoxes();
            }
            return m_collisionBoxesByRotation[rotation];
        }

        public BoundingBox[] GetInteractionBoxes(int rotation) {
            if (m_interactionBoxesByRotation == null) {
                CreateCollisionAndInteractionBoxes();
            }
            return m_interactionBoxesByRotation[rotation];
        }

        public BoundingBox[] GetTorchPoints(int rotation) {
            if (m_torchPointsByRotation == null) {
                CreateTorchPoints();
            }
            return m_torchPointsByRotation[rotation];
        }

        public void Paint(int? color) {
            int[] array = new int[m_values.Length];
            for (int i = 0; i < m_values.Length; i++) {
                int num = m_values[i];
                int num2 = Terrain.ExtractContents(num);
                IPaintableBlock paintableBlock = BlocksManager.Blocks[num2] as IPaintableBlock;
                array[i] = paintableBlock?.Paint(null, num, color) ?? num;
            }
            SetValues(Resolution, array);
        }

        public void Resize(int resolution) {
            if (resolution < MinResolution
                || resolution > MaxResolution) {
                throw new ArgumentException(LanguageControl.Get(fName, 3));
            }
            if (resolution == m_resolution) {
                return;
            }
            int[] array = new int[resolution * resolution * resolution];
            for (int i = 0; i < resolution; i++) {
                for (int j = 0; j < resolution; j++) {
                    for (int k = 0; k < resolution; k++) {
                        if (k >= 0
                            && k < m_resolution
                            && j >= 0
                            && j < m_resolution
                            && i >= 0
                            && i < m_resolution) {
                            array[k + j * resolution + i * resolution * resolution] =
                                m_values[k + j * m_resolution + i * m_resolution * m_resolution];
                        }
                    }
                }
            }
            SetValues(resolution, array);
        }

        public void Shift(Point3 delta) {
            if (!(delta != Point3.Zero)) {
                return;
            }
            int[] array = new int[m_resolution * m_resolution * m_resolution];
            for (int i = 0; i < m_resolution; i++) {
                for (int j = 0; j < m_resolution; j++) {
                    for (int k = 0; k < m_resolution; k++) {
                        int num = k + delta.X;
                        int num2 = j + delta.Y;
                        int num3 = i + delta.Z;
                        if (num >= 0
                            && num < m_resolution
                            && num2 >= 0
                            && num2 < m_resolution
                            && num3 >= 0
                            && num3 < m_resolution) {
                            array[num + num2 * m_resolution + num3 * m_resolution * m_resolution] =
                                m_values[k + j * m_resolution + i * m_resolution * m_resolution];
                        }
                    }
                }
            }
            SetValues(m_resolution, array);
        }

        public void Rotate(int axis, int steps) {
            steps %= 4;
            if (steps < 0) {
                steps += 4;
            }
            if (steps <= 0) {
                return;
            }
            int[] array = new int[m_resolution * m_resolution * m_resolution];
            for (int i = 0; i < m_resolution; i++) {
                for (int j = 0; j < m_resolution; j++) {
                    for (int k = 0; k < m_resolution; k++) {
                        Vector3 vector = RotatePoint(new Vector3(k, j, i) - new Vector3(m_resolution / 2f - 0.5f), axis, steps)
                            + new Vector3(m_resolution / 2f - 0.5f);
                        Point3 point = new((int)MathF.Round(vector.X), (int)MathF.Round(vector.Y), (int)MathF.Round(vector.Z));
                        if (point.X >= 0
                            && point.X < m_resolution
                            && point.Y >= 0
                            && point.Y < m_resolution
                            && point.Z >= 0
                            && point.Z < m_resolution) {
                            array[point.X + point.Y * m_resolution + point.Z * m_resolution * m_resolution] =
                                m_values[k + j * m_resolution + i * m_resolution * m_resolution];
                        }
                    }
                }
            }
            SetValues(m_resolution, array);
        }

        public void Mirror(int axis) {
            int[] array = new int[m_resolution * m_resolution * m_resolution];
            for (int i = 0; i < m_resolution; i++) {
                for (int j = 0; j < m_resolution; j++) {
                    for (int k = 0; k < m_resolution; k++) {
                        Vector3 vector = MirrorPoint(new Vector3(k, j, i) - new Vector3(m_resolution / 2f - 0.5f), axis)
                            + new Vector3(m_resolution / 2f - 0.5f);
                        Point3 point = new((int)MathF.Round(vector.X), (int)MathF.Round(vector.Y), (int)MathF.Round(vector.Z));
                        if (point.X >= 0
                            && point.X < m_resolution
                            && point.Y >= 0
                            && point.Y < m_resolution
                            && point.Z >= 0
                            && point.Z < m_resolution) {
                            array[point.X + point.Y * m_resolution + point.Z * m_resolution * m_resolution] =
                                m_values[k + j * m_resolution + i * m_resolution * m_resolution];
                        }
                    }
                }
            }
            SetValues(m_resolution, array);
        }

        public ValuesDictionary Save() {
            StringBuilder stringBuilder = new();
            int num = m_values[0];
            int num2 = 1;
            for (int i = 1; i < m_values.Length; i++) {
                if (m_values[i] != num) {
                    stringBuilder.Append(num2.ToString(CultureInfo.InvariantCulture));
                    stringBuilder.Append('*');
                    stringBuilder.Append(num.ToString(CultureInfo.InvariantCulture));
                    stringBuilder.Append(',');
                    num = m_values[i];
                    num2 = 1;
                }
                else {
                    num2++;
                }
            }
            stringBuilder.Append(num2.ToString(CultureInfo.InvariantCulture));
            stringBuilder.Append('*');
            stringBuilder.Append(num.ToString(CultureInfo.InvariantCulture));
            stringBuilder.Append(',');
            ValuesDictionary valuesDictionary = new();
            if (!string.IsNullOrEmpty(Name)) {
                valuesDictionary.SetValue("Name", Name);
            }
            valuesDictionary.SetValue("TerrainUseCount", m_terrainUseCount);
            valuesDictionary.SetValue("Resolution", m_resolution);
            valuesDictionary.SetValue("InteractionMode", m_interactionMode);
            if (LinkedDesign != null) {
                valuesDictionary.SetValue("LinkedDesign", LinkedDesign.Index);
            }
            valuesDictionary.SetValue("Values", stringBuilder.ToString());
            return valuesDictionary;
        }

        public bool Compare(FurnitureDesign other) {
            if (this == other) {
                return true;
            }
            if (Resolution == other.Resolution
                && InteractionMode == other.InteractionMode
                && Hash == other.Hash
                && Name == other.Name) {
                for (int i = 0; i < m_values.Length; i++) {
                    if (m_values[i] != other.m_values[i]) {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public bool CompareChain(FurnitureDesign other) {
            if (this == other) {
                return true;
            }
            List<FurnitureDesign> list = ListChain();
            List<FurnitureDesign> list2 = other.ListChain();
            if (list.Count != list2.Count) {
                return false;
            }
            for (int i = 0; i < list.Count; i++) {
                if (!list[i].Compare(list2[i])) {
                    return false;
                }
            }
            return true;
        }

        public FurnitureDesign Clone() {
            FurnitureDesign furnitureDesign = new(m_subsystemTerrain);
            furnitureDesign.SetValues(Resolution, m_values);
            furnitureDesign.Name = Name;
            furnitureDesign.LinkedDesign = LinkedDesign;
            furnitureDesign.InteractionMode = InteractionMode;
            return furnitureDesign;
        }

        public List<FurnitureDesign> CloneChain() {
            List<FurnitureDesign> list = ListChain();
            List<FurnitureDesign> list2 = new(list.Count);
            for (int i = 0; i < list.Count; i++) {
                list2.Add(list[i].Clone());
            }
            for (int j = 0; j < list2.Count - 1; j++) {
                list2[j].LinkedDesign = list2[j + 1];
            }
            int num = list.IndexOf(list[^1].LinkedDesign);
            if (num >= 0) {
                list2[^1].LinkedDesign = list2[num];
            }
            return list2;
        }

        public List<FurnitureDesign> ListChain() {
            FurnitureDesign furnitureDesign = this;
            HashSet<FurnitureDesign> hashSet = new();
            List<FurnitureDesign> list = new();
            do {
                hashSet.Add(furnitureDesign);
                list.Add(furnitureDesign);
                furnitureDesign = furnitureDesign.LinkedDesign;
            }
            while (furnitureDesign != null
                && !hashSet.Contains(furnitureDesign));
            return list;
        }

        public static List<List<FurnitureDesign>> ListChains(IEnumerable<FurnitureDesign> designs) {
            List<List<FurnitureDesign>> list = new();
            List<FurnitureDesign> list2 = new(designs);
            while (list2.Count > 0) {
                List<FurnitureDesign> list3 = list2[0].ListChain();
                list.Add(list3);
                foreach (FurnitureDesign item in list3) {
                    list2.Remove(item);
                }
            }
            return list;
        }

        public byte[] CreatePrecedingEmptySpacesArray() {
            byte[] array = new byte[m_values.Length];
            int num = 0;
            for (int i = 0; i < Resolution; i++) {
                for (int j = 0; j < Resolution; j++) {
                    int num2 = 0;
                    int num3 = 0;
                    while (num3 < Resolution) {
                        num2 = m_values[num] == 0 ? num2 + 1 : 0;
                        array[num] = (byte)num2;
                        num3++;
                        num++;
                    }
                }
            }
            return array;
        }

        public Box CalculateBox(Box box, byte[] precedingEmptySpaces) {
            int num = int.MaxValue;
            int num2 = int.MaxValue;
            int num3 = int.MaxValue;
            int num4 = int.MinValue;
            int num5 = int.MinValue;
            int num6 = int.MinValue;
            for (int i = box.Near; i < box.Far; i++) {
                int num7 = Math.Min(num3, i);
                int num8 = Math.Max(num6, i);
                int num9 = box.Top;
                int num10 = (num9 + i * Resolution) * Resolution;
                while (num9 < box.Bottom) {
                    int num11 = box.Right - 1 - precedingEmptySpaces[num10 + box.Right - 1];
                    if (num11 >= box.Left) {
                        num4 = Math.Max(num4, num11);
                        num2 = Math.Min(num2, num9);
                        num5 = Math.Max(num5, num9);
                        num3 = num7;
                        num6 = num8;
                        int num12 = num - 1;
                        for (int j = box.Left; j <= num12; j++) {
                            if (m_values[num10 + j] != 0) {
                                num = Math.Min(num, j);
                                break;
                            }
                        }
                    }
                    num9++;
                    num10 += Resolution;
                }
            }
            return new Box(num, num2, num3, num4 - num + 1, num5 - num2 + 1, num6 - num3 + 1);
        }

        public void CalculateShadowStrengthFactor() {
            float[] array = new float[Resolution * Resolution];
            int num = 0;
            for (int i = 0; i < Resolution; i++) {
                for (int j = 0; j < Resolution; j++) {
                    float x = (j + 1) / (float)Resolution;
                    for (int k = 0; k < Resolution; k++) {
                        if (!IsValueTransparent(m_values[num++])) {
                            array[k + i * Resolution] = MathUtils.Max(array[k + i * Resolution], x);
                        }
                    }
                }
            }
            float num2 = 0f;
            for (int l = 0; l < Resolution * Resolution; l++) {
                num2 += array[l];
            }
            num2 /= Resolution * Resolution;
            float num3 = 1.5f;
            m_shadowStrengthFactor = (int)Math.Clamp(MathF.Round(num2 * 3f * num3), 0f, 3f);
        }

        public void CreateGeometry() {
            m_geometry = new FurnitureGeometry();
            for (int i = 0; i < 6; i++) {
                int num = CellFace.OppositeFace(i);
                Point3 point;
                Point3 point2;
                Point3 point3;
                Point3 point4;
                Point3 point5;
                switch (i) {
                    case 0:
                        point = new Point3(0, 0, 1);
                        point2 = new Point3(-1, 0, 0);
                        point3 = new Point3(0, -1, 0);
                        point4 = new Point3(m_resolution, m_resolution, 0);
                        point5 = new Point3(m_resolution - 1, m_resolution - 1, 0);
                        break;
                    case 1:
                        point = new Point3(1, 0, 0);
                        point2 = new Point3(0, 0, 1);
                        point3 = new Point3(0, -1, 0);
                        point4 = new Point3(0, m_resolution, 0);
                        point5 = new Point3(0, m_resolution - 1, 0);
                        break;
                    case 2:
                        point = new Point3(0, 0, -1);
                        point2 = new Point3(1, 0, 0);
                        point3 = new Point3(0, -1, 0);
                        point4 = new Point3(0, m_resolution, m_resolution);
                        point5 = new Point3(0, m_resolution - 1, m_resolution - 1);
                        break;
                    case 3:
                        point = new Point3(-1, 0, 0);
                        point2 = new Point3(0, 0, -1);
                        point3 = new Point3(0, -1, 0);
                        point4 = new Point3(m_resolution, m_resolution, m_resolution);
                        point5 = new Point3(m_resolution - 1, m_resolution - 1, m_resolution - 1);
                        break;
                    case 4:
                        point = new Point3(0, 1, 0);
                        point2 = new Point3(-1, 0, 0);
                        point3 = new Point3(0, 0, 1);
                        point4 = new Point3(m_resolution, 0, 0);
                        point5 = new Point3(m_resolution - 1, 0, 0);
                        break;
                    default:
                        point = new Point3(0, -1, 0);
                        point2 = new Point3(-1, 0, 0);
                        point3 = new Point3(0, 0, -1);
                        point4 = new Point3(m_resolution, m_resolution, m_resolution);
                        point5 = new Point3(m_resolution - 1, m_resolution - 1, m_resolution - 1);
                        break;
                }
                BlockMesh blockMesh = new();
                BlockMesh blockMesh2 = new();
                BlockMesh blockMeshTransparent = new();
                for (int j = 0; j < m_resolution; j++) {
                    Cell[] array = new Cell[m_resolution * m_resolution];
                    for (int k = 0; k < m_resolution; k++) {
                        for (int l = 0; l < m_resolution; l++) {
                            int num2 = j * point.X + k * point3.X + l * point2.X + point5.X;
                            int num3 = j * point.Y + k * point3.Y + l * point2.Y + point5.Y;
                            int num4 = j * point.Z + k * point3.Z + l * point2.Z + point5.Z;
                            int num5 = num2 + num3 * m_resolution + num4 * m_resolution * m_resolution;
                            int num6 = m_values[num5];
                            Cell cell = default;
                            cell.Value = num6;
                            Cell cell2 = cell;
                            if (j > 0
                                && num6 != 0) {
                                int num7 = num2 - point.X + (num3 - point.Y) * m_resolution + (num4 - point.Z) * m_resolution * m_resolution;
                                int value = m_values[num7];
                                if (!IsValueTransparent(value)
                                    || Terrain.ExtractContents(num6) == Terrain.ExtractContents(value)) {
                                    cell2.Value = 0;
                                }
                            }
                            array[l + k * m_resolution] = cell2;
                        }
                    }
                    for (int m = 0; m < m_resolution; m++) {
                        for (int n = 0; n < m_resolution; n++) {
                            int value2 = array[n + m * m_resolution].Value;
                            if (value2 == 0) {
                                continue;
                            }
                            Point2 point6 = FindLargestSize(array, new Point2(n, m), value2);
                            if (!(point6 == Point2.Zero)) {
                                MarkUsed(array, new Point2(n, m), point6);
                                float num8 = 0.0005f * m_resolution;
                                float num9 = n - num8;
                                float num10 = n + point6.X + num8;
                                float num11 = m - num8;
                                float num12 = m + point6.Y + num8;
                                float x = j * point.X + num11 * point3.X + num9 * point2.X + point4.X;
                                float y = j * point.Y + num11 * point3.Y + num9 * point2.Y + point4.Y;
                                float z = j * point.Z + num11 * point3.Z + num9 * point2.Z + point4.Z;
                                float x2 = j * point.X + num11 * point3.X + num10 * point2.X + point4.X;
                                float y2 = j * point.Y + num11 * point3.Y + num10 * point2.Y + point4.Y;
                                float z2 = j * point.Z + num11 * point3.Z + num10 * point2.Z + point4.Z;
                                float x3 = j * point.X + num12 * point3.X + num10 * point2.X + point4.X;
                                float y3 = j * point.Y + num12 * point3.Y + num10 * point2.Y + point4.Y;
                                float z3 = j * point.Z + num12 * point3.Z + num10 * point2.Z + point4.Z;
                                float x4 = j * point.X + num12 * point3.X + num9 * point2.X + point4.X;
                                float y4 = j * point.Y + num12 * point3.Y + num9 * point2.Y + point4.Y;
                                float z4 = j * point.Z + num12 * point3.Z + num9 * point2.Z + point4.Z;
                                BlockMesh blockMesh3 = blockMesh;
                                int num13 = Terrain.ExtractContents(value2);
                                Block block = BlocksManager.Blocks[num13];
                                bool isEmissive = block.IsFurnitureEmissive(value2);
                                int faceTextureSlot = isEmissive ? 31 : block is WaterBlock ? 189 : block.GetFaceTextureSlot(num, value2);
                                Color color = block.GetFurnitureColor(value2, m_subsystemTerrain.SubsystemPalette);
                                GeometrySubsetType subsetType = block.GetFurnitureSubsetType(value2);
                                ModsManager.HookAction(
                                    "SetFurnitureDesignColor",
                                    loader => {
                                        loader.SetFurnitureDesignColor(this, block, value2, ref faceTextureSlot, ref color);
                                        return false;
                                    }
                                );
                                // 使用 GetTextureSlotCount 替代硬编码的 16 以支持自定义纹理图集
                                int slotCount = block.GetTextureSlotCount(value2);
                                // 按纹理分组：非 null 纹理路由到 Draws，null 纹理使用默认图集
                                Texture2D texture = block.GetDefaultTexture(value2);
                                if (texture != null) {
                                    m_geometry.Draws ??= new();
                                    if (!m_geometry.Draws.TryGetValue(texture, out FurnitureGeometry subGeo)) {
                                        subGeo = new FurnitureGeometry();
                                        m_geometry.Draws.Add(texture, subGeo);
                                    }
                                    switch (subsetType) {
                                        case GeometrySubsetType.AlphaTest:
                                            subGeo.SubsetAlphaTestByFace[i] ??= new BlockMesh();
                                            blockMesh3 = subGeo.SubsetAlphaTestByFace[i];
                                            break;
                                        case GeometrySubsetType.Transparent:
                                            subGeo.SubsetTransparentByFace[i] ??= new BlockMesh();
                                            blockMesh3 = subGeo.SubsetTransparentByFace[i];
                                            break;
                                        default:
                                            subGeo.SubsetOpaqueByFace[i] ??= new BlockMesh();
                                            blockMesh3 = subGeo.SubsetOpaqueByFace[i];
                                            break;
                                    }
                                }
                                else if (subsetType == GeometrySubsetType.Transparent) {
                                    blockMesh3 = blockMeshTransparent;
                                }
                                else if (subsetType == GeometrySubsetType.AlphaTest) {
                                    blockMesh3 = blockMesh2;
                                }
                                int num15 = faceTextureSlot % slotCount;
                                int num16 = faceTextureSlot / slotCount;
                                int count = blockMesh3.Vertices.Count;
                                blockMesh3.Vertices.Count += 4;
                                BlockMeshVertex[] array2 = blockMesh3.Vertices.Array;
                                float x5 = ((n + 0.01f) / m_resolution + num15) / slotCount;
                                float x6 = ((n + point6.X - 0.01f) / m_resolution + num15) / slotCount;
                                float y5 = ((m + 0.01f) / m_resolution + num16) / slotCount;
                                float y6 = ((m + point6.Y - 0.01f) / m_resolution + num16) / slotCount;
                                array2[count] = new BlockMeshVertex {
                                    Position = new Vector3(x, y, z) / m_resolution,
                                    Color = color,
                                    Face = (byte)num,
                                    TextureCoordinates = new Vector2(x5, y5),
                                    IsEmissive = isEmissive
                                };
                                array2[count + 1] = new BlockMeshVertex {
                                    Position = new Vector3(x2, y2, z2) / m_resolution,
                                    Color = color,
                                    Face = (byte)num,
                                    TextureCoordinates = new Vector2(x6, y5),
                                    IsEmissive = isEmissive
                                };
                                array2[count + 2] = new BlockMeshVertex {
                                    Position = new Vector3(x3, y3, z3) / m_resolution,
                                    Color = color,
                                    Face = (byte)num,
                                    TextureCoordinates = new Vector2(x6, y6),
                                    IsEmissive = isEmissive
                                };
                                array2[count + 3] = new BlockMeshVertex {
                                    Position = new Vector3(x4, y4, z4) / m_resolution,
                                    Color = color,
                                    Face = (byte)num,
                                    TextureCoordinates = new Vector2(x5, y6),
                                    IsEmissive = isEmissive
                                };
                                int count2 = blockMesh3.Indices.Count;
                                blockMesh3.Indices.Count += 6;
                                int[] array3 = blockMesh3.Indices.Array;
                                array3[count2] = count;
                                array3[count2 + 1] = count + 1;
                                array3[count2 + 2] = count + 2;
                                array3[count2 + 3] = count + 2;
                                array3[count2 + 4] = count + 3;
                                array3[count2 + 5] = count;
                            }
                        }
                    }
                }
                if (blockMesh.Indices.Count > 0) {
                    blockMesh.Trim();
                    blockMesh.GenerateSidesData();
                    m_geometry.SubsetOpaqueByFace[i] = blockMesh;
                }
                if (blockMesh2.Indices.Count > 0) {
                    blockMesh2.Trim();
                    blockMesh2.GenerateSidesData();
                    m_geometry.SubsetAlphaTestByFace[i] = blockMesh2;
                }
                if (blockMeshTransparent.Indices.Count > 0) {
                    blockMeshTransparent.Trim();
                    blockMeshTransparent.GenerateSidesData();
                    m_geometry.SubsetTransparentByFace[i] = blockMeshTransparent;
                }
                if (m_geometry.Draws != null) {
                    foreach (var kv in m_geometry.Draws) {
                        BlockMesh opaqueMesh = kv.Value.SubsetOpaqueByFace[i];
                        if (opaqueMesh != null && opaqueMesh.Indices.Count > 0) {
                            opaqueMesh.Trim();
                            opaqueMesh.GenerateSidesData();
                        }
                        BlockMesh alphaMesh = kv.Value.SubsetAlphaTestByFace[i];
                        if (alphaMesh != null && alphaMesh.Indices.Count > 0) {
                            alphaMesh.Trim();
                            alphaMesh.GenerateSidesData();
                        }
                        BlockMesh transparentMesh = kv.Value.SubsetTransparentByFace[i];
                        if (transparentMesh != null && transparentMesh.Indices.Count > 0) {
                            transparentMesh.Trim();
                            transparentMesh.GenerateSidesData();
                        }
                    }
                }
            }
        }

        public void CreateCollisionAndInteractionBoxes() {
            Subdivision subdivision = CreateBoundingBoxesHelper(Box, 0, CreatePrecedingEmptySpacesArray());
            List<BoundingBox> list = new(subdivision.Boxes.Count);
            for (int i = 0; i < subdivision.Boxes.Count; i++) {
                Box box = subdivision.Boxes[i];
                Vector3 min = new Vector3(box.Left, box.Top, box.Near) / Resolution;
                Vector3 max = new Vector3(box.Right, box.Bottom, box.Far) / Resolution;
                list.Add(new BoundingBox(min, max));
            }
            m_collisionBoxesByRotation = new BoundingBox[4][];
            for (int j = 0; j < 4; j++) {
                Matrix m = Matrix.CreateTranslation(-0.5f, 0f, -0.5f)
                    * Matrix.CreateRotationY(j * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                m_collisionBoxesByRotation[j] = new BoundingBox[list.Count];
                for (int k = 0; k < list.Count; k++) {
                    Vector3 v = Vector3.Transform(list[k].Min, m);
                    Vector3 v2 = Vector3.Transform(list[k].Max, m);
                    BoundingBox boundingBox = new(Vector3.Min(v, v2), Vector3.Max(v, v2));
                    m_collisionBoxesByRotation[j][k] = boundingBox;
                }
            }
            List<BoundingBox> list2 = new(list);
            while (true) {
                int num = 0;
                int l;
                BoundingBox item;
                while (true) {
                    if (num < list2.Count) {
                        for (l = 0; l < list2.Count; l++) {
                            if (num != l) {
                                BoundingBox b = list2[num];
                                BoundingBox b2 = list2[l];
                                item = BoundingBox.Union(b, b2);
                                Vector3 vector = item.Size();
                                if ((item.Volume() - b.Volume() - b2.Volume()) / MathUtils.Min(vector.X, vector.Y, vector.Z) < 0.4f) {
                                    goto end_IL_0263;
                                }
                            }
                        }
                        num++;
                        continue;
                    }
                    bool flag = false;
                    for (int n = 0; n < list2.Count; n++) {
                        Vector3 vector2 = list2[n].Size();
                        flag |= vector2.X >= 0.6f && vector2.Y >= 0.6f;
                        flag |= vector2.X >= 0.6f && vector2.Z >= 0.6f;
                        flag |= vector2.Y >= 0.6f && vector2.Z >= 0.6f;
                    }
                    float minSize = flag ? 0.0625f : 0.6f;
                    for (int num2 = 0; num2 < list2.Count; num2++) {
                        BoundingBox value = list2[num2];
                        EnsureMinSize(ref value.Min.X, ref value.Max.X, minSize);
                        EnsureMinSize(ref value.Min.Y, ref value.Max.Y, minSize);
                        EnsureMinSize(ref value.Min.Z, ref value.Max.Z, minSize);
                        list2[num2] = value;
                    }
                    m_interactionBoxesByRotation = new BoundingBox[4][];
                    for (int num3 = 0; num3 < 4; num3++) {
                        Matrix m2 = Matrix.CreateTranslation(-0.5f, 0f, -0.5f)
                            * Matrix.CreateRotationY(num3 * (float)Math.PI / 2f)
                            * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                        m_interactionBoxesByRotation[num3] = new BoundingBox[list2.Count];
                        for (int num4 = 0; num4 < list2.Count; num4++) {
                            Vector3 v3 = Vector3.Transform(list2[num4].Min, m2);
                            Vector3 v4 = Vector3.Transform(list2[num4].Max, m2);
                            BoundingBox boundingBox2 = new(Vector3.Min(v3, v4), Vector3.Max(v3, v4));
                            m_interactionBoxesByRotation[num3][num4] = boundingBox2;
                        }
                    }
                    return;
                    end_IL_0263:
                    break;
                }
                list2.RemoveAt(num);
                list2.RemoveAt(num < l ? l - 1 : l);
                list2.Add(item);
            }
        }

        public void CreateTorchPoints() {
            List<BoundingBox> list = new();
            for (int i = 0; i < Resolution; i++) {
                for (int j = 0; j < Resolution; j++) {
                    for (int k = 0; k < Resolution; k++) {
                        int num = Terrain.ExtractContents(m_values[k + j * Resolution + i * Resolution * Resolution]);
                        if (num != 31
                            && num != 17) {
                            continue;
                        }
                        BoundingBox boundingBox = new(new Vector3(k, j, i) / Resolution, new Vector3(k + 1, j + 1, i + 1) / Resolution);
                        int num2 = -1;
                        for (int l = 0; l < list.Count; l++) {
                            BoundingBox boundingBox2 = list[l];
                            Vector3 vector = boundingBox2.Size();
                            Vector3 vector2 = boundingBox2.Center() - boundingBox.Center();
                            vector2.X = MathUtils.Max(MathF.Abs(vector2.X) - vector.X / 2f, 0f);
                            vector2.Y = MathUtils.Max(MathF.Abs(vector2.Y) - vector.Y / 2f, 0f);
                            vector2.Z = MathUtils.Max(MathF.Abs(vector2.Z) - vector.Z / 2f, 0f);
                            if (vector2.Length() < 0.15f) {
                                num2 = l;
                                break;
                            }
                        }
                        if (num2 >= 0) {
                            list[num2] = BoundingBox.Union(list[num2], boundingBox);
                        }
                        else if (list.Count < 4) {
                            list.Add(boundingBox);
                        }
                    }
                }
            }
            m_torchPointsByRotation = new BoundingBox[4][];
            for (int m = 0; m < 4; m++) {
                Matrix m2 = Matrix.CreateTranslation(-0.5f, 0f, -0.5f)
                    * Matrix.CreateRotationY(m * (float)Math.PI / 2f)
                    * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
                m_torchPointsByRotation[m] = new BoundingBox[list.Count];
                for (int n = 0; n < list.Count; n++) {
                    Vector3 v = Vector3.Transform(list[n].Min, m2);
                    Vector3 v2 = Vector3.Transform(list[n].Max, m2);
                    m_torchPointsByRotation[m][n] = new BoundingBox(Vector3.Min(v, v2), Vector3.Max(v, v2));
                }
            }
        }

        public void CalculateMainValue() {
            Dictionary<int, int> dictionary = new();
            for (int i = 0; i < Resolution; i++) {
                for (int j = 0; j < Resolution; j++) {
                    for (int num = Resolution - 1; num >= 0; num--) {
                        int num2 = m_values[j + num * Resolution + i * Resolution * Resolution];
                        if (num2 != 0) {
                            dictionary.TryGetValue(num2, out int value);
                            dictionary[num2] = value + 1;
                            break;
                        }
                    }
                }
            }
            int num3 = 0;
            foreach (KeyValuePair<int, int> item in dictionary) {
                if (item.Value > num3) {
                    m_mainValue = item.Key;
                    num3 = item.Value;
                }
            }
        }

        public void CalculateFacesMasks() {
            m_mountingFacesMask = 0;
            m_transparentFacesMask = 0;
            for (int i = 0; i < Resolution; i++) {
                for (int j = 0; j < Resolution; j++) {
                    int[] values = m_values;
                    int num = i + j * Resolution;
                    _ = Resolution;
                    int value = values[num + 0 * Resolution];
                    int value2 = m_values[i + j * Resolution + (Resolution - 1) * Resolution * Resolution];
                    if (IsValueTransparent(value)) {
                        m_transparentFacesMask |= 4;
                    }
                    else {
                        m_mountingFacesMask |= 4;
                    }
                    if (IsValueTransparent(value2)) {
                        m_transparentFacesMask |= 1;
                    }
                    else {
                        m_mountingFacesMask |= 1;
                    }
                }
            }
            for (int k = 0; k < Resolution; k++) {
                for (int l = 0; l < Resolution; l++) {
                    int value3 = m_values[k * Resolution + l * Resolution * Resolution];
                    int value4 = m_values[Resolution - 1 + k * Resolution + l * Resolution * Resolution];
                    if (IsValueTransparent(value3)) {
                        m_transparentFacesMask |= 8;
                    }
                    else {
                        m_mountingFacesMask |= 8;
                    }
                    if (IsValueTransparent(value4)) {
                        m_transparentFacesMask |= 2;
                    }
                    else {
                        m_mountingFacesMask |= 2;
                    }
                }
            }
            for (int m = 0; m < Resolution; m++) {
                for (int n = 0; n < Resolution; n++) {
                    int[] values2 = m_values;
                    int num2 = m;
                    _ = Resolution;
                    int value5 = values2[num2 + 0 + n * Resolution * Resolution];
                    int value6 = m_values[m + (Resolution - 1) * Resolution + n * Resolution * Resolution];
                    if (IsValueTransparent(value5)) {
                        m_transparentFacesMask |= 32;
                    }
                    else {
                        m_mountingFacesMask |= 32;
                    }
                    if (IsValueTransparent(value6)) {
                        m_transparentFacesMask |= 16;
                    }
                    else {
                        m_mountingFacesMask |= 16;
                    }
                }
            }
        }

        public Subdivision CreateBoundingBoxesHelper(Box box, int depth, byte[] precedingEmptySpaces) {
            int num = 0;
            Subdivision result = default;
            result.TotalVolume = box.Width * box.Height * box.Depth;
            result.MinVolume = result.TotalVolume;
            result.Boxes = [box];
            if (depth < 2) {
                for (int num2 = box.Bottom - 1; num2 >= box.Top + 1; num2--) {
                    Box box2 = CalculateBox(new Box(box.Left, box.Top, box.Near, box.Width, num2 - box.Top, box.Depth), precedingEmptySpaces);
                    Box box3 = CalculateBox(new Box(box.Left, num2, box.Near, box.Width, box.Bottom - num2, box.Depth), precedingEmptySpaces);
                    Subdivision subdivision = CreateBoundingBoxesHelper(box2, depth + 1, precedingEmptySpaces);
                    Subdivision subdivision2 = CreateBoundingBoxesHelper(box3, depth + 1, precedingEmptySpaces);
                    int num3 = subdivision.Boxes.Count + subdivision2.Boxes.Count;
                    int num4 = subdivision.TotalVolume + subdivision2.TotalVolume;
                    int num5 = MathUtils.Min(subdivision.MinVolume, subdivision2.MinVolume);
                    int num6 = num3 > result.Boxes.Count ? num4 + num : num4;
                    if (num6 < result.TotalVolume
                        || (num6 == result.TotalVolume && num5 > result.MinVolume)) {
                        result.TotalVolume = num4;
                        result.MinVolume = num5;
                        result.Boxes = subdivision.Boxes;
                        result.Boxes.AddRange(subdivision2.Boxes);
                    }
                }
                for (int i = box.Near + 1; i < box.Far; i++) {
                    Box box4 = CalculateBox(new Box(box.Left, box.Top, box.Near, box.Width, box.Height, i - box.Near), precedingEmptySpaces);
                    Box box5 = CalculateBox(new Box(box.Left, box.Top, i, box.Width, box.Height, box.Far - i), precedingEmptySpaces);
                    Subdivision subdivision3 = CreateBoundingBoxesHelper(box4, depth + 1, precedingEmptySpaces);
                    Subdivision subdivision4 = CreateBoundingBoxesHelper(box5, depth + 1, precedingEmptySpaces);
                    int num7 = subdivision3.Boxes.Count + subdivision4.Boxes.Count;
                    int num8 = subdivision3.TotalVolume + subdivision4.TotalVolume;
                    int num9 = MathUtils.Min(subdivision3.MinVolume, subdivision4.MinVolume);
                    int num10 = num7 > result.Boxes.Count ? num8 + num : num8;
                    if (num10 < result.TotalVolume
                        || (num10 == result.TotalVolume && num9 > result.MinVolume)) {
                        result.TotalVolume = num8;
                        result.MinVolume = num9;
                        result.Boxes = subdivision3.Boxes;
                        result.Boxes.AddRange(subdivision4.Boxes);
                    }
                }
                for (int j = box.Left + 1; j < box.Right; j++) {
                    Box box6 = CalculateBox(new Box(box.Left, box.Top, box.Near, j - box.Left, box.Height, box.Depth), precedingEmptySpaces);
                    Box box7 = CalculateBox(new Box(j, box.Top, box.Near, box.Right - j, box.Height, box.Depth), precedingEmptySpaces);
                    Subdivision subdivision5 = CreateBoundingBoxesHelper(box6, depth + 1, precedingEmptySpaces);
                    Subdivision subdivision6 = CreateBoundingBoxesHelper(box7, depth + 1, precedingEmptySpaces);
                    int num11 = subdivision5.Boxes.Count + subdivision6.Boxes.Count;
                    int num12 = subdivision5.TotalVolume + subdivision6.TotalVolume;
                    int num13 = MathUtils.Min(subdivision5.MinVolume, subdivision6.MinVolume);
                    int num14 = num11 > result.Boxes.Count ? num12 + num : num12;
                    if (num14 < result.TotalVolume
                        || (num14 == result.TotalVolume && num13 > result.MinVolume)) {
                        result.TotalVolume = num12;
                        result.MinVolume = num13;
                        result.Boxes = subdivision5.Boxes;
                        result.Boxes.AddRange(subdivision6.Boxes);
                    }
                }
            }
            return result;
        }

        public Point2 FindLargestSize(Cell[] surface, Point2 start, int value) {
            Point2 result = Point2.Zero;
            int num = m_resolution;
            for (int i = start.Y; i < m_resolution; i++) {
                for (int j = start.X; j <= num; j++) {
                    if (j == num
                        || surface[j + i * m_resolution].Value != value) {
                        num = j;
                        Point2 point = new(num - start.X, i - start.Y + 1);
                        if (point.X * point.Y > result.X * result.Y) {
                            result = point;
                        }
                    }
                }
            }
            return result;
        }

        public void MarkUsed(Cell[] surface, Point2 start, Point2 size) {
            for (int i = start.Y; i < start.Y + size.Y; i++) {
                for (int j = start.X; j < start.X + size.X; j++) {
                    surface[j + i * m_resolution].Value = 0;
                }
            }
        }

        public static Vector3 RotatePoint(Vector3 p, int axis, int steps) {
            for (int i = 0; i < steps; i++) {
                switch (axis) {
                    case 0: p = new Vector3(p.X, p.Z, 0f - p.Y); break;
                    case 1: p = new Vector3(0f - p.Z, p.Y, p.X); break;
                    default: p = new Vector3(0f - p.Y, p.X, p.Z); break;
                }
            }
            return p;
        }

        public static Vector3 MirrorPoint(Vector3 p, int axis) {
            switch (axis) {
                case 0: p = new Vector3(p.X, p.Y, 0f - p.Z); break;
                case 1: p = new Vector3(0f - p.X, p.Y, p.Z); break;
                default: p = new Vector3(0f - p.X, p.Y, p.Z); break;
            }
            return p;
        }

        public static void EnsureMinSize(ref float min, ref float max, float minSize) {
            float num = max - min;
            if (num < minSize) {
                float num2 = minSize - num;
                min -= num2 / 2f;
                max += num2 / 2f;
                if (min < 0f) {
                    max -= min;
                    min = 0f;
                }
                else if (max > 1f) {
                    min -= max - 1f;
                    max = 1f;
                }
            }
        }

        public static bool IsValueTransparent(int value) {
            if (value != 0) {
                return Terrain.ExtractContents(value) == 15;
            }
            return true;
        }
    }
}