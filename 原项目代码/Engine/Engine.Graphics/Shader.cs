using Silk.NET.OpenGLES;
using System.Text;
using System.Xml.Linq;

namespace Engine.Graphics {
    public class Shader : GraphicsResource {
        public Dictionary<string, ShaderParameter> m_parametersByName;
        public ShaderParameter[] m_parameters;
        public string m_vertexShaderCode;
        public string m_pixelShaderCode;
        public ShaderMacro[] m_shaderMacros;
        public struct ShaderAttributeData {
            public string Semantic;

            public int Location;
        }

        public struct VertexAttributeData {
            public int Size;

            public VertexAttribPointerType Type;

            public bool Normalize;

            public int Offset;
        }

        public int m_program;
        public int m_vertexShader;
        public int m_pixelShader;
        public Dictionary<VertexDeclaration, VertexAttributeData[]> m_vertexAttributeDataByDeclaration = [];
        public List<ShaderAttributeData> m_shaderAttributeData = [];
        public ShaderParameter m_glymulParameter;

        public string DebugName {
            get {
                return string.Empty;
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
                // For Direct3D backend
            }
        }

        public virtual ShaderParameter GetParameter(string name, bool allowNull = false) =>
            m_parametersByName.TryGetValue(name, out ShaderParameter value) ? value :
            allowNull ? new ShaderParameter("null", ShaderParameterType.Null) :
            throw new InvalidOperationException($"Parameter \"{name}\" not found.");

        public override int GetGpuMemoryUsage() => 16384;

        public virtual void PrepareForDrawingOverride() { }

        public virtual void InitializeShader(string vertexShaderCode, string pixelShaderCode, ShaderMacro[] shaderMacros) {
            ArgumentNullException.ThrowIfNull(vertexShaderCode);
            ArgumentNullException.ThrowIfNull(pixelShaderCode);
            ArgumentNullException.ThrowIfNull(shaderMacros);
            m_vertexShaderCode = vertexShaderCode;
            m_pixelShaderCode = pixelShaderCode;
            m_shaderMacros = (ShaderMacro[])shaderMacros.Clone();
        }

        public object Tag { get; set; }

        public ReadOnlyList<ShaderParameter> Parameters => new(m_parameters);

        public virtual void Construct(string vertexShaderCode, string pixelShaderCode, params ShaderMacro[] shaderMacros) {
            try {
                InitializeShader(vertexShaderCode, pixelShaderCode, shaderMacros);
                CompileShaders();
            }
            catch {
                Dispose();
                throw;
            }
        }

        public Shader() { }

        /// <summary>
        /// 从已链接的 program handle 创建 Shader（供 ShaderCache 使用）
        /// </summary>
        public Shader(uint programHandle) {
            m_program = (int)programHandle;
            m_vertexShader = 0;
            m_pixelShader = 0;
            CacheUniformParameters();
            m_glymulParameter = GetParameter("u_glymul", true);
        }

        public Shader(string vertexShaderCode, string pixelShaderCode, params ShaderMacro[] shaderMacros) {
            Construct(vertexShaderCode, pixelShaderCode, shaderMacros);
        }

        public override void Dispose() {
            base.Dispose();
            DeleteShaders();
        }

        public virtual void PrepareForDrawing() {
            m_glymulParameter.SetValue(Display.RenderTarget != null ? -1f : 1f);
            PrepareForDrawingOverride();
        }

