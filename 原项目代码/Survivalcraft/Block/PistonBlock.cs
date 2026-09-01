using Engine;
using Engine.Graphics;

namespace Game {
    public class PistonBlock : Block, IElectricElementBlock {
        public static int Index = 237;

        public BlockMesh[] m_standaloneBlockMeshes = new BlockMesh[4];

        public BlockMesh[] m_blockMeshesByData = new BlockMesh[48];

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Pistons");
            for (PistonMode pistonMode = PistonMode.Pushing; pistonMode <= PistonMode.StrictPulling; pistonMode++) {
                for (int i = 0; i < 2; i++) {
                    string name = i == 0 ? "PistonRetracted" : "PistonExtended";
                    Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh(name).ParentBone);
                    for (int j = 0; j < 6; j++) {
                        int num = SetFace(SetIsExtended(SetMode(0, pistonMode), i != 0), j);
                        Matrix m = j < 4 ? Matrix.CreateTranslation(0f, -0.5f, 0f)
                            * Matrix.CreateRotationY(j * (float)Math.PI / 2f + (float)Math.PI)
                            * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f) :
                            j != 4 ?
                                Matrix.CreateTranslation(0f, -0.5f, 0f)
                                * Matrix.CreateRotationX(-(float)Math.PI / 2f)
                                * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f) :
                                Matrix.CreateTranslation(0f, -0.5f, 0f)
                                * Matrix.CreateRotationX((float)Math.PI / 2f)
                                * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f);
                        m_blockMeshesByData[num] = new BlockMesh();
                        m_blockMeshesByData[num]
                            .AppendModelMeshPart(
                                model.FindMesh(name).MeshParts[0],
                                boneAbsoluteTransform * m,
                                false,
                                false,
                                false,
                                false,
                                Color.White
                            );
                        if (i == 0) {
                            switch (pistonMode) {
                                case PistonMode.Pulling:
                                    m_blockMeshesByData[num].TransformTextureCoordinates(Matrix.CreateTranslation(0f, 0.0625f, 0f), 1 << j); break;
                                case PistonMode.StrictPulling:
                                    m_blockMeshesByData[num].TransformTextureCoordinates(Matrix.CreateTranslation(0f, 0.125f, 0f), 1 << j); break;
                            }
                        }
                    }
                }
                Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("PistonRetracted").ParentBone);
                m_standaloneBlockMeshes[(int)pistonMode] = new BlockMesh();
                m_standaloneBlockMeshes[(int)pistonMode]
                    .AppendModelMeshPart(
                        model.FindMesh("PistonRetracted").MeshParts[0],
                        boneAbsoluteTransform2 * Matrix.CreateTranslation(0f, -0.5f, 0f),
                        false,
                        false,
                        false,
                        false,
                        Color.White
                    );
                switch (pistonMode) {
                    case PistonMode.Pulling:
                        m_standaloneBlockMeshes[(int)pistonMode].TransformTextureCoordinates(Matrix.CreateTranslation(0f, 0.0625f, 0f), 4); break;
                    case PistonMode.StrictPulling:
                        m_standaloneBlockMeshes[(int)pistonMode].TransformTextureCoordinates(Matrix.CreateTranslation(0f, 0.125f, 0f), 4); break;
                }
            }
        }

        public override bool IsCollapseSupportBlock(SubsystemTerrain subsystemTerrain, int value) => !IsFaceTransparent(subsystemTerrain, 4, value);

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) {
            int data = Terrain.ExtractData(value);
            int face2 = GetFace(data);
            if (GetIsExtended(data)) {
                if (face != face2) {
                    return face != CellFace.OppositeFace(face2);
                }
            }
            return false;
        }

        public override bool IsFaceNonAttachable(SubsystemTerrain subsystemTerrain, int face, int value, int attachBlockValue) =>
            IsFaceTransparent(subsystemTerrain, face, value);

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            int num = Terrain.ExtractData(value) & 0x3F;
            if (num < m_blockMeshesByData.Length
                && m_blockMeshesByData[num] != null) {
                Texture2D texture = GetDefaultTexture(value);
                generator.GenerateShadedMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_blockMeshesByData[num],
                    Color.White,
                    null,
                    null,
                    texture == null ? geometry.SubsetOpaque : geometry.GetGeometry(texture).SubsetOpaque
                );
            }
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            int mode = (int)GetMode(Terrain.ExtractData(value));
            if (mode < m_standaloneBlockMeshes.Length
                && m_standaloneBlockMeshes[mode] != null) {
                Texture2D texture = GetDefaultTexture(value);
                if (texture == null) {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[mode], color, 1f * size, ref matrix, environmentData);
                }
                else {
                    BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMeshes[mode], texture, color, 1f * size, ref matrix, environmentData);
                }
            }
        }

        public ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) =>
            new PistonElectricElement(subsystemElectricity, new Point3(x, y, z));

        public ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain,
            int value,
            int face,
            int connectorFace,
            int x,
            int y,
            int z) => ElectricConnectorType.Input;

        public int GetConnectionMask(int value) => int.MaxValue;

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(237, 0, SetMode(SetMaxExtension(0, 7), PistonMode.Pushing));
            yield return Terrain.MakeBlockValue(237, 0, SetMode(SetMaxExtension(0, 7), PistonMode.Pulling));
            yield return Terrain.MakeBlockValue(237, 0, SetMode(SetMaxExtension(0, 7), PistonMode.StrictPulling));
        }

        public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) =>
            LanguageControl.Get("PistonBlock", (int)GetMode(Terrain.ExtractData(value)));

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
            float num = float.PositiveInfinity;
            int face = 0;
            for (int i = 0; i < 6; i++) {
                float num2 = Vector3.Dot(CellFace.FaceToVector3(i), forward);
                if (num2 < num) {
                    num = num2;
                    face = i;
                }
            }
            int data = Terrain.ExtractData(value);
            BlockPlacementData result = default;
            result.Value = Terrain.MakeBlockValue(BlockIndex, 0, SetFace(data, face));
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            int data = Terrain.ExtractData(oldValue);
            dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(237, 0, SetFace(SetIsExtended(data, false), 0)), Count = 1 });
            showDebris = true;
        }

        public static bool GetIsExtended(int data) => (data & 1) != 0;

        public static int SetIsExtended(int data, bool isExtended) => (data & -2) | (isExtended ? 1 : 0);

        public static PistonMode GetMode(int data) => (PistonMode)((data >> 1) & 3);

        public static int SetMode(int data, PistonMode mode) => (data & -7) | (((int)mode & 3) << 1);

        public static int GetFace(int data) => (data >> 3) & 7;

        public static int SetFace(int data, int face) => (data & -57) | ((face & 7) << 3);

        public static int GetMaxExtension(int data) => (data >> 6) & 7;

        public static int SetMaxExtension(int data, int maxExtension) => (data & -449) | ((maxExtension & 7) << 6);

        public static int GetPullCount(int data) => (data >> 9) & 7;

        public static int SetPullCount(int data, int pullCount) => (data & -3585) | ((pullCount & 7) << 9);

        public static int GetSpeed(int data) => (data >> 12) & 7;

        public static int SetSpeed(int data, int speed) => (data & -12289) | ((speed & 3) << 12);

        public override bool IsMovableByPiston(int value, int pistonFace, int y, out bool isEnd) {
            int data = Terrain.ExtractData(value);
            isEnd = false;
            return !GetIsExtended(data);
        }
    }
}