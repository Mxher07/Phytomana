using Engine;
using Engine.Graphics;

namespace Game {
    /**
     * 此处基础坐标系为YZX
     */
    public class JsonModelReader {
        public static Dictionary<string, List<Vector3>> FacesDic = [];
        public static Dictionary<string, Vector3> NormalDic = [];
        public static Dictionary<string, List<int>> FacedirecDic = [];
        public static Dictionary<float, List<int>> TextureRotate = [];

        static JsonModelReader() {
            FacesDic.Add("north", [Vector3.UnitX, Vector3.Zero, Vector3.UnitY, new Vector3(1, 1, 0)]);
            FacesDic.Add("south", [new Vector3(1, 0, 1), Vector3.UnitZ, new Vector3(0, 1, 1), new Vector3(1, 1, 1)]);
            FacesDic.Add("east", [new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0)]);
            FacesDic.Add("west", [Vector3.Zero, Vector3.UnitZ, new Vector3(0, 1, 1), Vector3.UnitY]);
            FacesDic.Add("up", [Vector3.UnitY, new Vector3(0, 1, 1), Vector3.One, new Vector3(1, 1, 0)]);
            FacesDic.Add("down", [Vector3.Zero, Vector3.UnitZ, new Vector3(1, 0, 1), new Vector3(1, 0, 0)]);
            NormalDic.Add("north", new Vector3(0, 0, -1));
            NormalDic.Add("south", new Vector3(0, 0, 1));
            NormalDic.Add("east", new Vector3(1, 0, 0));
            NormalDic.Add("west", new Vector3(-1, 0, 0));
            NormalDic.Add("up", new Vector3(0, 1, 0));
            NormalDic.Add("down", new Vector3(0, -1, 0));
            FacedirecDic.Add("north", [0, 2, 1, 0, 3, 2]); //逆
            FacedirecDic.Add("west", [0, 2, 1, 0, 3, 2]); //逆
            FacedirecDic.Add("up", [0, 2, 1, 0, 3, 2]); //逆
            FacedirecDic.Add("south", [0, 1, 2, 0, 2, 3]); //顺
            FacedirecDic.Add("east", [0, 1, 2, 0, 2, 3]); //顺
            FacedirecDic.Add("down", [0, 1, 2, 0, 2, 3]); //顺
            TextureRotate.Add(
                0f,
                [
                    0,
                    3,
                    2,
                    3,
                    2,
                    1,
                    0,
                    1
                ]
            );
            TextureRotate.Add(
                90f,
                [
                    0,
                    1,
                    0,
                    3,
                    2,
                    3,
                    2,
                    1
                ]
            );
            TextureRotate.Add(
                180f,
                [
                    2,
                    1,
                    0,
                    1,
                    0,
                    3,
                    2,
                    3
                ]
            );
            TextureRotate.Add(
                270f,
                [
                    2,
                    3,
                    2,
                    1,
                    0,
                    1,
                    0,
                    3
                ]
            );
        }

        public static float ObjConvertFloat(object obj) {
            if (obj is double v) {
                return (float)v;
            }
            //else if (obj is int) return (float)(int)obj;
            if (obj is long v1) {
                return v1;
            }
            throw new Exception($"错误的数据转换，不能将{obj.GetType().Name}转换为float");
        }

