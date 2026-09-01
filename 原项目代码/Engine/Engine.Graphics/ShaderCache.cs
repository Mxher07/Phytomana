using System.Text;
using System.Text.RegularExpressions;
using Silk.NET.OpenGLES;

namespace Engine.Graphics {
    /// <summary>
    /// 着色器缓存系统
    /// 支持从 Stream 加载着色器源码，#include 语法解析，着色器变体编译和缓存
    /// </summary>
    public static class ShaderCache {
        public static Dictionary<string, string> m_sources;
        public static Dictionary<int, uint> m_shaderObjectCache;
        public static Dictionary<string, Shader> m_programCache;

        static string s_effectiveCacheDir;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// 二进制缓存根目录（由调用者设置）
        /// 实际缓存路径 = 根目录/GPU平台子目录
        /// </summary>
        public static string CacheDirectory {
            get => field;
            set {
                field = value;
                s_effectiveCacheDir = null; // 重置，下次访问时重新计算
            }
        }

        /// <summary>
        /// 获取 GPU 平台相关的有效缓存目录
        /// 格式: {CacheDirectory}/{vendor}_{renderer_hash}
        /// </summary>
        public static string GetEffectiveCacheDirectory() {
            if (s_effectiveCacheDir != null) {
                return s_effectiveCacheDir;
            }
            if (string.IsNullOrEmpty(CacheDirectory)) {
                return null;
            }
            string vendor = GLWrapper.GL.GetStringS(StringName.Vendor) ?? "unknown";
            string renderer = GLWrapper.GL.GetStringS(StringName.Renderer) ?? "unknown";
            string version = GLWrapper.GL.GetStringS(StringName.Version) ?? "unknown";

            // 用 vendor + renderer + version 生成平台标识
            string platformId = $"{vendor}|{renderer}|{version}";
            int hash = ComputeHash(platformId);
            string safeVendor = new(vendor.Take(8).Where(char.IsLetterOrDigit).ToArray());
            s_effectiveCacheDir = Path.Combine(CacheDirectory, $"{safeVendor}_{hash:X8}");
            return s_effectiveCacheDir;
        }

        /// <summary>
        /// Attribute Location 绑定回调（在链接前调用）
        /// </summary>
        public static Action<uint> BindAttributeLocationsCallback { get; set; }

        /// <summary>
        /// GLSL version (default 300). Set to 310 for image load/store support.
        /// </summary>
        public static int GlslVersion { get; set; } = 300;

        /// <summary>
        /// Uniform Block 绑定回调（在链接后调用）
        /// </summary>
        public static Action<uint> BindUniformBlockBindingsCallback { get; set; }

        /// <summary>
        /// 初始化着色器缓存（应用启动时调用一次）
        /// </summary>
        public static void Initialize() {
            if (IsInitialized) {
                return;
            }
            m_sources = [];
            m_shaderObjectCache = [];
            m_programCache = [];
            IsInitialized = true;
        }

        /// <summary>
        /// 加载着色器源码
        /// </summary>
        /// <param name="source">着色器内容</param>
        /// <param name="shaderName">着色器名称（用于缓存键）</param>
        /// <param name="basePath">用于 #include 解析的相对路径前缀</param>
        public static void LoadShaderSource(string source, string shaderName, string basePath = null) {
            if (!IsInitialized) {
                throw new InvalidOperationException("ShaderCache not initialized. Call Initialize() first.");
            }
            m_sources[shaderName] = source;

            // 解析 #include
            ResolveIncludes(basePath);
        }

        /// <summary>
        /// 加载多个着色器源码
        /// </summary>
        /// <param name="shaders">着色器名称和内容的字典</param>
        /// <param name="basePath">用于 #include 解析的相对路径前缀</param>
        public static void LoadShaderSources(Dictionary<string, string> shaders, string basePath = null) {
            if (!IsInitialized) {
                throw new InvalidOperationException("ShaderCache not initialized. Call Initialize() first.");
            }
            foreach ((string name, string source) in shaders) {
                m_sources[name] = source;
            }
            ResolveIncludes(basePath);
        }

