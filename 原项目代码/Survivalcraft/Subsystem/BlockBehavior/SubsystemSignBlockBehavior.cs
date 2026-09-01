using System.Globalization;
using Engine;
using Engine.Graphics;
using Engine.Media;
using TemplatesDatabase;

namespace Game {
    public class SubsystemSignBlockBehavior : SubsystemBlockBehavior, IDrawable, IUpdateable {
        public class TextData {
            public Point3 Point;

            public MovingBlock MovingBlock;

            public string[] Lines = [string.Empty, string.Empty, string.Empty, string.Empty];

            public Color[] Colors = [Color.Black, Color.Black, Color.Black, Color.Black];

            public string Url = string.Empty;

            public int? TextureLocation;

            public float UsedTextureWidth;

            public float UsedTextureHeight;

            public float Distance;

            public int ToBeRenderedFrame;

            public int Light;
        }

        public const float m_maxVisibilityDistanceSqr = 400f;

        public const float m_minUpdateDistance = 2f;

        public const int m_textWidth = 128;

        public const int m_textHeight = 32;

        public static int m_maxTexts = 32;

        public float m_fontScale = 1f;

        public SubsystemGameWidgets m_subsystemViews;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemGameInfo m_subsystemGameInfo;

        public Dictionary<Point3, TextData> m_textsByPoint = [];

        public Dictionary<MovingBlock, TextData> m_textsByMovingBlock = new();

        public List<RenderTarget2D> m_texturesByPoint = [];

        public TextData[] m_textureLocations = new TextData[32];

        public List<TextData> m_nearTexts = [];

        public BitmapFont m_font = LabelWidget.BitmapFont;

        public RenderTarget2D m_renderTarget;

        public List<Vector3> m_lastUpdatePositions = [];

        public PrimitivesRenderer2D m_primitivesRenderer2D = new();

        public PrimitivesRenderer3D m_primitivesRenderer3D = new();

        public bool ShowSignsTexture;

        public bool CopySignsText;

        public static int[] m_drawOrders = [50];

        public override int[] HandledBlocks => [23, 97, 98, 210, 211];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public int[] DrawOrders => m_drawOrders;

        public SignData GetSignData(Point3 point) {
            if (m_textsByPoint.TryGetValue(point, out TextData value)) {
                return new SignData { Lines = value.Lines.ToArray(), Colors = value.Colors.ToArray(), Url = value.Url };
            }
            return null;
        }

        public void SetSignData(Point3 point, string[] lines, Color[] colors, string url, MovingBlock movingBlock = null) {
            TextData textData = new() { Point = point };
            for (int i = 0; i < 4; i++) {
                textData.Lines[i] = lines[i];
                textData.Colors[i] = colors[i];
            }
            textData.Url = url;
            textData.MovingBlock = movingBlock;
            if (!MovingBlock.IsNullOrStopped(movingBlock)) {
                m_textsByMovingBlock[movingBlock] = textData;
            }
            else {
                m_textsByPoint[point] = textData;
            }
            m_lastUpdatePositions.Clear();
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
            int num = Terrain.ExtractContents(cellValueFast);
            int data = Terrain.ExtractData(cellValueFast);
            Block block = BlocksManager.Blocks[num];
            if (block is AttachedSignBlock) {
                Point3 point = CellFace.FaceToPoint3(AttachedSignBlock.GetFace(data));
                int x2 = x - point.X;
                int y2 = y - point.Y;
                int z2 = z - point.Z;
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(x2, y2, z2);
                int cellContents = Terrain.ExtractContents(cellValue);
                if (!BlocksManager.Blocks[cellContents].IsCollidable_(cellValue)) {
                    SubsystemTerrain.DestroyCell(
                        0,
                        x,
                        y,
                        z,
                        0,
                        false,
                        false
                    );
                }
            }
            else if (block is PostedSignBlock) {
                int num2 = PostedSignBlock.GetHanging(data)
                    ? SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z)
                    : SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
                if (!BlocksManager.Blocks[Terrain.ExtractContents(num2)].IsCollidable_(num2)) {
                    SubsystemTerrain.DestroyCell(
                        0,
                        x,
                        y,
                        z,
                        0,
                        false,
                        false
                    );
                }
            }
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
            Point3 point = new(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
            if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure) {
                SignData signData = GetSignData(point);
                if (signData != null
                    && !string.IsNullOrEmpty(signData.Url)) {
                    WebBrowserManager.LaunchBrowser(signData.Url);
                }
            }
            else if (componentMiner.ComponentPlayer != null) {
                DialogsManager.ShowDialog(componentMiner.ComponentPlayer.GuiWidget, new EditSignDialog(this, point));
            }
            return true;
        }

