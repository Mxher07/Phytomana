using Engine;
using Engine.Graphics;

namespace Game {
    public class ObjModelReader {
        public static Dictionary<int, List<int>> FaceMap = [];

        static ObjModelReader() {
            FaceMap.Add(4, [0, 2, 1]); //顶面
            FaceMap.Add(5, [0, 2, 1]); //底面
            FaceMap.Add(2, [0, 2, 1]); //逆
            FaceMap.Add(3, [0, 2, 1]); //逆
            FaceMap.Add(0, [0, 2, 1]); //顺
            FaceMap.Add(1, [0, 2, 1]); //顺
        }

        public struct ObjPosition {
            public float x, y, z;

            public ObjPosition(string x_, string y_, string z_) {
                x = float.Parse(x_);
                y = float.Parse(y_);
                z = float.Parse(z_);
            }

            public ObjPosition(float x_, float y_, float z_) {
                x = x_;
                y = y_;
                z = z_;
            }
        }

        public struct ObjVertex {
            public ObjPosition position;
            public ObjNormal objNormal;
            public ObjTexCood texCood;
        }

        public struct ObjNormal {
            public float x, y, z;

            public ObjNormal(string x_, string y_, string z_) {
                x = float.Parse(x_);
                y = float.Parse(y_);
                z = float.Parse(z_);
            }

            public ObjNormal(float x_, float y_, float z_) {
                x = x_;
                y = y_;
                z = z_;
            }
        }

        public struct ObjTexCood {
            public float tx, ty;

            public ObjTexCood(string tx_, string ty_) {
                tx = float.Parse(tx_);
                ty = float.Parse(ty_);
            }

            public ObjTexCood(float tx_, float ty_) {
                tx = tx_;
                ty = ty_;
            }
        }

        public class ObjMesh(string meshname) {
            public int ElementIndex;
            public DynamicArray<ObjVertex> Vertices = [];
            public DynamicArray<int> Indices = [];
            public string TexturePath = "Textures/NoneTexture"; //默认位置
            public string MeshName = meshname;
            public Matrix? MeshMatrix;

            public BoundingBox CalculateBoundingBox() {
                List<Vector3> vectors = [];
                for (int i = 0; i < Vertices.Count; i++) {
                    vectors.Add(new Vector3(Vertices[i].position.x, Vertices[i].position.y, Vertices[i].position.z));
                }
                return new BoundingBox(vectors);
            }

            public List<ObjMesh> ChildMeshes = [];
        }

        public static ObjModel Load(Stream stream) {
            Dictionary<string, ObjMesh> Meshes = [];
            Dictionary<string, string> TexturePaths = [];
            List<ObjPosition> objPositions = [];
            List<ObjTexCood> objTexCoods = [];
            List<ObjNormal> objNormals = [];
            using (stream) {
                StreamReader streamReader = new(stream);
                ObjMesh objMesh = null;
                string CurrentTkey = null;
                while (!streamReader.EndOfStream) {
                    string line = streamReader.ReadLine();
                    string[] spl = line.Split([(char)0x09, (char)0x20], StringSplitOptions.None);
                    switch (spl[0]) {
                        case "mtllib": {
                            MtllibStruct mtllibStruct = ContentManager.Get<MtllibStruct>(spl[1]);
                            TexturePaths = mtllibStruct.TexturePaths;
                            break;
                        }
                        case "o": {
                            if (Meshes.TryGetValue(spl[1], out ObjMesh mesh)) {
                                objMesh = mesh;
                            }
                            else {
                                objMesh = new ObjMesh(spl[1]);
                                Meshes.Add(spl[1], objMesh);
                            }
                            break;
                        }
                        case "v": {
                            objPositions.Add(new ObjPosition(spl[1], spl[2], spl[3]));
                            break;
                        }
                        case "vt": {
                            objTexCoods.Add(new ObjTexCood(spl[1], spl[2]));
                            break;
                        }
                        case "vn": {
                            objNormals.Add(new ObjNormal(spl[1], spl[2], spl[3]));
                            break;
                        }
                        case "usemtl": {
                            if (TexturePaths.TryGetValue(spl[1], out CurrentTkey)) {
                                //LoadingScreen.Info("Parse Obj mtl:" + CurrentTkey);
                            }
                            break;
                        }
                        case "f": {
                            if (string.IsNullOrEmpty(CurrentTkey)) {
                                CurrentTkey = "Textures/NoneTexture";
                            }
                            objMesh.TexturePath = CurrentTkey;
                            int SideCount = spl.Length - 1;
                            if (SideCount != 3) {
                                throw new Exception("模型必须为三角面");
                            }
                            int i = 0;
                            int startCount = objMesh.Vertices.Count;
                            while (++i < spl.Length) {
                                string[] param = spl[i].Split(['/'], StringSplitOptions.None);
                                if (param.Length != 3) {
                                    throw new Exception("面参数错误");
                                }
                                int pa = int.Parse(param[0]); //顶点索引
                                int pb = int.Parse(param[1]); //纹理索引
                                int pc = int.Parse(param[2]); //法线索引
                                ObjPosition objPosition = objPositions[pa - 1];
                                ObjTexCood texCood = objTexCoods[pb - 1];
                                ObjNormal objNormal = objNormals[pc - 1];
                                int face = CellFace.Vector3ToFace(new Vector3(objNormal.x, objNormal.y, objNormal.z));
                                objMesh.Indices.Add(startCount + FaceMap[face][i - 1]);
                                objMesh.Vertices.Add(new ObjVertex { position = objPosition, objNormal = objNormal, texCood = texCood });
                            }
                            break;
                        }
                    }
                }
            }
            return ObjMeshesToModel<ObjModel>(Meshes);
        }