        /// <summary>
        /// 解析 #include 指令
        /// </summary>
        public static void ResolveIncludes(string basePath) {
            if (basePath != null) {
                basePath = basePath.Replace('\\', '/');
                if (!basePath.EndsWith("/")) {
                    basePath += "/";
                }
            }
            bool changed = true;
            while (changed) {
                changed = false;
                string[] keys = m_sources.Keys.ToArray();
                foreach (string key in keys) {
                    string src = m_sources[key];
                    MatchCollection matches = Regex.Matches(src, @"#include\s+<([^>]+)>");
                    foreach (Match match in matches) {
                        string includeName = match.Groups[1].Value;

                        // 如果已缓存，直接替换
                        if (m_sources.TryGetValue(includeName, out string includeSource)) {
                            src = src.Replace(match.Value, includeSource);
                            changed = true;
                        }
                        // 否则通过回调加载
                        else if (Storage.LoadContentStreamCallback != null
                            && basePath != null) {
                            string includePath = basePath + includeName;
                            Stream includeStream = Storage.LoadContentStreamCallback(includePath);
                            if (includeStream != null) {
                                using StreamReader includeReader = new(includeStream);
                                includeSource = includeReader.ReadToEnd();
                                m_sources[includeName] = includeSource;
                                src = src.Replace(match.Value, includeSource);
                                changed = true;
                            }
                        }
                    }
                    m_sources[key] = src;
                }
            }
        }

        /// <summary>
        /// 选择或编译着色器变体
        /// </summary>
        /// <param name="shaderName">着色器文件名</param>
        /// <param name="defines">ShaderDefines 对象</param>
        /// <returns>着色器 hash</returns>
        public static int SelectShader(string shaderName, ShaderDefines defines) {
            if (!IsInitialized) {
                throw new InvalidOperationException("ShaderCache not initialized. Call Initialize() first.");
            }
            if (!m_sources.TryGetValue(shaderName, out string src)) {
                throw new FileNotFoundException($"Shader source not found: {shaderName}");
            }
            bool isVert = shaderName.EndsWith(".vert");
            int hash = ComputeHash(shaderName) ^ (defines?.ComputeHash() ?? 0);

            // 检查缓存
            if (m_shaderObjectCache.ContainsKey(hash)) {
                return hash;
            }

            // 构建完整源码
            string definesStr = defines?.GetDefinesCodeWithoutVersion() ?? "";
            string fullSource = BuildFullSource(src, definesStr, isVert, shaderName);

            // 编译
            uint shader = CompileShader(isVert, fullSource, shaderName);
            m_shaderObjectCache[hash] = shader;
            return hash;
        }

        /// <summary>
        /// 构建完整着色器源码
        /// </summary>
        public static string BuildFullSource(string baseSource, string definesStr, bool isVertexShader, string shaderName) {
            StringBuilder sb = new(baseSource.Length + definesStr.Length + 256);

            // 处理 #version
            int versionIndex = baseSource.IndexOf("#version");
            if (versionIndex >= 0) {
                int lineEnd = baseSource.IndexOf('\n', versionIndex);
                if (lineEnd >= 0) {
                    sb.Append(baseSource, 0, lineEnd + 1);
                    baseSource = baseSource.Substring(lineEnd + 1);
                }
                else {
                    sb.AppendLine(baseSource);
                    baseSource = "";
                }
            }
            else {
                sb.AppendLine($"#version {GlslVersion} es");
            }

            // GLSL define
            sb.AppendLine("#define GLSL");

            // Vertex shader special
            if (isVertexShader) {
                sb.AppendLine("uniform float u_glymul;");
                if (Display.UseReducedZRange) {
                    sb.AppendLine("#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul;");
                }
                else {
                    sb.AppendLine("#define OPENGL_POSITION_FIX gl_Position.y *= u_glymul; gl_Position.z = 2.0 * gl_Position.z - gl_Position.w;");
                }
            }

            // Defines
            sb.Append(definesStr);
            sb.AppendLine("#line 1");

            // Base source
            sb.Append(baseSource);
            return sb.ToString();
        }