        public override void OnBlockStartMoving(int value, int newValue, int x, int y, int z, MovingBlock movingBlock) {
            Point3 key = new(x, y, z);
            bool valueGotten = m_textsByPoint.TryGetValue(key, out TextData textData);
            m_textsByPoint.Remove(key);
            if (valueGotten) {
                m_textsByMovingBlock.Add(movingBlock, textData);
                textData.MovingBlock = movingBlock;
            }
            m_lastUpdatePositions.Clear();
        }

        public override void OnBlockStopMoving(int value, int oldValue, int x, int y, int z, MovingBlock movingBlock) {
            bool valueGotten = m_textsByMovingBlock.TryGetValue(movingBlock, out TextData textData);
            m_textsByMovingBlock.Remove(movingBlock);
            if (valueGotten) {
                m_textsByPoint[new Point3(x, y, z)] = textData;
                textData.Point = new Point3(x, y, z);
                textData.MovingBlock = null;
            }
            m_lastUpdatePositions.Clear();
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            Point3 key = new(x, y, z);
            m_textsByPoint.Remove(key);
            m_lastUpdatePositions.Clear();
        }

        public virtual void Update(float dt) {
            UpdateRenderTarget();
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            DrawSigns(camera);
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemViews = Project.FindSubsystem<SubsystemGameWidgets>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            CreateRenderTarget();
            foreach (ValuesDictionary value11 in valuesDictionary.GetValue<ValuesDictionary>("Texts").Values) {
                Point3 value = value11.GetValue<Point3>("Point");
                MovingBlock movingBlock = MovingBlock.LoadFromValuesDictionary(Project, value11);
                string value2 = value11.GetValue("Line1", string.Empty);
                string value3 = value11.GetValue("Line2", string.Empty);
                string value4 = value11.GetValue("Line3", string.Empty);
                string value5 = value11.GetValue("Line4", string.Empty);
                Color value6 = value11.GetValue("Color1", Color.Black);
                Color value7 = value11.GetValue("Color2", Color.Black);
                Color value8 = value11.GetValue("Color3", Color.Black);
                Color value9 = value11.GetValue("Color4", Color.Black);
                string value10 = value11.GetValue("Url", string.Empty);
                SetSignData(value, [value2, value3, value4, value5], [value6, value7, value8, value9], value10, movingBlock);
            }
            Display.DeviceReset += Display_DeviceReset;
        }

