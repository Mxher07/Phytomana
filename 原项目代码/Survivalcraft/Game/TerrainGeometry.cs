using Engine;
using Engine.Graphics;

namespace Game {
    public class TerrainGeometry : IDisposable {
        public TerrainGeometrySubset SubsetOpaque;

        public TerrainGeometrySubset SubsetAlphaTest;

        public TerrainGeometrySubset SubsetTransparent;

        public TerrainGeometrySubset[] OpaqueSubsetsByFace;

        public TerrainGeometrySubset[] AlphaTestSubsetsByFace;

        public TerrainGeometrySubset[] TransparentSubsetsByFace;

        public TerrainGeometrySubset[] Subsets;

        public Dictionary<Texture2D, TerrainGeometry> Draws;

        public Texture2D DefaultTexture;

        [Obsolete("此方法将弃用")]
        public TerrainGeometry() {
            InitSubsets();
        }

        public TerrainGeometry(Texture2D texture2D) {
            InitSubsets();
            DefaultTexture = texture2D;
            //添加到默认纹理区
            Draws = new Dictionary<Texture2D, TerrainGeometry> { { DefaultTexture, this } };
        }

        public virtual void InitSubsets() {
            Subsets = new TerrainGeometrySubset[7];
            for (int i = 0; i < 7; i++) {
                Subsets[i] = new TerrainGeometrySubset();
            }
            SubsetOpaque = Subsets[4];
            SubsetAlphaTest = Subsets[5];
            SubsetTransparent = Subsets[6];
            OpaqueSubsetsByFace = [Subsets[0], Subsets[1], Subsets[2], Subsets[3], Subsets[4], Subsets[4]];
            AlphaTestSubsetsByFace = [Subsets[5], Subsets[5], Subsets[5], Subsets[5], Subsets[5], Subsets[5]];
            TransparentSubsetsByFace = [Subsets[6], Subsets[6], Subsets[6], Subsets[6], Subsets[6], Subsets[6]];
        }

        public virtual TerrainGeometry GetGeometry(Texture2D texture) {
            if (Draws?.TryGetValue(texture, out TerrainGeometry geometries) ?? false) {
                return geometries;
            }
            TerrainGeometry geometry = new(texture);
            Draws ??= [];
            Draws.Add(texture, geometry);
            return geometry;
        }

        public virtual void ClearGeometry() {
            foreach (TerrainGeometrySubset subset in Subsets) {
                subset.Indices.Clear();
                subset.Vertices.Clear();
            }
            if (Draws == null) {
                return;
            }
            foreach (KeyValuePair<Texture2D, TerrainGeometry> drawItem in Draws) {
                if (drawItem.Value != this) {
                    drawItem.Value.ClearGeometry();
                }
            }
            Draws.Clear();
            if (DefaultTexture != null) {
                Draws.Add(DefaultTexture, this);
            }
        }

        public virtual void Dispose() {
            Utilities.Dispose(ref SubsetOpaque);
            Utilities.Dispose(ref SubsetAlphaTest);
            Utilities.Dispose(ref SubsetTransparent);
            for (int i = 0; i < OpaqueSubsetsByFace.Length; i++)
            {
                Utilities.Dispose(ref OpaqueSubsetsByFace[i]);
            }
            for (int j = 0; j < AlphaTestSubsetsByFace.Length; j++)
            {
                Utilities.Dispose(ref AlphaTestSubsetsByFace[j]);
            }
            for (int k = 0; k < TransparentSubsetsByFace.Length; k++)
            {
                Utilities.Dispose(ref TransparentSubsetsByFace[k]);
            }
            for (int l = 0; l < Subsets.Length; l++)
            {
                Utilities.Dispose(ref Subsets[l]);
            }
            if (Draws != null) {
                foreach (TerrainGeometry terrainGeometry in Draws.Values) {
                    if (terrainGeometry != this) {
                        terrainGeometry?.Dispose();
                    }
                }
            }
        }
    }
}