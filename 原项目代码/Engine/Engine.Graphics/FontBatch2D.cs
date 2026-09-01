using Engine.Media;

namespace Engine.Graphics {
    public class FontBatch2D : BaseFontBatch {
        public FontBatch2D() {
            Font = BitmapFont.DebugFont;
            DepthStencilState = DepthStencilState.None;
            RasterizerState = RasterizerState.CullNoneScissor;
            BlendState = BlendState.AlphaBlend;
            SamplerState = SamplerState.LinearClamp;
        }

        public void QueueBatch(FontBatch2D batch, Matrix? matrix = null, Color? color = null) {
            int count = TriangleVertices.Count;
            TriangleVertices.AddRange(batch.TriangleVertices);
            for (int i = 0; i < batch.TriangleIndices.Count; i++) {
                TriangleIndices.Add(batch.TriangleIndices[i] + count);
            }
            if (matrix.HasValue
                && matrix != Matrix.Identity) {
                TransformTriangles(matrix.Value, count);
            }
            if (color.HasValue
                && color != Color.White) {
                TransformTrianglesColors(color.Value, count);
            }
        }

        public void QueueText(string text, Vector2 position, float depth, Color color, TextAnchor anchor = TextAnchor.Default) {
            QueueText(
                text,
                position,
                depth,
                color,
                anchor,
                Vector2.One,
                Vector2.Zero
            );
        }

        public void QueueText(string text,
            Vector2 position,
            float depth,
            Color color,
            TextAnchor anchor,
            Vector2 scale,
            Vector2 spacing = default,
            float angle = 0f) {
            Vector2 vector;
            Vector2 vector2;
            Vector2 vector3;
            Vector2 vector4;
            if (angle != 0f) {
                vector = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                vector2 = position + new Vector2(0f - vector.Y, vector.X) * CalculateTextOffset(text, 0, text.Length, anchor, scale, spacing).Y;
                vector3 = vector * (scale.X * Font.Scale);
                vector4 = new Vector2(0f - vector.Y, vector.X) * (scale.Y * Font.Scale);
            }
            else {
                vector = new Vector2(1f, 0f);
                vector2 = position + new Vector2(0f, 1f) * CalculateTextOffset(text, 0, text.Length, anchor, scale, spacing).Y;
                vector3 = new Vector2(scale.X * Font.Scale, 0f);
                vector4 = new Vector2(0f, scale.Y * Font.Scale);
            }
            Vector2 vector5 = spacing + Font.Spacing;
            vector2 += 0.5f * (vector3 * vector5.X + vector4 * vector5.Y);
            int num = 0;
            int num2 = 0;
            for (int i = 0; i <= text.Length; i++) {
                if (i >= text.Length
                    || text[i] == '\n') {
                    Vector2 vector6 = vector2;
                    vector6 += num * (Font.GlyphHeight + vector5.Y) * vector4;
                    vector6 += CalculateTextOffset(text, num2, i - num2, anchor, scale, spacing).X * vector;
                    if ((anchor & TextAnchor.DisableSnapToPixels) == 0) {
                        vector6 = Vector2.Round(vector6);
                    }
                    QueueLine(
                        text,
                        num2,
                        i,
                        depth,
                        color,
                        vector5.X,
                        vector6,
                        vector3,
                        vector4
                    );
                    num++;
                    num2 = i + 1;
                }
            }
        }

        public void Flush(bool clearAfterFlush = true) {
            Flush(Vector4.One, clearAfterFlush);
        }

        public void Flush(Vector4 color, bool clearAfterFlush = true) {
            Flush(PrimitivesRenderer2D.ViewportMatrix(), color, clearAfterFlush);
        }

        void QueueLine(string text,
            int begin,
            int end,
            float depth,
            Color color,
            float fullSpacing,
            Vector2 corner,
            Vector2 right,
            Vector2 down) {
            Vector2 vector = corner;
            for (int i = begin; i < end; i++) {
                char c = text[i];
                if (c != '\r'
                    && c != '\u200b') {
                    if (c == '\u00a0') {
                        c = ' ';
                    }
                    BitmapFont.Glyph glyph = Font.GetGlyph(c);
                    if (!glyph.IsBlank) {
                        Vector2 vector2 = right * (glyph.TexCoord2.X - glyph.TexCoord1.X) * Font.Texture.Width;
                        Vector2 vector3 = down * (glyph.TexCoord2.Y - glyph.TexCoord1.Y) * Font.Texture.Height;
                        Vector2 vector4 = right * glyph.Offset.X + down * glyph.Offset.Y;
                        Vector2 vector5 = vector + vector4;
                        Vector2 vector6 = vector5 + vector2;
                        Vector2 vector7 = vector5 + vector3;
                        Vector2 vector8 = vector5 + vector2 + vector3;
                        int count = TriangleVertices.Count;
                        TriangleVertices.Count += 4;
                        TriangleVertices.Array[count] = new VertexPositionColorTexture(
                            new Vector3(vector5.X, vector5.Y, depth),
                            color,
                            new Vector2(glyph.TexCoord1.X, glyph.TexCoord1.Y)
                        );
                        TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(
                            new Vector3(vector6.X, vector6.Y, depth),
                            color,
                            new Vector2(glyph.TexCoord2.X, glyph.TexCoord1.Y)
                        );
                        TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(
                            new Vector3(vector8.X, vector8.Y, depth),
                            color,
                            new Vector2(glyph.TexCoord2.X, glyph.TexCoord2.Y)
                        );
                        TriangleVertices.Array[count + 3] = new VertexPositionColorTexture(
                            new Vector3(vector7.X, vector7.Y, depth),
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
                    float num = i < text.Length - 1 ? Font.GetKerning(c, text[i + 1]) : 0f;
                    vector += right * (glyph.Width - num + fullSpacing);
                }
            }
        }
    }
}