        /// <summary>
        /// 编译着色器
        /// </summary>
        public static uint CompileShader(bool isVert, string source, string shaderName) {
            ShaderType type = isVert ? ShaderType.VertexShader : ShaderType.FragmentShader;
            uint shader = GLWrapper.GL.CreateShader(type);
            GLWrapper.GL.ShaderSource(shader, source);
            GLWrapper.GL.CompileShader(shader);
            GLWrapper.GL.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0) {
                string log = GLWrapper.GL.GetShaderInfoLog(shader);
                GLWrapper.GL.DeleteShader(shader);
                throw new InvalidOperationException($"Shader compilation failed ({shaderName}): {log}");
            }
            return shader;
        }

        /// <summary>
        /// 尝试用预计算的 hash 获取着色器程序
        /// </summary>
        public static Shader TryGetShaderProgram(int vertexShaderHash, int fragmentShaderHash) {
            if (!IsInitialized) {
                return null;
            }
            string cacheKey = $"{vertexShaderHash},{fragmentShaderHash}";
            if (m_programCache.TryGetValue(cacheKey, out Shader program)) {
                return program;
            }

            // 检查着色器 hash 是否存在
            if (!m_shaderObjectCache.ContainsKey(vertexShaderHash)
                || !m_shaderObjectCache.ContainsKey(fragmentShaderHash)) {
                return null;
            }

            // 尝试从二进制缓存加载
            uint programHandle = TryLoadProgramBinary(cacheKey);
            if (programHandle == 0) {
                return null;
            }

            // 绑定 UBO
            BindUniformBlockBindingsCallback?.Invoke(programHandle);
            program = new Shader(programHandle);
            m_programCache[cacheKey] = program;
            return program;
        }

        /// <summary>
        /// 获取或链接着色器程序
        /// </summary>
        public static Shader GetShaderProgram(int vertexShaderHash, int fragmentShaderHash) {
            if (!IsInitialized) {
                throw new InvalidOperationException("ShaderCache not initialized. Call Initialize() first.");
            }
            string cacheKey = $"{vertexShaderHash},{fragmentShaderHash}";
            if (m_programCache.TryGetValue(cacheKey, out Shader program)) {
                return program;
            }

            // 尝试从二进制缓存加载
            uint programHandle = TryLoadProgramBinary(cacheKey);
            bool fromCache = programHandle != 0;
            if (!fromCache) {
                // 缓存加载失败，走编译链接流程
                if (!m_shaderObjectCache.TryGetValue(vertexShaderHash, out uint vertShader)) {
                    throw new InvalidOperationException($"Vertex shader not found: {vertexShaderHash}");
                }
                if (!m_shaderObjectCache.TryGetValue(fragmentShaderHash, out uint fragShader)) {
                    throw new InvalidOperationException($"Fragment shader not found: {fragmentShaderHash}");
                }
                programHandle = GLWrapper.GL.CreateProgram();
                GLWrapper.GL.AttachShader(programHandle, vertShader);
                GLWrapper.GL.AttachShader(programHandle, fragShader);

                // 绑定 attribute locations（必须在链接前）
                BindAttributeLocationsCallback?.Invoke(programHandle);
                GLWrapper.GL.LinkProgram(programHandle);
                GLWrapper.GL.GetProgram(programHandle, ProgramPropertyARB.LinkStatus, out int status);
                if (status == 0) {
                    string log = GLWrapper.GL.GetProgramInfoLog(programHandle);
                    throw new InvalidOperationException($"Program link failed: {log}");
                }

                // 链接后分离着色器（不再需要，避免内存泄漏）
                GLWrapper.GL.DetachShader(programHandle, vertShader);
                GLWrapper.GL.DetachShader(programHandle, fragShader);

                // 链接后绑定 UBO
                BindUniformBlockBindingsCallback?.Invoke(programHandle);

                // 保存二进制缓存
                SaveProgramBinary(programHandle, cacheKey);
            }
            else {
                // 从缓存加载成功，仍需绑定 UBO
                BindUniformBlockBindingsCallback?.Invoke(programHandle);
            }
            program = new Shader(programHandle);
            m_programCache[cacheKey] = program;
            return program;
        }

