using Engine.Media;

namespace Engine.Graphics {
    public class BasePrimitivesRenderer<T1, T2, T3> where T1 : BaseFlatBatch, new()
        where T2 : BaseTexturedBatch, new()
        where T3 : BaseFontBatch, new() {
        public bool m_sortNeeded;

        public List<BaseBatch> m_allBatches = [];

        public LinkedList<T1> m_flatBatches = new();

        public LinkedList<T2> m_texturedBatches = new();

        public LinkedList<T3> m_fontBatches = new();

        public IEnumerable<T1> FlatBatches => m_flatBatches;

        public IEnumerable<T2> TexturedBatches => m_texturedBatches;

        public IEnumerable<T3> FontBatches => m_fontBatches;

        public T1 FindFlatBatch(int layer, DepthStencilState depthStencilState, RasterizerState rasterizerState, BlendState blendState) {
            for (LinkedListNode<T1> linkedListNode = m_flatBatches.First; linkedListNode != null; linkedListNode = linkedListNode.Next) {
                T1 value = linkedListNode.Value;
                if (layer == value.Layer
                    && depthStencilState == value.DepthStencilState
                    && rasterizerState == value.RasterizerState
                    && blendState == value.BlendState) {
                    if (linkedListNode.Previous != null) {
                        m_flatBatches.Remove(linkedListNode);
                        m_flatBatches.AddFirst(linkedListNode);
                    }
                    return value;
                }
            }
            m_sortNeeded |= m_allBatches.Count > 0 && m_allBatches[^1].Layer > layer;
            T1 val = new() { Layer = layer, DepthStencilState = depthStencilState, RasterizerState = rasterizerState, BlendState = blendState };
            m_flatBatches.AddFirst(val);
            m_allBatches.Add(val);
            return val;
        }

        public T2 FindTexturedBatch(Texture2D texture,
            bool useAlphaTest,
            int layer,
            DepthStencilState depthStencilState,
            RasterizerState rasterizerState,
            BlendState blendState,
            SamplerState samplerState) {
            ArgumentNullException.ThrowIfNull(texture);
            for (LinkedListNode<T2> linkedListNode = m_texturedBatches.First; linkedListNode != null; linkedListNode = linkedListNode.Next) {
                T2 value = linkedListNode.Value;
                if (texture == value.Texture
                    && useAlphaTest == value.UseAlphaTest
                    && layer == value.Layer
                    && depthStencilState == value.DepthStencilState
                    && rasterizerState == value.RasterizerState
                    && blendState == value.BlendState
                    && samplerState == value.SamplerState) {
                    if (linkedListNode.Previous != null) {
                        m_texturedBatches.Remove(linkedListNode);
                        m_texturedBatches.AddFirst(linkedListNode);
                    }
                    return value;
                }
            }
            m_sortNeeded |= m_allBatches.Count > 0 && m_allBatches[^1].Layer > layer;
            T2 val = new() {
                Layer = layer,
                UseAlphaTest = useAlphaTest,
                Texture = texture,
                SamplerState = samplerState,
                DepthStencilState = depthStencilState,
                RasterizerState = rasterizerState,
                BlendState = blendState
            };
            m_texturedBatches.AddFirst(val);
            m_allBatches.Add(val);
            return val;
        }

        public T3 FindFontBatch(BitmapFont font,
            int layer,
            DepthStencilState depthStencilState,
            RasterizerState rasterizerState,
            BlendState blendState,
            SamplerState samplerState) {
            ArgumentNullException.ThrowIfNull(font);
            for (LinkedListNode<T3> linkedListNode = m_fontBatches.First; linkedListNode != null; linkedListNode = linkedListNode.Next) {
                T3 value = linkedListNode.Value;
                if (font == value.Font
                    && layer == value.Layer
                    && depthStencilState == value.DepthStencilState
                    && rasterizerState == value.RasterizerState
                    && blendState == value.BlendState
                    && samplerState == value.SamplerState) {
                    if (linkedListNode.Previous != null) {
                        m_fontBatches.Remove(linkedListNode);
                        m_fontBatches.AddFirst(linkedListNode);
                    }
                    return value;
                }
            }
            m_sortNeeded |= m_allBatches.Count > 0 && m_allBatches[^1].Layer > layer;
            T3 val = new() {
                Layer = layer,
                Font = font,
                SamplerState = samplerState,
                DepthStencilState = depthStencilState,
                RasterizerState = rasterizerState,
                BlendState = blendState
            };
            m_fontBatches.AddFirst(val);
            m_allBatches.Add(val);
            return val;
        }

        public void Flush(Matrix matrix, bool clearAfterFlush = true, int maxLayer = int.MaxValue) {
            Flush(matrix, Vector4.One, clearAfterFlush, maxLayer);
        }

        public void Flush(Matrix matrix, Vector4 color, bool clearAfterFlush = true, int maxLayer = int.MaxValue) {
            if (m_sortNeeded) {
                m_sortNeeded = false;
                m_allBatches.Sort(
                    delegate(BaseBatch b1, BaseBatch b2) {
                        return b1.Layer < b2.Layer ? -1 :
                            b1.Layer > b2.Layer ? 1 : 0;
                    }
                );
            }
            foreach (BaseBatch allBatch in m_allBatches) {
                if (allBatch.Layer > maxLayer) {
                    break;
                }
                if (!allBatch.IsEmpty()) {
                    allBatch.Flush(matrix, color, clearAfterFlush);
                }
            }
        }

        public void Clear() {
            foreach (BaseBatch allBatch in m_allBatches) {
                allBatch.Clear();
            }
        }
    }
}