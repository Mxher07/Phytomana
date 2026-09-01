using Engine.Media;

namespace Engine.Graphics {
    public class FontBatch3D : BaseFontBatch {
        public FontBatch3D() {
            Font = BitmapFont.DebugFont;
            DepthStencilState = DepthStencilState.Default;
            RasterizerState = RasterizerState.CullNoneScissor;
            BlendState = BlendState.AlphaBlend;
            SamplerState = SamplerState.LinearClamp;
        }

        public void QueueText(string text, Vector3 position, Vector3 right, Vector3 down, Color color, TextAnchor anchor = TextAnchor.Default) {
            QueueText(
                text,
                position,
                right,
                down,
                color,
                anchor,
                Vector2.Zero
            );
        }

        public void QueueText(string text,
            Vector3 position,
            Vector3 right,
            Vector3 down,
            Color color,
            TextAnchor anchor,
            Vector2 spacing) {
            Vector2 scale = new(right.Length(), down.Length());
            Vector2 vector = CalculateTextOffset(text, 0, text.Length, anchor, scale, spacing);
            Vector3 vector2 = position + vector.X * Vector3.Normalize(right) + vector.Y * Vector3.Normalize(down);
            Vector3 v = vector2;
            right *= Font.Scale;
            down *= Font.Scale;
            int num = 0;
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (c == '\u00a0') {
                    c = ' ';
                }
                switch (c) {
                    case '\n':
                        num++;
                        v = vector2 + num * (Font.GlyphHeight + Font.Spacing.Y + spacing.Y) * down;
                        continue;
                    case '\r':
                    case '\u200b': continue;
                }
                BitmapFont.Glyph glyph = Font.GetGlyph(c);
                if (!glyph.IsBlank) {
                    Vector3 v2 = right * (glyph.TexCoord2.X - glyph.TexCoord1.X) * Font.Texture.Width;
                    Vector3 v3 = down * (glyph.TexCoord2.Y - glyph.TexCoord1.Y) * Font.Texture.Height;
                    Vector3 v4 = right * glyph.Offset.X + down * glyph.Offset.Y;
                    Vector3 v5 = v + v4;
                    Vector3 vector3 = v5 + v2;
                    Vector3 vector4 = v5 + v3;
                    Vector3 vector5 = v5 + v2 + v3;
                    int count = TriangleVertices.Count;
                    TriangleVertices.Count += 4;
                    TriangleVertices.Array[count] = new VertexPositionColorTexture(
                        new Vector3(v5.X, v5.Y, v5.Z),
                        color,
                        new Vector2(glyph.TexCoord1.X, glyph.TexCoord1.Y)
                    );
                    TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(
                        new Vector3(vector3.X, vector3.Y, vector3.Z),
                        color,
                        new Vector2(glyph.TexCoord2.X, glyph.TexCoord1.Y)
                    );
                    TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(
                        new Vector3(vector5.X, vector5.Y, vector5.Z),
                        color,
                        new Vector2(glyph.TexCoord2.X, glyph.TexCoord2.Y)
                    );
                    TriangleVertices.Array[count + 3] = new VertexPositionColorTexture(
                        new Vector3(vector4.X, vector4.Y, vector4.Z),
                        color,
                        new Vector2(glyph.TexCoord1.X, glyph.TexCoord2.Y)
                    );
                    int count2 = TriangleIndices.Count;
                    TriangleIndices.Count += 6;
                    TriangleIndices.Array[count2] = count;
                    TriangleIndices.Array[count2 + 1] = count + 1;
                    TriangleIndices.Array[count2 + 2] = count + 2;
                    TriangleIndices.Array[count2 + 3] = count + 2;
                    TriangleIndices.Array[count2 + 4] = count + 3;
                    TriangleIndices.Array[count2 + 5] = count;
                }
                float num2 = i < text.Length - 1 ? Font.GetKerning(c, text[i + 1]) : 0f;
                v += right * (glyph.Width - num2 + Font.Spacing.X + spacing.X);
            }
        }
    }
}