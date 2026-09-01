using System.Reflection;
using Engine.Graphics;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Media {
    public class BitmapFont : IDisposable {
        public class Counter {
            short[] m_counts = new short[32];

            public int MaxUsedIndex { get; private set; } = -1;

            public void Increment(int i) {
                while (i >= m_counts.Length) {
                    short[] counts = m_counts;
                    m_counts = new short[m_counts.Length * 2];
                    Array.Copy(counts, m_counts, MaxUsedIndex + 1);
                }
                m_counts[i]++;
                MaxUsedIndex = MathUtils.Max(i, MaxUsedIndex);
            }

            public int Get(int i) => m_counts[i];

            public void Clear() {
                Array.Clear(m_counts, 0, MaxUsedIndex + 1);
                MaxUsedIndex = -1;
            }
        }

        public class Glyph(char code, Vector2 texCoord1, Vector2 texCoord2, Vector2 offset, float width) {
            public readonly char Code = code;

            public readonly bool IsBlank = texCoord1 == texCoord2;

            public readonly Vector2 TexCoord1 = texCoord1;

            public readonly Vector2 TexCoord2 = texCoord2;

            public readonly Vector2 Offset = offset;

            public readonly float Width = width;
        }

        public class KerningSettings {
            public int Limit = 5;

            public int Tolerance = 1;

            public int BulkingRadius = 1;

            public float BulkingGradient = 1f;
        }

        public static BitmapFont m_debugFont;

        public Glyph[] m_glyphsByCode;

        public Image m_image;

        public Dictionary<int, short> m_kerningPairs;

        public Texture2D Texture { get; set; }

        public float GlyphHeight { get; set; }

        public float LineHeight { get; set; }

        public Vector2 Spacing { get; set; }

        public float Scale { get; set; }

        public Glyph FallbackGlyph { get; set; }

        public static Glyph EmptyGlyph = new((char)0, Vector2.Zero, Vector2.Zero, Vector2.Zero, 0f);

        public char MaxGlyphCode { get; set; }

        public static BitmapFont DebugFont {
            get {
                if (m_debugFont == null) {
                    using Stream stream = typeof(BitmapFont).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.Debugfont.png");
                    using Stream stream2 = typeof(BitmapFont).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.Debugfont.lst");
                    if (stream == null) {
                        throw new FileNotFoundException("Debugfont.png");
                    }
                    if (stream2 == null) {
                        throw new FileNotFoundException("Debugfont.lst");
                    }
                    m_debugFont = Initialize(stream, stream2);
                }
                return m_debugFont;
            }
        }

        /// <summary>
        ///     纹理图
        /// </summary>
        /// <param name="TextureStream">图片文件的输入流</param>
        /// <param name="GlyphsStream">位图数据的输入流</param>
        /// <param name="customGlyphOffset">自定义位图偏移</param>
        public static BitmapFont Initialize(Stream TextureStream, Stream GlyphsStream, Vector2? customGlyphOffset = null) =>
            Initialize(Texture2D.Load(TextureStream), GlyphsStream, customGlyphOffset);

        public static BitmapFont Initialize(Texture2D texture, Stream GlyphsStream, Vector2? customGlyphOffset = null) {
            try {
                char[] splitters = [(char)0x20, (char)0x09]; // 空格和制表符
                BitmapFont bitmapFont = new();
                StreamReader streamReader = new(GlyphsStream);
                string firstLine = streamReader.ReadLine();
                if (firstLine == null) {
                    throw new FormatException("The first line of the .lst file of the bitmap font can not be founded");
                }
                int num = int.Parse(firstLine);
                Glyph[] array = new Glyph[num];
                for (int i = 0; i < num; i++) {
                    string line = streamReader.ReadLine();
                    if (line == null) {
                        throw new FormatException($"The {i + 2} line of the .lst file of the bitmap font can not be founded");
                    }
                    string[] arr = line.Split(splitters, StringSplitOptions.None);
                    if (arr.Length == 9) {
                        string[] tmp = new string[8];
                        tmp[0] = " ";
                        for (int j = 2; j < arr.Length; j++) {
                            tmp[j - 1] = arr[j];
                        }
                        arr = tmp;
                    }
                    char code = char.Parse(arr[0]);
                    Vector2 texCoord = new(float.Parse(arr[1]), float.Parse(arr[2]));
                    Vector2 texCoord2 = new(float.Parse(arr[3]), float.Parse(arr[4]));
                    Vector2 offset = new(float.Parse(arr[5]), float.Parse(arr[6]));
                    if (customGlyphOffset.HasValue) {
                        offset += customGlyphOffset.Value;
                    }
                    float width = float.Parse(arr[7]);
                    array[i] = new Glyph(code, texCoord, texCoord2, offset, width);
                }
                string glyphHeightLine = streamReader.ReadLine();
                if (glyphHeightLine == null) {
                    throw new FormatException("The height line of the .lst file of the bitmap font can not be founded");
                }
                float glyphHeight = float.Parse(glyphHeightLine);
                string spacingLine = streamReader.ReadLine();
                if (spacingLine == null) {
                    throw new FormatException("The spacing line of the .lst file of the bitmap font can not be founded");
                }
                string[] arr2 = spacingLine.Split(splitters, StringSplitOptions.None);
                Vector2 spacing = new(float.Parse(arr2[0]), float.Parse(arr2[1]));
                string scaleLine = streamReader.ReadLine();
                if (scaleLine == null) {
                    throw new FormatException("The scale line of the .lst file of the bitmap font can not be founded");
                }
                float scale = float.Parse(scaleLine);
                string fallbackLine = streamReader.ReadLine();
                if (fallbackLine == null) {
                    throw new FormatException("The fallback line of the .lst file of the bitmap font can not be founded");
                }
                char fallbackCode = char.Parse(fallbackLine);
                string kerningCountLine = streamReader.ReadLine();
                if (kerningCountLine != null) {
                    int kerningCount = int.Parse(kerningCountLine);
                    for (int j = 0; j < kerningCount; j++) {
                        string line = streamReader.ReadLine();
                        if (line == null) {
                            throw new FormatException($"The {j + 7} line of the .lst file of the bitmap font can not be founded");
                        }
                        string[] arr = line.Split(splitters, StringSplitOptions.None);
                        if (arr.Length == 3) {
                            char code2 = char.Parse(arr[0]);
                            char followingCode = char.Parse(arr[1]);
                            float num3 = float.Parse(arr[2]);
                            bitmapFont.SetKerning(code2, followingCode, num3);
                        }
                    }
                }
                bitmapFont.Initialize(
                    texture,
                    null,
                    array,
                    fallbackCode,
                    glyphHeight,
                    spacing,
                    scale
                );
                return bitmapFont;
            }
            catch (Exception e) {
                Log.Error(e.Message);
                return null;
            }
        }

        public BitmapFont(Texture2D texture, IEnumerable<Glyph> glyphs, char fallbackCode, float glyphHeight, Vector2 spacing, float scale) {
            Initialize(
                texture,
                null,
                glyphs,
                fallbackCode,
                glyphHeight,
                spacing,
                scale
            );
        }

        public void Dispose() {
            if (Texture != null) {
                Texture.Dispose();
                Texture = null;
            }
        }

        public Glyph GetGlyph(char code) => code == 0 ? EmptyGlyph :
            code >= m_glyphsByCode.Length ? FallbackGlyph : m_glyphsByCode[code];

        public float GetKerning(char code, char followingCode) {
            short value = 0;
            if (m_kerningPairs != null) {
                m_kerningPairs.TryGetValue((int)(((uint)code << 16) | followingCode), out value);
            }
            return value;
        }

        public Vector2 MeasureText(string text, Vector2 scale, Vector2 spacing) => MeasureText(text, 0, text.Length, scale, spacing);

        public Vector2 MeasureText(string text, int start, int count, Vector2 scale, Vector2 spacing) {
            if (text == null) {
                return Vector2.Zero;
            }
            start = MathUtils.Clamp(start, 0, text.Length - 1);
            count = MathUtils.Clamp(count, 0, text.Length - start);
            scale *= Scale;
            spacing += Spacing;
            float lineHeight = GlyphHeight + spacing.Y;
            Vector2 vector = new(0f, lineHeight);
            Vector2 vector2 = vector;
            int end = start + count - 1;
            for (int i = start; i <= end; i++) {
                char c = text[i];
                if (c == '\n') {
                    vector.X = 0f;
                    vector.Y += lineHeight;
                    if (vector.Y > vector2.Y) {
                        vector2.Y = vector.Y;
                    }
                }
                else if (c != '\r'
                    && c != '\u200b') {
                    if (c == '\u00a0') {
                        c = ' ';
                    }
                    Glyph glyph = GetGlyph(c);
                    float num3 = i < end ? GetKerning(c, text[i + 1]) : 0f;
                    vector.X += glyph.Width - num3 + spacing.X;
                    if (vector.X > vector2.X) {
                        vector2.X = vector.X;
                    }
                }
            }
            return vector2 * scale;
        }

        public int FitText(float width, string text, float scale, float spacing) => FitText(width, text, 0, text.Length, scale, spacing);

        public int FitText(float width, string text, int start, int length, float scale, float spacing) {
            scale *= Scale;
            spacing += Spacing.X;
            float num = 0f;
            for (int i = start; i < start + length; i++) {
                char c = text[i];
                if (c == '\n') {
                    num = 0f;
                }
                else if (c != '\r'
                    && c != '\u200b') {
                    if (c == '\u00a0') {
                        c = ' ';
                    }
                    Glyph glyph = GetGlyph(c);
                    float num2 = i < text.Length - 1 ? GetKerning(c, text[i + 1]) : 0f;
                    num += (glyph.Width - num2 + spacing) * scale;
                    if (num > width) {
                        return i - start;
                    }
                }
            }
            return length;
        }

        public float CalculateCharacterPosition(string text, int characterIndex, Vector2 scale, Vector2 spacing) {
            characterIndex = Math.Clamp(characterIndex, 0, text.Length);
            return MeasureText(text, 0, characterIndex, scale, spacing).X;
        }

        public static BitmapFont Load(Image image,
            char firstCode,
            char fallbackCode,
            Vector2 spacing,
            float scale,
            Vector2 offset,
            KerningSettings kerningSettings = null,
            int mipLevelsCount = 1,
            bool premultiplyAlpha = true) => InternalLoad(
            image,
            firstCode,
            fallbackCode,
            spacing,
            scale,
            offset,
            kerningSettings,
            mipLevelsCount,
            premultiplyAlpha,
            true
        );

        public static BitmapFont Load(Stream stream,
            char firstCode,
            char fallbackCode,
            Vector2 spacing,
            float scale,
            Vector2 offset,
            KerningSettings kerningSettings = null,
            int mipLevelsCount = 1,
            bool premultiplyAlpha = true) => Load(
            Image.Load(stream),
            firstCode,
            fallbackCode,
            spacing,
            scale,
            offset,
            kerningSettings,
            mipLevelsCount,
            premultiplyAlpha
        );

        public static BitmapFont Load(string fileName,
            char firstCode,
            char fallbackCode,
            Vector2 spacing,
            float scale,
            Vector2 offset,
            KerningSettings kerningSettings = null,
            int mipLevelsCount = 1,
            bool premultiplyAlpha = true) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(
                    stream,
                    firstCode,
                    fallbackCode,
                    spacing,
                    scale,
                    offset,
                    kerningSettings,
                    mipLevelsCount,
                    premultiplyAlpha
                );
            }
        }

        static BitmapFont() {
            Display.DeviceReset += delegate {
                if (m_debugFont != null) {
                    using Stream stream = typeof(BitmapFont).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.Debugfont.png");
                    using Stream stream2 = typeof(BitmapFont).GetTypeInfo().Assembly.GetManifestResourceStream("Engine.Resources.Debugfont.lst");
                    m_debugFont = Initialize(stream, stream2);
                }
            };
        }

        internal BitmapFont() { }

        internal static BitmapFont InternalLoad(Image image,
            char firstCode,
            char fallbackCode,
            Vector2 spacing,
            float scale,
            Vector2 offset,
            KerningSettings kerningSettings,
            int mipLevelsCount,
            bool premultiplyAlpha,
            bool createTexture) {
            List<Rectangle> list = new(FindGlyphs(image));
            List<Rectangle> list2 = new(list.Select(r => CropGlyph(image, r)));
            if (list.Count == 0) {
                throw new InvalidOperationException("No glyphs found in BitmapFont image.");
            }
            int num = int.MaxValue;
            int num2 = int.MaxValue;
            int num3 = int.MaxValue;
            int num4 = int.MaxValue;
            for (int i = 0; i < list2.Count; i++) {
                if (list2[i].Width > 0
                    && list2[i].Height > 0) {
                    num = Math.Min(num, list2[i].Left - list[i].Left);
                    num2 = Math.Min(num2, list2[i].Top - list[i].Top);
                    num3 = Math.Min(num3, list[i].Right - list2[i].Right);
                    num4 = Math.Min(num4, list[i].Bottom - list2[i].Bottom);
                }
            }
            int num5 = firstCode;
            float num6 = 0f;
            List<Glyph> list3 = [];
            for (int j = 0; j < list2.Count; j++) {
                Vector2 texCoord;
                Vector2 texCoord2;
                Vector2 offset2;
                if (list2[j].Width > 0
                    && list2[j].Height > 0) {
                    texCoord = new Vector2((list2[j].Left - 0.5f) / image.Width, (list2[j].Top - 0.5f) / image.Height);
                    texCoord2 = new Vector2((list2[j].Right + 0.5f) / image.Width, (list2[j].Bottom + 0.5f) / image.Height);
                    offset2 = new Vector2(list2[j].Left - list[j].Left - num - 0.5f, list2[j].Top - list[j].Top - num2 - 0.5f);
                }
                else {
                    texCoord = Vector2.Zero;
                    texCoord2 = Vector2.Zero;
                    offset2 = Vector2.Zero;
                }
                offset2 += offset;
                float width = list[j].Width - num - num3;
                num6 = Math.Max(num6, list[j].Height - num2 - num4);
                list3.Add(new Glyph((char)num5, texCoord, texCoord2, offset2, width));
                num5++;
            }
            Image image2 = new(image.Width, image.Height);
            image.m_trueImage.ProcessPixelRows(
                image2.m_trueImage,
                (sourceAccessor, targetAccessor) => {
                    for (int i = 0; i < sourceAccessor.Height; i++) {
                        Span<Rgba32> sourceRow = sourceAccessor.GetRowSpan(i);
                        Span<Rgba32> targetRow = targetAccessor.GetRowSpan(i);
                        for (int x = 0; x < sourceRow.Length; x++) {
                            Rgba32 sourcePixel = sourceRow[x];
                            targetRow[x] = sourcePixel.IsMagenta() ? SixLabors.ImageSharp.Color.Transparent :
                                premultiplyAlpha ? sourcePixel.PremultiplyAlpha() : sourcePixel;
                        }
                    }
                }
            );
            Texture2D texture = createTexture ? Texture2D.Load(image2, mipLevelsCount) : null;
            Image image3 = createTexture ? null : image2;
            BitmapFont bitmapFont = new();
            bitmapFont.Initialize(
                texture,
                image3,
                list3,
                fallbackCode,
                num6,
                spacing,
                scale
            );
            if (kerningSettings != null) {
                int[][] array = new int[list.Count][];
                int[][] array2 = new int[list.Count][];
                for (int l = 0; l < list.Count; l++) {
                    CalculateKerningDepths(image, list2[l], out array[l], out array2[l]);
                    array[l] = ApplyKerningBulking(array[l], kerningSettings.BulkingRadius, kerningSettings.BulkingGradient);
                    array2[l] = ApplyKerningBulking(array2[l], kerningSettings.BulkingRadius, kerningSettings.BulkingGradient);
                }
                Counter counter = new();
                for (int m = 0; m < list.Count; m++) {
                    for (int n = 0; n < list.Count; n++) {
                        int num7 = list2[m].Top - list[m].Top;
                        int x = list2[m].Bottom - list[m].Top;
                        int num8 = list2[n].Top - list[n].Top;
                        int x2 = list2[n].Bottom - list[n].Top;
                        int num9 = MathUtils.Max(num7, num8);
                        int num10 = MathUtils.Min(x, x2);
                        counter.Clear();
                        for (int num11 = num9; num11 < num10; num11++) {
                            int num12 = num11 - num7;
                            int num13 = num11 - num8;
                            int num14 = array2[m][num12];
                            int num15 = array[n][num13];
                            counter.Increment(num15 + num14);
                        }
                        int num16 = Math.Min(kerningSettings.Limit - 1, counter.MaxUsedIndex);
                        int tolerance = kerningSettings.Tolerance;
                        int num17 = 0;
                        int num18;
                        for (num18 = 0; num18 <= num16; num18++) {
                            num17 += counter.Get(num18);
                            if (num17 > tolerance) {
                                break;
                            }
                        }
                        if (num18 != 0) {
                            bitmapFont.SetKerning((char)(m + firstCode), (char)(n + firstCode), num18);
                        }
                    }
                }
            }
            return bitmapFont;
        }

        internal void Initialize(Texture2D texture,
            Image image,
            IEnumerable<Glyph> glyphs,
            char fallbackCode,
            float glyphHeight,
            Vector2 spacing,
            float scale) {
            Dispose();
            Texture = texture;
            m_image = image;
            GlyphHeight = glyphHeight;
            LineHeight = glyphHeight + spacing.Y;
            Spacing = spacing;
            Scale = scale;
            IEnumerable<Glyph> enumerable = glyphs as Glyph[] ?? glyphs.ToArray();
            foreach (Glyph glyph in enumerable) {
                if (glyph.Code == fallbackCode) {
                    FallbackGlyph = glyph;
                }
                if (glyph.Code > MaxGlyphCode) {
                    MaxGlyphCode = glyph.Code;
                }
            }
            FallbackGlyph ??= enumerable.First(g => g.Code == 0);
            m_glyphsByCode = new Glyph[MaxGlyphCode + 1];
            for (int i = 0; i < m_glyphsByCode.Length; i++) {
                m_glyphsByCode[i] = FallbackGlyph;
            }
            foreach (Glyph glyph in enumerable) {
                m_glyphsByCode[glyph.Code] = glyph;
            }
        }

        public static IEnumerable<Rectangle> FindGlyphs(Image image) {
            int y = 1;
            while (y < image.Height) {
                int num;
                for (int x = 1; x < image.Width; x = num) {
                    if (!image.GetPixelFast(x, y).IsMagenta()
                        && image.GetPixelFast(x - 1, y).IsMagenta()
                        && image.GetPixelFast(x, y - 1).IsMagenta()) {
                        int i = 1;
                        int j = 1;
                        for (; x + i < image.Width && !image.GetPixelFast(x + i, y).IsMagenta(); i++) { }
                        for (; y + j < image.Height && !image.GetPixelFast(x, y + j).IsMagenta(); j++) { }
                        yield return new Rectangle(x, y, i, j);
                    }
                    num = x + 1;
                }
                num = y + 1;
                y = num;
            }
        }

        public static int[] ApplyKerningBulking(int[] depths, int radius, float gradient) {
            int[] array = new int[depths.Length];
            for (int i = 0; i < depths.Length; i++) {
                array[i] = depths[i];
                int num = MathUtils.Max(i - radius, 0);
                int num2 = MathUtils.Min(i + radius, depths.Length - 1);
                for (int j = num; j <= num2; j++) {
                    int num3 = Math.Abs(j - i);
                    int x = depths[j] + (int)Math.Round(gradient * num3);
                    array[i] = MathUtils.Min(array[i], x);
                }
            }
            return array;
        }

        public static void CalculateKerningDepths(Image image, Rectangle rectangle, out int[] leftDepths, out int[] rightDepths) {
            leftDepths = new int[rectangle.Height];
            rightDepths = new int[rectangle.Height];
            for (int i = rectangle.Top; i < rectangle.Bottom; i++) {
                int num = i - rectangle.Top;
                leftDepths[num] = rectangle.Width;
                rightDepths[num] = rectangle.Width;
                for (int j = rectangle.Left; j < rectangle.Right; j++) {
                    if (image.GetPixel(j, i).A != 0) {
                        leftDepths[num] = MathUtils.Min(leftDepths[num], j - rectangle.Left);
                        rightDepths[num] = MathUtils.Min(rightDepths[num], rectangle.Right - j - 1);
                    }
                }
            }
        }

        public BitmapFont Clone(float scale, Vector2 spacing) => new() {
            m_glyphsByCode = m_glyphsByCode,
            m_kerningPairs = m_kerningPairs,
            m_image = m_image,
            Texture = Texture,
            GlyphHeight = GlyphHeight,
            LineHeight = LineHeight,
            Spacing = spacing,
            Scale = scale,
            FallbackGlyph = FallbackGlyph,
            MaxGlyphCode = MaxGlyphCode
        };

        public static Rectangle CropGlyph(Image image, Rectangle rectangle) {
            int num = int.MaxValue;
            int num2 = int.MaxValue;
            int num3 = int.MinValue;
            int num4 = int.MinValue;
            for (int i = rectangle.Left; i < rectangle.Left + rectangle.Width; i++) {
                for (int j = rectangle.Top; j < rectangle.Top + rectangle.Height; j++) {
                    if (image.GetPixelFast(i, j).A != 0) {
                        num = Math.Min(num, i);
                        num2 = Math.Min(num2, j);
                        num3 = Math.Max(num3, i);
                        num4 = Math.Max(num4, j);
                    }
                }
            }
            return num == int.MaxValue
                ? new Rectangle(rectangle.Left, rectangle.Top, 0, 0)
                : new Rectangle(num, num2, num3 - num + 1, num4 - num2 + 1);
        }

        public void SetKerning(char code, char followingCode, float kerning) {
            m_kerningPairs ??= new Dictionary<int, short>();
            m_kerningPairs[(int)(((uint)code << 16) | followingCode)] = (short)kerning;
        }
    }
}