        public static void AppendMesh(Model model, ModelBone rootBone, string texturepath, ObjMesh objMesh) {
            ModelBone modelBone = model.NewBone(objMesh.MeshName, objMesh.MeshMatrix ?? Matrix.Identity, rootBone);
            if (objMesh.Vertices.Count > 0) {
                ModelMesh mesh = model.NewMesh(objMesh.MeshName, modelBone, objMesh.CalculateBoundingBox());
                VertexBuffer vertexBuffer = new(
                    new VertexDeclaration(
                        new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position),
                        new VertexElement(12, VertexElementFormat.Vector3, VertexElementSemantic.Normal),
                        new VertexElement(24, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate)
                    ),
                    objMesh.Vertices.Count
                );
                MemoryStream stream1 = new();
                MemoryStream stream2 = new();
                BinaryWriter binaryWriter1 = new(stream1);
                BinaryWriter binaryWriter2 = new(stream2);
                for (int i = 0; i < objMesh.Vertices.Count; i++) {
                    ObjVertex objVertex = objMesh.Vertices[i];
                    binaryWriter1.Write(objVertex.position.x);
                    binaryWriter1.Write(objVertex.position.y);
                    binaryWriter1.Write(objVertex.position.z);
                    binaryWriter1.Write(objVertex.objNormal.x);
                    binaryWriter1.Write(objVertex.objNormal.y);
                    binaryWriter1.Write(objVertex.objNormal.z);
                    binaryWriter1.Write(objVertex.texCood.tx);
                    binaryWriter1.Write(objVertex.texCood.ty);
                }
                for (int i = 0; i < objMesh.Indices.Count; i++) {
                    binaryWriter2.Write(objMesh.Indices[i]);
                }
                byte[] vs = stream1.ToArray();
                byte[] ins = stream2.ToArray();
                stream1.Close();
                stream2.Close();
                vertexBuffer.SetData(objMesh.Vertices.Array, 0, objMesh.Vertices.Count);
                vertexBuffer.Tag = vs;
                IndexBuffer indexBuffer = new(IndexFormat.ThirtyTwoBits, objMesh.Indices.Count);
                indexBuffer.SetData(objMesh.Indices.Array, 0, objMesh.Indices.Count);
                indexBuffer.Tag = ins;
                ModelMeshPart modelMeshPart = mesh.NewMeshPart(vertexBuffer, indexBuffer, 0, objMesh.Indices.Count, objMesh.CalculateBoundingBox());
                modelMeshPart.TexturePath = objMesh.TexturePath;
                model.AddMesh(mesh);
            }
            foreach (ObjMesh objMesh1 in objMesh.ChildMeshes) {
                AppendMesh(model, modelBone, objMesh1.TexturePath, objMesh1);
            }
        }

        public static T ObjMeshesToModel<T>(Dictionary<string, ObjMesh> Meshes) where T : class {
            Type ctype = typeof(T);
            if (!ctype.IsSubclassOf(typeof(Model))) {
                throw new Exception($"不能将{ctype.Name}转换为Model类型");
            }
#pragma warning disable IL2087
            object iobj = Activator.CreateInstance(ctype);
#pragma warning restore IL2087
            Model Model = iobj as Model;
            ModelBone rootBone = Model.NewBone("Object", Matrix.Identity, null);
            foreach (KeyValuePair<string, ObjMesh> c in Meshes) {
                AppendMesh(Model, rootBone, c.Key, c.Value);
            }
            return iobj as T;
        }
    }

    public class ObjModel : Model { }
}