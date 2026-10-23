using System;
using Engine.Core;
using Engine.Graphics;
using Game;

namespace Phytomana {
    /// <summary>
    /// 联机版 glTF 模型与方块网格的兼容层。
    /// 引擎的 GltfMeshConverter 会给「含法线 + UV 的 TRIANGLES」网格自动生成切线，
    /// 顶点声明因此变成 Position/Normal/TexCoord/Tangent 四元素；
    /// 而 BlockMesh.AppendModelMeshPart 只接受三元素（Position/Normal/TexCoord）格式，
    /// 否则抛 "Wrong vertex format for a block mesh."，方块模型直接不显示。
    /// 本类把多元素顶点缓冲按三元素格式重打包后，仍交给原生 AppendModelMeshPart 处理
    /// 变换、光照与绕序，行为与 .dae 模型一致；三元素输入则原样走原生路径。
    /// </summary>
    public static class GltfBlockMeshCompat {
        static readonly string PositionSemantic = VertexElementSemantic.Position.GetSemanticString();
        static readonly string NormalSemantic = VertexElementSemantic.Normal.GetSemanticString();
        static readonly string TexCoordSemantic = VertexElementSemantic.TextureCoordinate.GetSemanticString();

        public static void AppendModelMeshPart(
            BlockMesh blockMesh,
            ModelMeshPart meshPart,
            Matrix matrix,
            bool makeEmissive,
            bool flipWindingOrder,
            bool doubleSided,
            bool flipNormals,
            Color color
        ) {
            VertexBuffer source = meshPart.VertexBuffer;
            if (source?.VertexDeclaration == null || !(source.Tag is byte[] sourceBytes)) {
                // 无源数据可重打包：交给原生路径给出与引擎一致的行为。
                blockMesh.AppendModelMeshPart(meshPart, matrix, makeEmissive, flipWindingOrder, doubleSided, flipNormals, color);
                return;
            }
            var elements = source.VertexDeclaration.VertexElements;
            bool nativeFormat = elements.Count == 3
                && elements[0].Semantic == PositionSemantic
                && elements[1].Semantic == NormalSemantic
                && elements[2].Semantic == TexCoordSemantic;
            if (nativeFormat) {
                blockMesh.AppendModelMeshPart(meshPart, matrix, makeEmissive, flipWindingOrder, doubleSided, flipNormals, color);
                return;
            }
            int stride = source.VertexDeclaration.VertexStride;
            int positionOffset = -1;
            int normalOffset = -1;
            int texCoordOffset = -1;
            for (int i = 0; i < elements.Count; i++) {
                VertexElement element = elements[i];
                if (element.Semantic == PositionSemantic) {
                    positionOffset = element.Offset;
                }
                else if (element.Semantic == NormalSemantic) {
                    normalOffset = element.Offset;
                }
                else if (element.Semantic == TexCoordSemantic) {
                    texCoordOffset = element.Offset;
                }
            }
            if (stride <= 0 || positionOffset < 0 || normalOffset < 0 || texCoordOffset < 0) {
                blockMesh.AppendModelMeshPart(meshPart, matrix, makeEmissive, flipWindingOrder, doubleSided, flipNormals, color);
                return;
            }
            int vertexCount = sourceBytes.Length / stride;
            // 三元素顶点：Position(12) + Normal(12) + TexCoord(8) = 32 字节。
            const int packedStride = 32;
            byte[] packed = new byte[vertexCount * packedStride];
            for (int i = 0; i < vertexCount; i++) {
                int sourceBase = i * stride;
                int packedBase = i * packedStride;
                Buffer.BlockCopy(sourceBytes, sourceBase + positionOffset, packed, packedBase, 12);
                Buffer.BlockCopy(sourceBytes, sourceBase + normalOffset, packed, packedBase + 12, 12);
                Buffer.BlockCopy(sourceBytes, sourceBase + texCoordOffset, packed, packedBase + 24, 8);
            }
            var declaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementSemantic.Normal),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate)
            );
            var buffer = new VertexBuffer(declaration, vertexCount) { Tag = packed };
            var packedPart = new ModelMeshPart {
                VertexBuffer = buffer,
                IndexBuffer = meshPart.IndexBuffer,
                StartIndex = meshPart.StartIndex,
                IndicesCount = meshPart.IndicesCount
            };
            blockMesh.AppendModelMeshPart(packedPart, matrix, makeEmissive, flipWindingOrder, doubleSided, flipNormals, color);
            // 原生附加只读取 Tag 源数据，临时顶点缓冲用完即释放，避免驻留 GPU 资源。
            buffer.Dispose();
        }
    }
}