        public virtual VertexAttributeData[] GetVertexAttribData(VertexDeclaration vertexDeclaration) {
            if (!m_vertexAttributeDataByDeclaration.TryGetValue(vertexDeclaration, out VertexAttributeData[] value)) {
                value = new VertexAttributeData[8];
                foreach (ShaderAttributeData shaderAttributeDatum in m_shaderAttributeData) {
                    VertexElement vertexElement = null;
                    for (int i = 0; i < vertexDeclaration.m_elements.Length; i++) {
                        if (vertexDeclaration.m_elements[i].Semantic == shaderAttributeDatum.Semantic) {
                            vertexElement = vertexDeclaration.m_elements[i];
                            break;
                        }
                    }
                    if (!(vertexElement != null)) {
                        throw new InvalidOperationException($"VertexElement not found for shader attribute \"{shaderAttributeDatum.Semantic}\".");
                    }
                    value[shaderAttributeDatum.Location] = new VertexAttributeData {
                        Size = vertexElement.Format.GetElementsCount(), Offset = vertexElement.Offset
                    };
                    GLWrapper.TranslateVertexElementFormat(
                        vertexElement.Format,
                        out value[shaderAttributeDatum.Location].Type,
                        out value[shaderAttributeDatum.Location].Normalize
                    );
                }
                m_vertexAttributeDataByDeclaration.Add(vertexDeclaration, value);
            }
            return value;
        }