        /// <summary>
        /// 尝试从二进制缓存加载程序
        /// </summary>
        public static uint TryLoadProgramBinary(string cacheKey) {
            string dir = GetEffectiveCacheDirectory();
            if (dir == null) {
                return 0;
            }
            string cacheFile = Path.Combine(dir, $"{cacheKey}.bin");
            if (!File.Exists(cacheFile)) {
                return 0;
            }
            try {
                byte[] fileData = File.ReadAllBytes(cacheFile);
                if (fileData.Length < 4) {
                    return 0;
                }
                uint formatValue = BitConverter.ToUInt32(fileData, 0);
                int binaryLength = fileData.Length - 4;
                uint programHandle = GLWrapper.GL.CreateProgram();
                GLWrapper.GL.ProgramBinary(programHandle, (GLEnum)formatValue, fileData.AsSpan(4, binaryLength), (uint)binaryLength);
                GLWrapper.GL.GetProgram(programHandle, ProgramPropertyARB.LinkStatus, out int status);
                if (status == 0) {
                    GLWrapper.GL.DeleteProgram(programHandle);
                    return 0;
                }
                return programHandle;
            }
            catch {
                // 删除损坏的缓存文件
                try {
                    if (File.Exists(cacheFile)) {
                        File.Delete(cacheFile);
                    }
                }
                catch {
                    // ignored
                }
                return 0;
            }
        }

        /// <summary>
        /// 保存程序二进制到缓存
        /// </summary>
        public static unsafe void SaveProgramBinary(uint programHandle, string cacheKey) {
            string dir = GetEffectiveCacheDirectory();
            if (dir == null) {
                return;
            }
            try {
                if (!Storage.DirectoryExists(dir)) {
                    Storage.CreateDirectory(dir);
                }
                GLWrapper.GL.GetProgram(programHandle, ProgramPropertyARB.ProgramBinaryLength, out int binaryLength);
                if (binaryLength <= 0) {
                    return;
                }
                byte[] binary = new byte[binaryLength + 4];
                uint formatValue;
                fixed (byte* ptr = &binary[4]) {
                    GLWrapper.GL.GetProgramBinary(programHandle, (uint)binaryLength, out _, out GLEnum format, ptr);
                    formatValue = (uint)format;
                }
                BitConverter.TryWriteBytes(binary, formatValue);
                string cacheFile = Path.Combine(dir, $"{cacheKey}.bin");
                Storage.WriteAllBytes(cacheFile, binary);
            }
            catch {
                // 保存失败不影响正常流程
            }
        }

        /// <summary>
        /// 获取已解析的着色器源代码
        /// </summary>
        public static string GetSource(string shaderName) => m_sources.TryGetValue(shaderName, out string src) ? src : null;

        /// <summary>
        /// 计算字符串 hash
        /// </summary>
        public static int ComputeHash(string input) {
            unchecked {
                int hash = 17;
                foreach (char c in input) {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public static void Dispose() {
            if (!IsInitialized) {
                return;
            }

            // 删除着色器对象
            foreach (uint shader in m_shaderObjectCache.Values) {
                GLWrapper.GL.DeleteShader(shader);
            }
            m_shaderObjectCache.Clear();

            // 删除程序
            foreach (Shader program in m_programCache.Values) {
                program.Dispose();
            }
            m_programCache.Clear();
            m_sources.Clear();
            IsInitialized = false;
        }
    }
}