        public virtual void SaveTextData(TextData textData, ValuesDictionary valuesDictionary) {
            valuesDictionary.SetValue("Point", textData.Point);
            textData.MovingBlock?.SetValuesDicionary(valuesDictionary);
            if (!string.IsNullOrEmpty(textData.Lines[0])) {
                valuesDictionary.SetValue("Line1", textData.Lines[0]);
            }
            if (!string.IsNullOrEmpty(textData.Lines[1])) {
                valuesDictionary.SetValue("Line2", textData.Lines[1]);
            }
            if (!string.IsNullOrEmpty(textData.Lines[2])) {
                valuesDictionary.SetValue("Line3", textData.Lines[2]);
            }
            if (!string.IsNullOrEmpty(textData.Lines[3])) {
                valuesDictionary.SetValue("Line4", textData.Lines[3]);
            }
            if (textData.Colors[0] != Color.Black) {
                valuesDictionary.SetValue("Color1", textData.Colors[0]);
            }
            if (textData.Colors[1] != Color.Black) {
                valuesDictionary.SetValue("Color2", textData.Colors[1]);
            }
            if (textData.Colors[2] != Color.Black) {
                valuesDictionary.SetValue("Color3", textData.Colors[2]);
            }
            if (textData.Colors[3] != Color.Black) {
                valuesDictionary.SetValue("Color4", textData.Colors[3]);
            }
            if (!string.IsNullOrEmpty(textData.Url)) {
                valuesDictionary.SetValue("Url", textData.Url);
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            int num = 0;
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Texts", valuesDictionary2);
            foreach (TextData value in m_textsByPoint.Values) {
                if (!MovingBlock.IsNullOrStopped(value.MovingBlock)) {
                    continue;
                }
                ValuesDictionary valuesDictionary3 = new();
                SaveTextData(value, valuesDictionary3);
                valuesDictionary2.SetValue(num++.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
            }
            foreach (TextData textData in m_textsByMovingBlock.Values) {
                ValuesDictionary valuesDictionary3 = new();
                SaveTextData(textData, valuesDictionary3);
                valuesDictionary2.SetValue(num++.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
            }
        }

        public override void Dispose() {
            Utilities.Dispose(ref m_renderTarget);
            Display.DeviceReset -= Display_DeviceReset;
        }

        public void Display_DeviceReset() {
            InvalidateRenderTarget();
        }

        public void CreateRenderTarget() {
            int eachSignHeight = (int)(m_font.GlyphHeight * m_fontScale * 4);
            if (Display.MaxTextureSize < eachSignHeight * 32) {
                m_maxTexts = Display.MaxTextureSize / eachSignHeight;
            }
            m_renderTarget = new RenderTarget2D(
                (int)(m_font.GlyphHeight * 16 * m_fontScale),
                eachSignHeight * m_maxTexts,
                1,
                ColorFormat.Rgba8888,
                DepthFormat.None
            );
        }

        public void InvalidateRenderTarget() {
            m_lastUpdatePositions.Clear();
            for (int i = 0; i < m_textureLocations.Length; i++) {
                m_textureLocations[i] = null;
            }
            foreach (TextData value in m_textsByPoint.Values) {
                value.TextureLocation = null;
            }
        }

        public void RenderText(FontBatch2D fontBatch, FlatBatch2D flatBatch, TextData textData) {
            if (!textData.TextureLocation.HasValue) {
                return;
            }
            List<string> list = [];
            List<Color> list2 = [];
            for (int i = 0; i < textData.Lines.Length; i++) {
                if (!string.IsNullOrEmpty(textData.Lines[i])) {
                    list.Add(textData.Lines[i].Replace("\\", "").ToUpper());
                    list2.Add(textData.Colors[i]);
                }
            }
            if (list.Count > 0) {
                float num = list.Max(l => l.Length) * m_font.GlyphHeight * m_fontScale;
                float num2 = list.Count * m_font.GlyphHeight * m_fontScale;
                float num3 = 4f;
                float num4;
                float num5;
                if (num / num2 < num3) {
                    num4 = num2 * num3;
                    num5 = num2;
                }
                else {
                    num4 = num;
                    num5 = num / num3;
                }
                bool flag = !string.IsNullOrEmpty(textData.Url);
                for (int j = 0; j < list.Count; j++) {
                    fontBatch.QueueText(
                        position: new Vector2(
                            num4 / 2f,
                            j * m_font.GlyphHeight * m_fontScale
                            + textData.TextureLocation.Value * (4f * m_font.GlyphHeight * m_fontScale)
                            + (num5 - num2) / 2f
                        ),
                        text: list[j],
                        depth: 0f,
                        color: flag ? new Color(0, 0, 64) : list2[j],
                        anchor: TextAnchor.HorizontalCenter,
                        scale: new Vector2(1f / m_font.Scale * m_fontScale),
                        spacing: Vector2.Zero
                    );
                }
                textData.UsedTextureWidth = num4;
                textData.UsedTextureHeight = num5;
            }
        }

        public virtual void UpdateRenderTarget() {
            bool flag = false;
            foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets) {
                bool flag2 = false;
                foreach (Vector3 lastUpdatePosition in m_lastUpdatePositions) {
                    if (Vector3.DistanceSquared(gameWidget.ActiveCamera.ViewPosition, lastUpdatePosition) < 4f) {
                        flag2 = true;
                        break;
                    }
                }
                if (!flag2) {
                    flag = true;
                    break;
                }
            }
            if (!flag) {
                return;
            }
            m_lastUpdatePositions.Clear();
            m_lastUpdatePositions.AddRange(m_subsystemViews.GameWidgets.Select(v => v.ActiveCamera.ViewPosition));
            m_nearTexts.Clear();
            foreach (TextData value in m_textsByPoint.Values) {
                Point3 point = value.Point;
                float num = m_subsystemViews.CalculateSquaredDistanceFromNearestView(new Vector3(point));
                if (num <= m_maxVisibilityDistanceSqr) {
                    value.Distance = num;
                    m_nearTexts.Add(value);
                }
            }
            foreach (MovingBlock movingBlock in m_textsByMovingBlock.Keys) {
                Vector3 position = movingBlock.Position;
                TextData value = m_textsByMovingBlock[movingBlock];
                float num = m_subsystemViews.CalculateSquaredDistanceFromNearestView(position);
                if (num <= m_maxVisibilityDistanceSqr) {
                    value.Distance = num;
                    m_nearTexts.Add(value);
                }
            }
            m_nearTexts.Sort((d1, d2) => Comparer<float>.Default.Compare(d1.Distance, d2.Distance));
            if (m_nearTexts.Count > m_maxTexts) {
                m_nearTexts.RemoveRange(m_maxTexts, m_nearTexts.Count - m_maxTexts);
            }
            foreach (TextData nearText in m_nearTexts) {
                nearText.ToBeRenderedFrame = Time.FrameIndex;
            }
            bool flag3 = false;
            for (int i = 0; i < MathUtils.Min(m_nearTexts.Count, m_maxTexts); i++) {
                TextData textData = m_nearTexts[i];
                if (textData.TextureLocation.HasValue) {
                    continue;
                }
                int num2 = m_textureLocations.FirstIndex(d => d == null);
                if (num2 < 0
                    || num2 >= m_maxTexts) {
                    num2 = m_textureLocations.FirstIndex(d => d.ToBeRenderedFrame != Time.FrameIndex);
                }
                if (num2 >= 0) {
                    TextData textData2 = m_textureLocations[num2];
                    if (textData2 != null) {
                        textData2.TextureLocation = null;
                        m_textureLocations[num2] = null;
                    }
                    m_textureLocations[num2] = textData;
                    textData.TextureLocation = num2;
                    flag3 = true;
                }
            }
            if (!flag3) {
                return;
            }
            RenderTarget2D renderTarget = Display.RenderTarget;
            Display.RenderTarget = m_renderTarget;
            try {
                Display.Clear(new Vector4(Color.Transparent));
                FlatBatch2D flatBatch = m_primitivesRenderer2D.FlatBatch(0, DepthStencilState.None, null, BlendState.Opaque);
                FontBatch2D fontBatch = m_primitivesRenderer2D.FontBatch(
                    m_font,
                    1,
                    DepthStencilState.None,
                    null,
                    BlendState.Opaque,
                    SamplerState.PointClamp
                );
                for (int j = 0; j < m_maxTexts; j++) {
                    TextData textData3 = m_textureLocations[j];
                    if (textData3 != null) {
                        RenderText(fontBatch, flatBatch, textData3);
                    }
                }
                m_primitivesRenderer2D.Flush();
            }
            finally {
                Display.RenderTarget = renderTarget;
            }
        }

        public virtual void DrawSigns(Camera camera) {
            if (m_nearTexts.Count <= 0) {
                return;
            }
            TexturedBatch3D texturedBatch3D = m_primitivesRenderer3D.TexturedBatch(
                m_renderTarget,
                false,
                0,
                DepthStencilState.DepthRead,
                RasterizerState.CullCounterClockwiseScissor,
                null,
                SamplerState.PointClamp
            );
            foreach (TextData nearText in m_nearTexts) {
                if (!nearText.TextureLocation.HasValue) {
                    continue;
                }
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(nearText.Point.X, nearText.Point.Y, nearText.Point.Z);
                if (!MovingBlock.IsNullOrStopped(nearText.MovingBlock)) {
                    cellValue = nearText.MovingBlock.Value;
                }
                int num = Terrain.ExtractContents(cellValue);
                if (!(BlocksManager.Blocks[num] is SignBlock signBlock)) {
                    continue;
                }
                int data = Terrain.ExtractData(cellValue);
                BlockMesh signSurfaceBlockMesh = signBlock.GetSignSurfaceBlockMesh(data);
                if (signSurfaceBlockMesh != null) {
                    TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(nearText.Point.X, nearText.Point.Z);
                    if (chunkAtCell != null
                        && chunkAtCell.State >= TerrainChunkState.InvalidVertices1) {
                        nearText.Light = Terrain.ExtractLight(cellValue);
                    }
                    float num2 = LightingManager.LightIntensityByLightValue[nearText.Light];
                    Color color = new(num2, num2, num2);
                    float x = 0f;
                    float x2 = nearText.UsedTextureWidth / (m_font.GlyphHeight * 16f * m_fontScale);
                    float x3 = (float)nearText.TextureLocation.Value / m_maxTexts;
                    float x4 = (nearText.TextureLocation.Value + nearText.UsedTextureHeight / (m_font.GlyphHeight * 4f * m_fontScale)) / m_maxTexts;
                    Vector3 signSurfaceNormal = signBlock.GetSignSurfaceNormal(data);
                    Vector3 vector = new(nearText.Point.X, nearText.Point.Y, nearText.Point.Z);
                    if (!MovingBlock.IsNullOrStopped(nearText.MovingBlock)) {
                        vector = nearText.MovingBlock.Position;
                    }
                    float num3 = Vector3.Dot(camera.ViewPosition - (vector + new Vector3(0.5f)), signSurfaceNormal);
                    Vector3 vector2 = MathUtils.Max(0.01f * num3, 0.005f) * signSurfaceNormal;
                    for (int i = 0; i < signSurfaceBlockMesh.Indices.Count / 3; i++) {
                        BlockMeshVertex blockMeshVertex = signSurfaceBlockMesh.Vertices.Array[signSurfaceBlockMesh.Indices.Array[i * 3]];
                        BlockMeshVertex blockMeshVertex2 = signSurfaceBlockMesh.Vertices.Array[signSurfaceBlockMesh.Indices.Array[i * 3 + 1]];
                        BlockMeshVertex blockMeshVertex3 = signSurfaceBlockMesh.Vertices.Array[signSurfaceBlockMesh.Indices.Array[i * 3 + 2]];
                        Vector3 p = blockMeshVertex.Position + vector + vector2;
                        Vector3 p2 = blockMeshVertex2.Position + vector + vector2;
                        Vector3 p3 = blockMeshVertex3.Position + vector + vector2;
                        Vector2 textureCoordinates = blockMeshVertex.TextureCoordinates;
                        Vector2 textureCoordinates2 = blockMeshVertex2.TextureCoordinates;
                        Vector2 textureCoordinates3 = blockMeshVertex3.TextureCoordinates;
                        textureCoordinates.X = MathUtils.Lerp(x, x2, textureCoordinates.X);
                        textureCoordinates2.X = MathUtils.Lerp(x, x2, textureCoordinates2.X);
                        textureCoordinates3.X = MathUtils.Lerp(x, x2, textureCoordinates3.X);
                        textureCoordinates.Y = MathUtils.Lerp(x3, x4, textureCoordinates.Y);
                        textureCoordinates2.Y = MathUtils.Lerp(x3, x4, textureCoordinates2.Y);
                        textureCoordinates3.Y = MathUtils.Lerp(x3, x4, textureCoordinates3.Y);
                        texturedBatch3D.QueueTriangle(
                            p,
                            p2,
                            p3,
                            textureCoordinates,
                            textureCoordinates2,
                            textureCoordinates3,
                            color
                        );
                    }
                }
            }
            m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
        }
    }
}