        public static JsonModel Load(Stream stream) => throw new Exception("很抱歉，JsonModel功能暂时下线");
        /*
            Dictionary<string, ObjModelReader.ObjMesh> Meshes = [];
            Vector3 FirstPersonOffset = Vector3.One;
            Vector3 FirstPersonRotation = Vector3.Zero;
            Vector3 FirstPersonScale = Vector3.One;
            Vector3 InHandOffset = Vector3.One;
            Vector3 InHandRotation = Vector3.Zero;
            Vector3 InHandScale = Vector3.One;
            string parent = string.Empty;
            JsonElement jsonObj = JsonDocument.Parse(new StreamReader(stream).ReadToEnd()).RootElement;
            Vector2 textureSize = Vector2.Zero;
            Dictionary<string, string> texturemap = [];
            if (jsonObj.TryGetProperty("display", out JsonElement obj13) && obj13.ValueKind == JsonValueKind.Object)
            {
                if (obj13.TryGetProperty("thirdperson_righthand", out JsonElement jobj1) && jobj1.ValueKind == JsonValueKind.Object)
                {
                    if (jobj1.TryGetProperty("rotation", out JsonElement jsonArray1) && jsonArray1.ValueKind == JsonValueKind.Array)
                    {
                        InHandRotation = new Vector3(jsonArray1[0].GetSingle(), jsonArray1[1].GetSingle(), jsonArray1[2].GetSingle());
                    }
                    if (jobj1.TryGetProperty("translation", out JsonElement jsonArray2) && jsonArray2.ValueKind == JsonValueKind.Array)
                    {
                        InHandOffset = new Vector3(jsonArray2[0].GetSingle(), jsonArray2[1].GetSingle(), jsonArray2[2].GetSingle());
                    }
                    if (jobj1.TryGetProperty("scale", out JsonElement jsonArray3) && jsonArray3.ValueKind == JsonValueKind.Array)
                    {
                        InHandScale = new Vector3(jsonArray3[0].GetSingle(), jsonArray3[1].GetSingle(), jsonArray3[2].GetSingle());
                    }
                }
                if (obj13.TryGetProperty("firstperson_righthand", out JsonElement jobj2) && jobj2.ValueKind == JsonValueKind.Object)
                {
                    if (jobj2.TryGetProperty("rotation", out JsonElement jsonArray1) && jsonArray1.ValueKind == JsonValueKind.Array)
                    {
                        FirstPersonRotation = new Vector3(jsonArray1[0].GetSingle(), jsonArray1[1].GetSingle(), jsonArray1[2].GetSingle());
                    }
                    if (jobj2.TryGetProperty("translation", out JsonElement jsonArray2) && jsonArray2.ValueKind == JsonValueKind.Array)
                    {
                        FirstPersonOffset = new Vector3(jsonArray2[0].GetSingle(), jsonArray2[1].GetSingle(), jsonArray2[2].GetSingle());
                    }
                    if (jobj2.TryGetProperty("scale", out JsonElement jsonArray3) && jsonArray3.ValueKind == JsonValueKind.Array)
                    {
                        FirstPersonScale = new Vector3(jsonArray3[0].GetSingle(), jsonArray3[1].GetSingle(), jsonArray3[2].GetSingle());
                    }
                }
            }
            if (jsonObj.TryGetProperty("parent", out JsonElement obj12) && obj12.ValueKind == JsonValueKind.String)
            {
                parent = obj12.GetString();
            }
            if (jsonObj.TryGetProperty("textures", out JsonElement jobj5) && jobj5.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty item in jobj5.EnumerateObject())
                {
                    texturemap.Add(item.Name, item.Value.GetString());
                }
            }
            if (jsonObj.TryGetProperty("texture_size", out JsonElement array1) && array1.ValueKind == JsonValueKind.Array)
            {
                textureSize = new Vector2(array1[0].GetSingle(), array1[1].GetSingle());
            }
            if (jsonObj.TryGetProperty("elements", out JsonElement array2) && array2.ValueKind == JsonValueKind.Array)
            {
                int l = 0;
                foreach (JsonElement jobj in array2.EnumerateArray())
                {
                    JsonElement from = jobj.GetProperty("from");
                    JsonElement to = jobj.GetProperty("to");
                    string name = "undefined";
                    if (jobj.TryGetProperty("name", out JsonElement obj8) && obj8.ValueKind == JsonValueKind.String)
                    {
                        name = obj8.GetString();
                    }
                    if (!Meshes.TryGetValue(name, out ObjModelReader.ObjMesh objMesh))
                    {
                        objMesh = new ObjModelReader.ObjMesh(name);
                        objMesh.ElementIndex = l;
                        Meshes.Add(name, objMesh);
                    }
                    //if (jobj.TryGetProperty("rotation", out JsonElement jobj8) && jobj8.ValueKind == JsonValueKind.Object)
                    //{ //处理模型旋转
                        //JsonElement ori = jobj8.GetProperty("origin");
                        //float ang = jobj8.GetProperty("angle").GetSingle();
                      //objMesh.MeshMatrix = Matrix.CreateFromAxisAngle(new Vector3(ObjConvertFloat(ori[0]) / 16f, ObjConvertFloat(ori[1]) / 16f, ObjConvertFloat(ori[2]) / 16f), ang);
                    //}
                    Vector3 start = new(from[0].GetSingle(), from[1].GetSingle(), from[2].GetSingle());
                    Vector3 end = new(to[0].GetSingle(), to[1].GetSingle(), to[2].GetSingle());
                    Matrix transform = Matrix.CreateScale(end.X - start.X, end.Y - start.Y, end.Z - start.Z) * Matrix.CreateTranslation(start.X, start.Y, start.Z) * Matrix.CreateScale(0.0625f);//基础缩放变换
                    if (jobj.TryGetProperty("faces", out JsonElement jsonobj2) && jsonobj2.ValueKind == JsonValueKind.Object)
                    {//每个面，开始生成六个面的顶点数据
                        foreach (JsonProperty jobj2 in jsonobj2.EnumerateObject())
                        {
                            ObjModelReader.ObjMesh childMesh = new(jobj2.Name);
                            List<Vector3> vectors = FacesDic[jobj2.Name];//预取出四个面的点
                            JsonElement jobj3 = jobj2.Value;
                            float rotate = 0f;
                            string facename = jobj2.Name;
                            float[] uvs = new float[4];
                            List<Vector2> TexCoords = [];
                            if (jobj3.TryGetProperty("rotation", out JsonElement obj6) && obj6.ValueKind == JsonValueKind.Number)
                            {//处理uv旋转数据
                                rotate = obj6.GetSingle();
                            }
                            if (jobj3.TryGetProperty("uv", out JsonElement uvarr) && uvarr.ValueKind == JsonValueKind.Array)
                            {//处理uv坐标数据
                                int k = 0;
                                foreach (JsonElement element in uvarr.EnumerateArray())
                                {
                                    uvs[k] = element.GetSingle() / 16f;
                                    k++;
                                }
                                Vector2 center = (new Vector2(uvs[2] - uvs[0], uvs[3] - uvs[1]) / 2f) + new Vector2(uvs[0], uvs[1]);//中心点
                                TexCoords.Add(new Vector2(uvs[TextureRotate[rotate][0]], uvs[TextureRotate[rotate][1]]));//x1,y2
                                TexCoords.Add(new Vector2(uvs[TextureRotate[rotate][2]], uvs[TextureRotate[rotate][3]]));//x1,y2
                                TexCoords.Add(new Vector2(uvs[TextureRotate[rotate][4]], uvs[TextureRotate[rotate][5]]));//x1,y2
                                TexCoords.Add(new Vector2(uvs[TextureRotate[rotate][6]], uvs[TextureRotate[rotate][7]]));//x1,y2
                            }
                            if (jobj3.TryGetProperty("texture", out JsonElement obj5) && obj5.ValueKind == JsonValueKind.String)
                            {//处理贴图数据
                                if (texturemap.TryGetValue(obj5.GetString().Substring(1), out string path))
                                {
                                    childMesh.TexturePath = path;
                                }
                            }
                            ObjModelReader.ObjPosition[] ops = new ObjModelReader.ObjPosition[3];
                            ObjModelReader.ObjTexCood[] ots = new ObjModelReader.ObjTexCood[3];
                            ObjModelReader.ObjNormal[] ons = new ObjModelReader.ObjNormal[3];
                            //生成第一个三角面顶点
                            int c1 = FacedirecDic[facename][0];
                            int c2 = FacedirecDic[facename][1];
                            int c3 = FacedirecDic[facename][2];
                            Vector3 p1 = Vector3.Transform(vectors[c1], transform);
                            Vector3 p2 = Vector3.Transform(vectors[c2], transform);
                            Vector3 p3 = Vector3.Transform(vectors[c3], transform);
                            ops[0] = new ObjModelReader.ObjPosition(p1.X, p1.Y, p1.Z);
                            ops[1] = new ObjModelReader.ObjPosition(p2.X, p2.Y, p2.Z);
                            ops[2] = new ObjModelReader.ObjPosition(p3.X, p3.Y, p3.Z);
                            //生成第一个三角面的纹理坐标
                            Vector2 t1 = TexCoords[c1];
                            Vector2 t2 = TexCoords[c2];
                            Vector2 t3 = TexCoords[c3];
                            ots[0] = new ObjModelReader.ObjTexCood(t1.X, t1.Y);
                            ots[1] = new ObjModelReader.ObjTexCood(t2.X, t2.Y);
                            ots[2] = new ObjModelReader.ObjTexCood(t3.X, t3.Y);
                            //生成第一个三角面的顶点法线
                            //Vector3 normal = NormalDic[facename];
                            int startcount = childMesh.Vertices.Count;
                            childMesh.Indices.Add(startcount++);
                            childMesh.Indices.Add(startcount++);
                            childMesh.Indices.Add(startcount++);
                            childMesh.Vertices.Add(new ObjModelReader.ObjVertex { position = ops[0], objNormal = new ObjModelReader.ObjNormal(0, 0, 0), texCood = ots[0] });
                            childMesh.Vertices.Add(new ObjModelReader.ObjVertex { position = ops[1], objNormal = new ObjModelReader.ObjNormal(0, 0, 0), texCood = ots[1] });
                            childMesh.Vertices.Add(new ObjModelReader.ObjVertex { position = ops[2], objNormal = new ObjModelReader.ObjNormal(0, 0, 0), texCood = ots[2] });
                            //生成第二个三角面
                            c1 = FacedirecDic[facename][3];
                            c2 = FacedirecDic[facename][4];
                            c3 = FacedirecDic[facename][5];
                            p1 = Vector3.Transform(vectors[c1], transform);
                            p2 = Vector3.Transform(vectors[c2], transform);
                            p3 = Vector3.Transform(vectors[c3], transform);
                            ops[0] = new ObjModelReader.ObjPosition(p1.X, p1.Y, p1.Z);
                            ops[1] = new ObjModelReader.ObjPosition(p2.X, p2.Y, p2.Z);
                            ops[2] = new ObjModelReader.ObjPosition(p3.X, p3.Y, p3.Z);
                            //生成第二个三角面的纹理坐标
                            t1 = TexCoords[c1];
                            t2 = TexCoords[c2];
                            t3 = TexCoords[c3];
                            ots[0] = new ObjModelReader.ObjTexCood(t1.X, t1.Y);
                            ots[1] = new ObjModelReader.ObjTexCood(t2.X, t2.Y);
                            ots[2] = new ObjModelReader.ObjTexCood(t3.X, t3.Y);
                            //生成第二个三角面的顶点法线
                            childMesh.Indices.Add(startcount++);
                            childMesh.Indices.Add(startcount++);
                            childMesh.Indices.Add(startcount++);
                            childMesh.Vertices.Add(new ObjModelReader.ObjVertex { position = ops[0], objNormal = new ObjModelReader.ObjNormal(0, 0, 0), texCood = ots[0] });
                            childMesh.Vertices.Add(new ObjModelReader.ObjVertex { position = ops[1], objNormal = new ObjModelReader.ObjNormal(0, 0, 0), texCood = ots[1] });
                            childMesh.Vertices.Add(new ObjModelReader.ObjVertex { position = ops[2], objNormal = new ObjModelReader.ObjNormal(0, 0, 0), texCood = ots[2] });
                            objMesh.ChildMeshes.Add(childMesh);
                        }
                    }
                }
                l++;
            }
            List<ObjModelReader.ObjMesh> objMeshes = [];
            foreach (var c in Meshes)
            {
                objMeshes.Add(c.Value);
            }
            if (jsonObj.TryGetProperty("groups", out JsonElement jsonArray) && jsonArray.ValueKind == JsonValueKind.Array)
            {//解析groups
                Dictionary<string, ObjModelReader.ObjMesh> Meshes2 = [];
                int m = 0;
                foreach (JsonElement jobj10 in jsonArray.EnumerateArray())
                {
                    string jobj10name = m.ToString();
                    if (jobj10.TryGetProperty("name", out JsonElement jobj10name_) && jobj10name_.ValueKind == JsonValueKind.String) jobj10name = jobj10name_.GetString();
                    ObjModelReader.ObjMesh mesh = new(jobj10name);
                    if (jobj10.TryGetProperty("oringin", out JsonElement jarr2) && jarr2.ValueKind == JsonValueKind.Array)
                    {
                        Vector3 start = new Vector3(jarr2[0].GetSingle(), jarr2[1].GetSingle(), jarr2[2].GetSingle()) / 16f;
                        mesh.MeshMatrix = Matrix.CreateTranslation(start.X, start.Y, start.Z);
                    }
                    if (jobj10.TryGetProperty("children", out JsonElement jarr3) && jarr3.ValueKind == JsonValueKind.Array)
                    {
                        int i = 0;
                        foreach (JsonElement eleindex in jarr3.EnumerateArray())
                        {
                            mesh.ChildMeshes.Add(objMeshes.Find(xp => xp.ElementIndex == (int)eleindex.GetSingle()));
                            i++;
                        }
                    }
                    Meshes2.Add(jobj10name, mesh);
                    m++;
                }
                JsonModel jsonModel = ObjModelReader.ObjMeshesToModel<JsonModel>(Meshes2);
                //if (string.IsNullOrEmpty(parent) == false) try { jsonModel.ParentModel = ContentManager.Get<JsonModel>(parent); } catch { }
                jsonModel.InHandScale = InHandScale;
                jsonModel.InHandOffset = InHandOffset;
                jsonModel.InHandRotation = InHandRotation;
                jsonModel.FirstPersonOffset = FirstPersonOffset;
                jsonModel.FirstPersonScale = FirstPersonScale;
                jsonModel.FirstPersonRotation = FirstPersonRotation;
                return jsonModel;
            }

            JsonModel jsonModel2 = ObjModelReader.ObjMeshesToModel<JsonModel>(Meshes);
            //if (string.IsNullOrEmpty(parent) == false) try { jsonModel2.ParentModel = ContentManager.Get<JsonModel>(parent); } catch { }
            jsonModel2.InHandScale = InHandScale;
            jsonModel2.InHandOffset = InHandOffset;
            jsonModel2.InHandRotation = InHandRotation;
            jsonModel2.FirstPersonOffset = FirstPersonOffset;
            jsonModel2.FirstPersonScale = FirstPersonScale;
            jsonModel2.FirstPersonRotation = FirstPersonRotation;
            return jsonModel2;
            */
    }

    public class JsonModel : Model {
        public Model ParentModel;
        public string ParticleTexture;
        public Vector3 FirstPersonOffset;
        public Vector3 FirstPersonRotation;
        public Vector3 FirstPersonScale;
        public Vector3 InHandOffset;
        public Vector3 InHandRotation;
        public Vector3 InHandScale;
    }
}