        public static void ParseShaderMetadata(string shaderCode,
            Dictionary<string, string> semanticsByAttribute,
            Dictionary<string, string> samplersByTexture) {
            string[] array = shaderCode.Split('\n');
            for (int i = 0; i < array.Length; i++) {
                try {
                    string text = array[i];
                    text = text.Trim();
                    if (text.StartsWith("//")) {
                        text = text.Substring(2).TrimStart();
                        if (text.StartsWith('<')
                            && text.EndsWith("/>")) {
                            XElement xElement = XElement.Parse(text);
                            if (xElement.Name == "Semantic") {
                                XAttribute attribute = xElement.Attribute("Attribute");
                                if (attribute == null) {
                                    throw new InvalidOperationException("Missing \"Attribute\" attribute in shader metadata.");
                                }
                                XAttribute name = xElement.Attribute("Name");
                                if (name == null) {
                                    throw new InvalidOperationException("Missing \"Name\" attribute in shader metadata.");
                                }
                                semanticsByAttribute.Add(attribute.Value, name.Value);
                            }
                            else {
                                if (!(xElement.Name == "Sampler")) {
                                    throw new InvalidOperationException("Unrecognized shader metadata node.");
                                }
                                XAttribute texture = xElement.Attribute("Texture");
                                if (texture == null) {
                                    throw new InvalidOperationException("Missing \"Texture\" attribute in shader metadata.");
                                }
                                XAttribute name = xElement.Attribute("Name");
                                if (name == null) {
                                    throw new InvalidOperationException("Missing \"Name\" attribute in shader metadata.");
                                }
                                samplersByTexture.Add(texture.Value, name.Value);
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    throw new InvalidOperationException($"Error in shader metadata, line {i + 1}. {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 提取着色器中的版本指令信息，跳过元数据注释
        /// </summary>
        static (int version, bool isEs, int versionLineIndex) ExtractVersionInfo(string shaderCode) {
            string[] lines = shaderCode.Split('\n');
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i].Trim();
                // 跳过元数据注释
                if (line.StartsWith("// <")
                    || line.StartsWith("//<")) {
                    continue;
                }
                // 跳过普通注释和空行
                if (line.StartsWith("//")
                    || string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                // 找到 #version 指令
                if (line.StartsWith("#version ")) {
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) {
                        int version = int.Parse(parts[1]);
                        bool isEs = parts.Length >= 3 && parts[2] == "es";
                        return (version, isEs, i);
                    }
                }
                // 遇到非版本指令，停止搜索
                break;
            }
            return (100, false, -1);
        }

        public virtual string PrependShaderMacros(string shaderCode, ShaderMacro[] shaderMacros, bool isVertexShader) {
            var (version, isEs, versionLineIndex) = ExtractVersionInfo(shaderCode);

            StringBuilder str = new();

            // 添加版本指令（必须在最前面）
            if (version >= 300 || isEs) {
                str.AppendLine($"#version {version} es");
            }
            else {
                str.AppendLine($"#version {version}");
            }

            str.AppendLine("#define GLSL");

            if (isVertexShader) {
                if (!Display.UseReducedZRange) {
                    str.AppendLine("#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul; gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;");
                }
                else {
                    str.AppendLine("#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul;");
                }
                str.AppendLine("uniform float u_glymul;");
            }

            foreach (ShaderMacro shaderMacro in shaderMacros) {
                str.AppendLine($"#define {shaderMacro.Name} {shaderMacro.Value}");
            }

            str.AppendLine("#line 1");

            // 移除原始的版本指令行
            if (versionLineIndex >= 0) {
                string[] lines = shaderCode.Split('\n');
                lines[versionLineIndex] = "// " + lines[versionLineIndex];
                shaderCode = string.Join("\n", lines);
            }

            return str.ToString() + shaderCode;
        }

        public override void HandleDeviceLost() {
            DeleteShaders();
        }

        public override void HandleDeviceReset() {
            CompileShaders();
        }

        public virtual void CompileShaders() {
            DeleteShaders();
            Dictionary<string, string> dictionary = [];
            Dictionary<string, string> dictionary2 = [];
            ParseShaderMetadata(m_vertexShaderCode, dictionary, dictionary2);
            ParseShaderMetadata(m_pixelShaderCode, dictionary, dictionary2);
            string @string = PrependShaderMacros(m_vertexShaderCode, m_shaderMacros, true);
            string string2 = PrependShaderMacros(m_pixelShaderCode, m_shaderMacros, false);
            uint vertexShader = GLWrapper.GL.CreateShader(ShaderType.VertexShader);
            m_vertexShader = (int)vertexShader;
            GLWrapper.GL.ShaderSource(vertexShader, @string);
            GLWrapper.GL.CompileShader(vertexShader);
            GLWrapper.GL.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int @params);
            if (@params != 1) {
                string shaderInfoLog = GLWrapper.GL.GetShaderInfoLog(vertexShader);
                throw new InvalidOperationException($"Error compiling vertex shader.\n{shaderInfoLog}");
            }
            uint pixelShader = GLWrapper.GL.CreateShader(ShaderType.FragmentShader);
            m_pixelShader = (int)pixelShader;
            GLWrapper.GL.ShaderSource(pixelShader, string2);
            GLWrapper.GL.CompileShader(pixelShader);
            GLWrapper.GL.GetShader(pixelShader, ShaderParameterName.CompileStatus, out int params2);
            if (params2 != 1) {
                string shaderInfoLog2 = GLWrapper.GL.GetShaderInfoLog(pixelShader);
                throw new InvalidOperationException($"Error compiling pixel shader.\n{shaderInfoLog2}");
            }
            uint program = GLWrapper.GL.CreateProgram();
            m_program = (int)program;
            GLWrapper.GL.AttachShader(program, vertexShader);
            GLWrapper.GL.AttachShader(program, pixelShader);
            GLWrapper.GL.LinkProgram(program);
            GLWrapper.GL.GetProgram(program, ProgramPropertyARB.LinkStatus, out int params3);
            if (params3 != 1) {
                string programInfoLog = GLWrapper.GL.GetProgramInfoLog(program);
                throw new InvalidOperationException($"Error linking program.\n{programInfoLog}");
            }
            GLWrapper.GL.GetProgram(program, ProgramPropertyARB.ActiveAttributes, out int params4);
            for (int i = 0; i < params4; i++) {
                GLWrapper.GL.GetActiveAttrib(
                    program,
                    (uint)i,
                    256u,
                    out uint _,
                    out int _,
                    out AttributeType _,
                    out string stringBuilder
                );
                int attribLocation = GLWrapper.GL.GetAttribLocation(program, stringBuilder);
                if (!dictionary.TryGetValue(stringBuilder, out string value)) {
                    throw new InvalidOperationException($"Attribute \"{stringBuilder}\" has no semantic defined in shader metadata.");
                }
                m_shaderAttributeData.Add(new ShaderAttributeData { Location = attribLocation, Semantic = value });
            }
            GLWrapper.GL.GetProgram(program, ProgramPropertyARB.ActiveUniforms, out int params5);
            List<ShaderParameter> list = [];
            Dictionary<string, ShaderParameter> dictionary3 = [];
            for (int j = 0; j < params5; j++) {
                GLWrapper.GL.GetActiveUniform(
                    program,
                    (uint)j,
                    256u,
                    out uint _,
                    out int size2,
                    out UniformType type2,
                    out string stringBuilder2
                );
                int uniformLocation = GLWrapper.GL.GetUniformLocation(program, stringBuilder2);
                ShaderParameterType shaderParameterType = GLWrapper.TranslateActiveUniformType(type2);
                int num = stringBuilder2.IndexOf('[');
                if (num >= 0) {
                    stringBuilder2 = stringBuilder2.Remove(num, stringBuilder2.Length - num);
                }
                ShaderParameter shaderParameter = new(this, stringBuilder2, shaderParameterType, size2) { Location = uniformLocation };
                dictionary3.Add(shaderParameter.Name, shaderParameter);
                list.Add(shaderParameter);
                if (shaderParameterType == ShaderParameterType.Texture2D
                    || shaderParameterType == ShaderParameterType.Texture2DArray) {
                    if (!dictionary2.TryGetValue(shaderParameter.Name, out string value2)) {
                        throw new InvalidOperationException($"Texture \"{shaderParameter.Name}\" has no sampler defined in shader metadata.");
                    }
                    ShaderParameter shaderParameter2 = new(this, value2, ShaderParameterType.Sampler2D, 1) { Location = int.MaxValue };
                    dictionary3.Add(value2, shaderParameter2);
                    list.Add(shaderParameter2);
                }
            }
            if (m_parameters != null) {
                foreach (KeyValuePair<string, ShaderParameter> item in dictionary3) {
                    if (m_parametersByName.TryGetValue(item.Key, out ShaderParameter value3)) {
                        value3.Location = item.Value.Location;
                    }
                }
                ShaderParameter[] parameters = m_parameters;
                for (int k = 0; k < parameters.Length; k++) {
                    parameters[k].IsChanged = true;
                }
            }
            else {
                m_parameters = list.ToArray();
                m_parametersByName = dictionary3;
            }
            m_glymulParameter = GetParameter("u_glymul", true);
        }

        public virtual void DeleteShaders() {
            uint vertexShader = (uint)m_vertexShader;
            uint pixelShader = (uint)m_pixelShader;
            if (m_program != 0) {
                uint program = (uint)m_program;
                if (m_vertexShader != 0) {
                    GLWrapper.GL.DetachShader(program, vertexShader);
                }
                if (m_pixelShader != 0) {
                    GLWrapper.GL.DetachShader(program, pixelShader);
                }
                GLWrapper.DeleteProgram(m_program);
                m_program = 0;
            }
            if (m_vertexShader != 0) {
                GLWrapper.GL.DeleteShader(vertexShader);
                m_vertexShader = 0;
            }
            if (m_pixelShader != 0) {
                GLWrapper.GL.DeleteShader(pixelShader);
                m_pixelShader = 0;
            }
        }

        /// <summary>
        /// 缓存所有 active uniform 参数（从 GL program 查询）
        /// </summary>
        internal void CacheUniformParameters() {
            if (m_program == 0) {
                return;
            }

            GLWrapper.GL.GetProgram((uint)m_program, ProgramPropertyARB.ActiveUniforms, out int uniformCount);

            List<ShaderParameter> list = [];
            Dictionary<string, ShaderParameter> dict = [];

            for (uint i = 0; i < uniformCount; i++) {
                GLWrapper.GL.GetActiveUniform(
                    (uint)m_program,
                    i,
                    256u,
                    out _,
                    out int size,
                    out UniformType type,
                    out string name
                );

                int location = GLWrapper.GL.GetUniformLocation((uint)m_program, name);
                ShaderParameterType paramType = GLWrapper.TranslateActiveUniformType(type);

                // 处理数组 uniform
                int bracketIndex = name.IndexOf('[');
                if (bracketIndex > 0) {
                    name = name.Substring(0, bracketIndex);
                }

                ShaderParameter param = new(this, name, paramType, size) { Location = location };
                if (!dict.ContainsKey(name)) {
                    dict.Add(name, param);
                    list.Add(param);
                }
            }

            m_parameters = list.ToArray();
            m_parametersByName = dict;
        }
    }
}
