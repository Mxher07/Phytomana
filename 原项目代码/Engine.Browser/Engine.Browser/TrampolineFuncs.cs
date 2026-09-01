using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

namespace Engine.Browser {
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "OpenGL names")]
    public static unsafe class TrampolineFuncs {
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TrampolineFuncs))]
        public static void ApplyWorkaroundFixingInvocations() {
            // this function needs to be here, else the trampolines below get trimmed
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCullFace_t(TriangleFace mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFrontFace_t(FrontFaceDirection mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glHint_t(HintTarget target, HintMode mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glLineWidth_t(float width);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPointSize_t(float size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPolygonMode_t(GLEnum face, PolygonMode mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glScissor_t(int x, int y, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexParameterf_t(TextureTarget target, TextureParameterName pname, float param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexParameterfv_t(TextureTarget target, TextureParameterName pname, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexParameteri_t(TextureTarget target, TextureParameterName pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexParameteriv_t(TextureTarget target, TextureParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexImage1D_t(TextureTarget target,
            int level,
            int internalformat,
            int width,
            int border,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexImage2D_t(TextureTarget target,
            int level,
            int internalformat,
            int width,
            int height,
            int border,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawBuffer_t(DrawBufferMode buf);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClear_t(uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearColor_t(float red, float green, float blue, float alpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearStencil_t(int s);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearDepth_t(double depth);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glStencilMask_t(uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glColorMask_t(bool red, bool green, bool blue, bool alpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDepthMask_t(bool flag);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDisable_t(EnableCap cap);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glEnable_t(EnableCap cap);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFinish_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFlush_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlendFunc_t(BlendingFactor sfactor, BlendingFactor dfactor);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glLogicOp_t(LogicOp opcode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glStencilFunc_t(StencilFunction func, int @ref, uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glStencilOp_t(StencilOp fail, StencilOp zfail, StencilOp zpass);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDepthFunc_t(DepthFunction func);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPixelStoref_t(PixelStoreParameter pname, float param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPixelStorei_t(PixelStoreParameter pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glReadBuffer_t(ReadBufferMode src);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glReadPixels_t(int x,
            int y,
            int width,
            int height,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetBooleanv_t(GetPName pname, bool* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetDoublev_t(GetPName pname, double* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate ErrorCode glGetError_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetFloatv_t(GetPName pname, float* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetIntegerv_t(GetPName pname, int* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate byte* glGetString_t(StringName name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTexImage_t(TextureTarget target, int level, PixelFormat format, PixelType type, void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTexParameterfv_t(TextureTarget target, GetTextureParameter pname, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTexParameteriv_t(TextureTarget target, GetTextureParameter pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTexLevelParameterfv_t(TextureTarget target, int level, GetTextureParameter pname, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTexLevelParameteriv_t(TextureTarget target, int level, GetTextureParameter pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsEnabled_t(EnableCap cap);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDepthRange_t(double n, double f);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glViewport_t(int x, int y, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawArrays_t(PrimitiveType mode, int first, int count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawElements_t(PrimitiveType mode, int count, DrawElementsType type, void* indices);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPolygonOffset_t(float factor, float units);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyTexImage1D_t(TextureTarget target,
            int level,
            InternalFormat internalformat,
            int x,
            int y,
            int width,
            int border);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyTexImage2D_t(TextureTarget target,
            int level,
            InternalFormat internalformat,
            int x,
            int y,
            int width,
            int height,
            int border);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyTexSubImage1D_t(TextureTarget target, int level, int xoffset, int x, int y, int width);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyTexSubImage2D_t(TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int x,
            int y,
            int width,
            int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexSubImage1D_t(TextureTarget target,
            int level,
            int xoffset,
            int width,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexSubImage2D_t(TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int width,
            int height,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindTexture_t(TextureTarget target, uint texture);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteTextures_t(int n, uint* textures);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenTextures_t(int n, uint* textures);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsTexture_t(uint texture);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawRangeElements_t(PrimitiveType mode, uint start, uint end, int count, DrawElementsType type, void* indices);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexImage3D_t(TextureTarget target,
            int level,
            int internalformat,
            int width,
            int height,
            int depth,
            int border,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexSubImage3D_t(TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int width,
            int height,
            int depth,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyTexSubImage3D_t(TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int x,
            int y,
            int width,
            int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glActiveTexture_t(TextureUnit texture);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSampleCoverage_t(float value, bool invert);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompressedTexImage3D_t(TextureTarget target,
            int level,
            InternalFormat internalformat,
            int width,
            int height,
            int depth,
            int border,
            int imageSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompressedTexImage2D_t(TextureTarget target,
            int level,
            InternalFormat internalformat,
            int width,
            int height,
            int border,
            int imageSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompressedTexImage1D_t(TextureTarget target,
            int level,
            InternalFormat internalformat,
            int width,
            int border,
            int imageSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompressedTexSubImage3D_t(TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int width,
            int height,
            int depth,
            PixelFormat format,
            int imageSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompressedTexSubImage2D_t(TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int width,
            int height,
            PixelFormat format,
            int imageSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompressedTexSubImage1D_t(TextureTarget target,
            int level,
            int xoffset,
            int width,
            PixelFormat format,
            int imageSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetCompressedTexImage_t(TextureTarget target, int level, void* img);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlendFuncSeparate_t(BlendingFactor sfactorRGB,
            BlendingFactor dfactorRGB,
            BlendingFactor sfactorAlpha,
            BlendingFactor dfactorAlpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiDrawArrays_t(PrimitiveType mode, int* first, int* count, int drawcount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiDrawElements_t(PrimitiveType mode, int* count, DrawElementsType type, IntPtr indices, int drawcount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPointParameterf_t(uint pname, float param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPointParameterfv_t(uint pname, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPointParameteri_t(uint pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPointParameteriv_t(uint pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlendColor_t(float red, float green, float blue, float alpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlendEquation_t(BlendEquationModeEXT mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenQueries_t(int n, uint* ids);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteQueries_t(int n, uint* ids);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsQuery_t(uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBeginQuery_t(QueryTarget target, uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glEndQuery_t(QueryTarget target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryiv_t(QueryTarget target, QueryParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryObjectiv_t(uint id, QueryObjectParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryObjectuiv_t(uint id, QueryObjectParameterName pname, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindBuffer_t(BufferTargetARB target, uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteBuffers_t(int n, uint* buffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenBuffers_t(int n, uint* buffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsBuffer_t(uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBufferData_t(BufferTargetARB target, int size, void* data, BufferUsageARB usage);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBufferSubData_t(BufferTargetARB target, IntPtr offset, int size, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetBufferSubData_t(BufferTargetARB target, IntPtr offset, int size, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void* glMapBuffer_t(BufferTargetARB target, BufferAccessARB access);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glUnmapBuffer_t(BufferTargetARB target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetBufferParameteriv_t(BufferTargetARB target, uint pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetBufferPointerv_t(BufferTargetARB target, uint pname, void** @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlendEquationSeparate_t(BlendEquationModeEXT modeRGB, BlendEquationModeEXT modeAlpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawBuffers_t(int n, uint* bufs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glStencilOpSeparate_t(GLEnum face, StencilOp sfail, StencilOp dpfail, StencilOp dppass);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glStencilFuncSeparate_t(GLEnum face, StencilFunction func, int @ref, uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glStencilMaskSeparate_t(GLEnum face, uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glAttachShader_t(uint program, uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindAttribLocation_t(uint program, uint index, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompileShader_t(uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint glCreateProgram_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint glCreateShader_t(ShaderType type);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteProgram_t(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteShader_t(uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDetachShader_t(uint program, uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDisableVertexAttribArray_t(uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glEnableVertexAttribArray_t(uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveAttrib_t(uint program,
            uint index,
            int bufSize,
            int* length,
            int* size,
            uint* type,
            char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveUniform_t(uint program,
            uint index,
            int bufSize,
            int* length,
            int* size,
            uint* type,
            char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetAttachedShaders_t(uint program, int maxCount, int* count, uint* shaders);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int glGetAttribLocation_t(uint program, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetProgramiv_t(uint program, ProgramPropertyARB pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetProgramInfoLog_t(uint program, int bufSize, int* length, char* infoLog);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetShaderiv_t(uint shader, ShaderParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetShaderInfoLog_t(uint shader, int bufSize, int* length, char* infoLog);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetShaderSource_t(uint shader, int bufSize, int* length, char* source);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int glGetUniformLocation_t(uint program, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetUniformfv_t(uint program, int location, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetUniformiv_t(uint program, int location, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexAttribdv_t(uint index, uint pname, double* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexAttribfv_t(uint index, uint pname, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexAttribiv_t(uint index, uint pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexAttribPointerv_t(uint index, uint pname, void** pointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsProgram_t(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsShader_t(uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glLinkProgram_t(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glShaderSource_t(uint shader, int count, IntPtr @string, int* length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUseProgram_t(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform1f_t(int location, float v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform2f_t(int location, float v0, float v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform3f_t(int location, float v0, float v1, float v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform4f_t(int location, float v0, float v1, float v2, float v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform1i_t(int location, int v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform2i_t(int location, int v0, int v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform3i_t(int location, int v0, int v1, int v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform4i_t(int location, int v0, int v1, int v2, int v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform1fv_t(int location, int count, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform2fv_t(int location, int count, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform3fv_t(int location, int count, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform4fv_t(int location, int count, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform1iv_t(int location, int count, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform2iv_t(int location, int count, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform3iv_t(int location, int count, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform4iv_t(int location, int count, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix2fv_t(int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix3fv_t(int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix4fv_t(int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glValidateProgram_t(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib1d_t(uint index, double x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib1dv_t(uint index, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib1f_t(uint index, float x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib1fv_t(uint index, float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib1s_t(uint index, short x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib1sv_t(uint index, short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib2d_t(uint index, double x, double y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib2dv_t(uint index, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib2f_t(uint index, float x, float y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib2fv_t(uint index, float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib2s_t(uint index, short x, short y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib2sv_t(uint index, short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib3d_t(uint index, double x, double y, double z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib3dv_t(uint index, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib3f_t(uint index, float x, float y, float z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib3fv_t(uint index, float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib3s_t(uint index, short x, short y, short z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib3sv_t(uint index, short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4Nbv_t(uint index, byte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4Niv_t(uint index, int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4Nsv_t(uint index, short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4Nub_t(uint index, byte x, byte y, byte z, byte w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4Nubv_t(uint index, byte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4Nuiv_t(uint index, uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4Nusv_t(uint index, short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4bv_t(uint index, byte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4d_t(uint index, double x, double y, double z, double w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4dv_t(uint index, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4f_t(uint index, float x, float y, float z, float w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4fv_t(uint index, float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4iv_t(uint index, int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4s_t(uint index, short x, short y, short z, short w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4sv_t(uint index, short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4ubv_t(uint index, byte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4uiv_t(uint index, uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttrib4usv_t(uint index, short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribPointer_t(uint index, int size, VertexAttribPointerType type, bool normalized, int stride, void* pointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix2x3fv_t(int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix3x2fv_t(int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix2x4fv_t(int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix4x2fv_t(int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix3x4fv_t(int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix4x3fv_t(int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glColorMaski_t(uint index, bool r, bool g, bool b, bool a);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetBooleani_v_t(BufferTargetARB target, uint index, bool* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetIntegeri_v_t(GLEnum target, uint index, int* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glEnablei_t(EnableCap target, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDisablei_t(EnableCap target, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsEnabledi_t(EnableCap target, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBeginTransformFeedback_t(PrimitiveType primitiveMode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glEndTransformFeedback_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindBufferRange_t(BufferTargetARB target, uint index, uint buffer, IntPtr offset, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindBufferBase_t(BufferTargetARB target, uint index, uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTransformFeedbackVaryings_t(uint program, int count, IntPtr varyings, uint bufferMode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTransformFeedbackVarying_t(uint program,
            uint index,
            int bufSize,
            int* length,
            int* size,
            uint* type,
            char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClampColor_t(uint target, uint clamp);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBeginConditionalRender_t(uint id, GLEnum mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glEndConditionalRender_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribIPointer_t(uint index, int size, VertexAttribPointerType type, int stride, void* pointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexAttribIiv_t(uint index, VertexAttribEnum pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexAttribIuiv_t(uint index, VertexAttribEnum pname, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI1i_t(uint index, int x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI2i_t(uint index, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI3i_t(uint index, int x, int y, int z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI4i_t(uint index, int x, int y, int z, int w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI1ui_t(uint index, uint x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI2ui_t(uint index, uint x, uint y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI3ui_t(uint index, uint x, uint y, uint z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI4ui_t(uint index, uint x, uint y, uint z, uint w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI1iv_t(uint index, int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI2iv_t(uint index, int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI3iv_t(uint index, int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI4iv_t(uint index, int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI1uiv_t(uint index, uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI2uiv_t(uint index, uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI3uiv_t(uint index, uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI4uiv_t(uint index, uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI4bv_t(uint index, byte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI4sv_t(uint index, short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI4ubv_t(uint index, byte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribI4usv_t(uint index, short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetUniformuiv_t(uint program, int location, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindFragDataLocation_t(uint program, uint color, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int glGetFragDataLocation_t(uint program, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform1ui_t(int location, uint v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform2ui_t(int location, uint v0, uint v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform3ui_t(int location, uint v0, uint v1, uint v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform4ui_t(int location, uint v0, uint v1, uint v2, uint v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform1uiv_t(int location, int count, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform2uiv_t(int location, int count, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform3uiv_t(int location, int count, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform4uiv_t(int location, int count, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexParameterIiv_t(TextureTarget target, TextureParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexParameterIuiv_t(TextureTarget target, TextureParameterName pname, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTexParameterIiv_t(TextureTarget target, GetTextureParameter pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTexParameterIuiv_t(TextureTarget target, GetTextureParameter pname, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearBufferiv_t(GLEnum buffer, int drawbuffer, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearBufferuiv_t(GLEnum buffer, int drawbuffer, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearBufferfv_t(GLEnum buffer, int drawbuffer, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearBufferfi_t(GLEnum buffer, int drawbuffer, float depth, int stencil);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate byte* glGetStringi_t(StringName name, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsRenderbuffer_t(uint renderbuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindRenderbuffer_t(RenderbufferTarget target, uint renderbuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteRenderbuffers_t(int n, uint* renderbuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenRenderbuffers_t(int n, uint* renderbuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glRenderbufferStorage_t(RenderbufferTarget target, InternalFormat internalformat, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetRenderbufferParameteriv_t(RenderbufferTarget target, RenderbufferParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsFramebuffer_t(uint framebuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindFramebuffer_t(FramebufferTarget target, uint framebuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteFramebuffers_t(int n, uint* framebuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenFramebuffers_t(int n, uint* framebuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate FramebufferStatus glCheckFramebufferStatus_t(FramebufferTarget target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFramebufferTexture1D_t(FramebufferTarget target,
            FramebufferAttachment attachment,
            TextureTarget textarget,
            uint texture,
            int level);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFramebufferTexture2D_t(FramebufferTarget target,
            FramebufferAttachment attachment,
            TextureTarget textarget,
            uint texture,
            int level);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFramebufferTexture3D_t(FramebufferTarget target,
            FramebufferAttachment attachment,
            TextureTarget textarget,
            uint texture,
            int level,
            int zoffset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFramebufferRenderbuffer_t(FramebufferTarget target,
            FramebufferAttachment attachment,
            RenderbufferTarget renderbuffertarget,
            uint renderbuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetFramebufferAttachmentParameteriv_t(FramebufferTarget target,
            FramebufferAttachment attachment,
            FramebufferAttachmentParameterName pname,
            int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenerateMipmap_t(TextureTarget target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlitFramebuffer_t(int srcX0,
            int srcY0,
            int srcX1,
            int srcY1,
            int dstX0,
            int dstY0,
            int dstX1,
            int dstY1,
            uint mask,
            BlitFramebufferFilter filter);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glRenderbufferStorageMultisample_t(RenderbufferTarget target,
            int samples,
            InternalFormat internalformat,
            int width,
            int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFramebufferTextureLayer_t(FramebufferTarget target, FramebufferAttachment attachment, uint texture, int level, int layer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void* glMapBufferRange_t(BufferTargetARB target, IntPtr offset, int length, uint access);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFlushMappedBufferRange_t(BufferTargetARB target, IntPtr offset, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindVertexArray_t(uint array);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteVertexArrays_t(int n, uint* arrays);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenVertexArrays_t(int n, uint* arrays);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsVertexArray_t(uint array);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawArraysInstanced_t(PrimitiveType mode, int first, int count, int instancecount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawElementsInstanced_t(PrimitiveType mode, int count, DrawElementsType type, void* indices, int instancecount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexBuffer_t(TextureTarget target, InternalFormat internalformat, uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPrimitiveRestartIndex_t(uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyBufferSubData_t(CopyBufferSubDataTarget readTarget,
            CopyBufferSubDataTarget writeTarget,
            IntPtr readOffset,
            IntPtr writeOffset,
            int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetUniformIndices_t(uint program, int uniformCount, IntPtr uniformNames, uint* uniformIndices);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveUniformsiv_t(uint program, int uniformCount, uint* uniformIndices, UniformPName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveUniformName_t(uint program, uint uniformIndex, int bufSize, int* length, char* uniformName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint glGetUniformBlockIndex_t(uint program, char* uniformBlockName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveUniformBlockiv_t(uint program, uint uniformBlockIndex, UniformBlockPName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveUniformBlockName_t(uint program, uint uniformBlockIndex, int bufSize, int* length, char* uniformBlockName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformBlockBinding_t(uint program, uint uniformBlockIndex, uint uniformBlockBinding);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawElementsBaseVertex_t(PrimitiveType mode, int count, DrawElementsType type, void* indices, int basevertex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawRangeElementsBaseVertex_t(PrimitiveType mode,
            uint start,
            uint end,
            int count,
            DrawElementsType type,
            void* indices,
            int basevertex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawElementsInstancedBaseVertex_t(PrimitiveType mode,
            int count,
            DrawElementsType type,
            void* indices,
            int instancecount,
            int basevertex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiDrawElementsBaseVertex_t(PrimitiveType mode,
            int* count,
            DrawElementsType type,
            IntPtr indices,
            int drawcount,
            int* basevertex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProvokingVertex_t(VertexProvokingMode mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr glFenceSync_t(SyncCondition condition, uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsSync_t(IntPtr sync);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteSync_t(IntPtr sync);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate SyncStatus glClientWaitSync_t(IntPtr sync, uint flags, ulong timeout);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glWaitSync_t(IntPtr sync, uint flags, ulong timeout);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetInteger64v_t(GetPName pname, long* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetSynciv_t(IntPtr sync, SyncParameterName pname, int bufSize, int* length, int* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetInteger64i_v_t(GLEnum target, uint index, long* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetBufferParameteri64v_t(BufferTargetARB target, uint pname, long* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFramebufferTexture_t(FramebufferTarget target, FramebufferAttachment attachment, uint texture, int level);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexImage2DMultisample_t(TextureTarget target,
            int samples,
            InternalFormat internalformat,
            int width,
            int height,
            bool fixedsamplelocations);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexImage3DMultisample_t(TextureTarget target,
            int samples,
            InternalFormat internalformat,
            int width,
            int height,
            int depth,
            bool fixedsamplelocations);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetMultisamplefv_t(uint pname, uint index, float* val);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSampleMaski_t(uint maskNumber, uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindFragDataLocationIndexed_t(uint program, uint colorNumber, uint index, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int glGetFragDataIndex_t(uint program, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenSamplers_t(int count, uint* samplers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteSamplers_t(int count, uint* samplers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsSampler_t(uint sampler);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindSampler_t(uint unit, uint sampler);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSamplerParameteri_t(uint sampler, GLEnum pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSamplerParameteriv_t(uint sampler, GLEnum pname, int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSamplerParameterf_t(uint sampler, GLEnum pname, float param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSamplerParameterfv_t(uint sampler, GLEnum pname, float* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSamplerParameterIiv_t(uint sampler, GLEnum pname, int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSamplerParameterIuiv_t(uint sampler, GLEnum pname, uint* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetSamplerParameteriv_t(uint sampler, GLEnum pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetSamplerParameterIiv_t(uint sampler, GLEnum pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetSamplerParameterfv_t(uint sampler, GLEnum pname, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetSamplerParameterIuiv_t(uint sampler, GLEnum pname, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glQueryCounter_t(uint id, QueryCounterTarget target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryObjecti64v_t(uint id, QueryObjectParameterName pname, long* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryObjectui64v_t(uint id, QueryObjectParameterName pname, ulong* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribDivisor_t(uint index, uint divisor);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribP1ui_t(uint index, VertexAttribPointerType type, bool normalized, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribP1uiv_t(uint index, VertexAttribPointerType type, bool normalized, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribP2ui_t(uint index, VertexAttribPointerType type, bool normalized, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribP2uiv_t(uint index, VertexAttribPointerType type, bool normalized, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribP3ui_t(uint index, VertexAttribPointerType type, bool normalized, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribP3uiv_t(uint index, VertexAttribPointerType type, bool normalized, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribP4ui_t(uint index, VertexAttribPointerType type, bool normalized, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribP4uiv_t(uint index, VertexAttribPointerType type, bool normalized, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexP2ui_t(VertexPointerType type, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexP2uiv_t(VertexPointerType type, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexP3ui_t(VertexPointerType type, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexP3uiv_t(VertexPointerType type, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexP4ui_t(VertexPointerType type, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexP4uiv_t(VertexPointerType type, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexCoordP1ui_t(TexCoordPointerType type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexCoordP1uiv_t(TexCoordPointerType type, uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexCoordP2ui_t(TexCoordPointerType type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexCoordP2uiv_t(TexCoordPointerType type, uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexCoordP3ui_t(TexCoordPointerType type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexCoordP3uiv_t(TexCoordPointerType type, uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexCoordP4ui_t(TexCoordPointerType type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexCoordP4uiv_t(TexCoordPointerType type, uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiTexCoordP1ui_t(TextureUnit texture, TexCoordPointerType type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiTexCoordP1uiv_t(TextureUnit texture, TexCoordPointerType type, uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiTexCoordP2ui_t(TextureUnit texture, TexCoordPointerType type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiTexCoordP2uiv_t(TextureUnit texture, TexCoordPointerType type, uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiTexCoordP3ui_t(TextureUnit texture, TexCoordPointerType type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiTexCoordP3uiv_t(TextureUnit texture, TexCoordPointerType type, uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiTexCoordP4ui_t(TextureUnit texture, TexCoordPointerType type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiTexCoordP4uiv_t(TextureUnit texture, TexCoordPointerType type, uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNormalP3ui_t(NormalPointerType type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNormalP3uiv_t(NormalPointerType type, uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glColorP3ui_t(ColorPointerType type, uint color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glColorP3uiv_t(ColorPointerType type, uint* color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glColorP4ui_t(ColorPointerType type, uint color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glColorP4uiv_t(ColorPointerType type, uint* color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSecondaryColorP3ui_t(ColorPointerType type, uint color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSecondaryColorP3uiv_t(ColorPointerType type, uint* color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMinSampleShading_t(float value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlendEquationi_t(uint buf, BlendEquationModeEXT mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlendEquationSeparatei_t(uint buf, BlendEquationModeEXT modeRGB, BlendEquationModeEXT modeAlpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlendFunci_t(uint buf, BlendingFactor src, BlendingFactor dst);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlendFuncSeparatei_t(uint buf,
            BlendingFactor srcRGB,
            BlendingFactor dstRGB,
            BlendingFactor srcAlpha,
            BlendingFactor dstAlpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawArraysIndirect_t(PrimitiveType mode, void* indirect);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawElementsIndirect_t(PrimitiveType mode, DrawElementsType type, void* indirect);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform1d_t(int location, double x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform2d_t(int location, double x, double y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform3d_t(int location, double x, double y, double z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform4d_t(int location, double x, double y, double z, double w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform1dv_t(int location, int count, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform2dv_t(int location, int count, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform3dv_t(int location, int count, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniform4dv_t(int location, int count, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix2dv_t(int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix3dv_t(int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix4dv_t(int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix2x3dv_t(int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix2x4dv_t(int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix3x2dv_t(int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix3x4dv_t(int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix4x2dv_t(int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformMatrix4x3dv_t(int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetUniformdv_t(uint program, int location, double* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int glGetSubroutineUniformLocation_t(uint program, ShaderType shadertype, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint glGetSubroutineIndex_t(uint program, ShaderType shadertype, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveSubroutineUniformiv_t(uint program, ShaderType shadertype, uint index, SubroutineParameterName pname, int* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveSubroutineUniformName_t(uint program, ShaderType shadertype, uint index, int bufsize, int* length, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveSubroutineName_t(uint program, ShaderType shadertype, uint index, int bufsize, int* length, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUniformSubroutinesuiv_t(ShaderType shadertype, int count, uint* indices);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetUniformSubroutineuiv_t(ShaderType shadertype, int location, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetProgramStageiv_t(uint program, ShaderType shadertype, ProgramStagePName pname, int* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPatchParameteri_t(PatchParameterName pname, int value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPatchParameterfv_t(PatchParameterName pname, float* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindTransformFeedback_t(BindTransformFeedbackTarget target, uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteTransformFeedbacks_t(int n, uint* ids);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenTransformFeedbacks_t(int n, uint* ids);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsTransformFeedback_t(uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPauseTransformFeedback_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glResumeTransformFeedback_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawTransformFeedback_t(PrimitiveType mode, uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawTransformFeedbackStream_t(PrimitiveType mode, uint id, uint stream);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBeginQueryIndexed_t(QueryTarget target, uint index, uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glEndQueryIndexed_t(QueryTarget target, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryIndexediv_t(uint target, uint index, QueryParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glReleaseShaderCompiler_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glShaderBinary_t(int count, uint* shaders, uint binaryformat, void* binary, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetShaderPrecisionFormat_t(ShaderType shadertype, PrecisionType precisiontype, int* range, int* precision);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDepthRangef_t(float n, float f);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearDepthf_t(float d);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetProgramBinary_t(uint program, int bufSize, int* length, uint* binaryFormat, void* binary);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramBinary_t(uint program, uint binaryFormat, void* binary, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramParameteri_t(uint program, ProgramParameterPName pname, int value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glUseProgramStages_t(uint pipeline, uint stages, uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glActiveShaderProgram_t(uint pipeline, uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint glCreateShaderProgramv_t(ShaderType type, int count, IntPtr strings);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindProgramPipeline_t(uint pipeline);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDeleteProgramPipelines_t(int n, uint* pipelines);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenProgramPipelines_t(int n, uint* pipelines);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glIsProgramPipeline_t(uint pipeline);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetProgramPipelineiv_t(uint pipeline, PipelineParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform1i_t(uint program, int location, int v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform1iv_t(uint program, int location, int count, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform1f_t(uint program, int location, float v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform1fv_t(uint program, int location, int count, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform1d_t(uint program, int location, double v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform1dv_t(uint program, int location, int count, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform1ui_t(uint program, int location, uint v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform1uiv_t(uint program, int location, int count, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform2i_t(uint program, int location, int v0, int v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform2iv_t(uint program, int location, int count, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform2f_t(uint program, int location, float v0, float v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform2fv_t(uint program, int location, int count, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform2d_t(uint program, int location, double v0, double v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform2dv_t(uint program, int location, int count, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform2ui_t(uint program, int location, uint v0, uint v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform2uiv_t(uint program, int location, int count, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform3i_t(uint program, int location, int v0, int v1, int v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform3iv_t(uint program, int location, int count, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform3f_t(uint program, int location, float v0, float v1, float v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform3fv_t(uint program, int location, int count, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform3d_t(uint program, int location, double v0, double v1, double v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform3dv_t(uint program, int location, int count, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform3ui_t(uint program, int location, uint v0, uint v1, uint v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform3uiv_t(uint program, int location, int count, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform4i_t(uint program, int location, int v0, int v1, int v2, int v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform4iv_t(uint program, int location, int count, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform4f_t(uint program, int location, float v0, float v1, float v2, float v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform4fv_t(uint program, int location, int count, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform4d_t(uint program, int location, double v0, double v1, double v2, double v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform4dv_t(uint program, int location, int count, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform4ui_t(uint program, int location, uint v0, uint v1, uint v2, uint v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniform4uiv_t(uint program, int location, int count, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix2fv_t(uint program, int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix3fv_t(uint program, int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix4fv_t(uint program, int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix2dv_t(uint program, int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix3dv_t(uint program, int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix4dv_t(uint program, int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix2x3fv_t(uint program, int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix3x2fv_t(uint program, int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix2x4fv_t(uint program, int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix4x2fv_t(uint program, int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix3x4fv_t(uint program, int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix4x3fv_t(uint program, int location, int count, bool transpose, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix2x3dv_t(uint program, int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix3x2dv_t(uint program, int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix2x4dv_t(uint program, int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix4x2dv_t(uint program, int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix3x4dv_t(uint program, int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glProgramUniformMatrix4x3dv_t(uint program, int location, int count, bool transpose, double* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glValidateProgramPipeline_t(uint pipeline);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetProgramPipelineInfoLog_t(uint pipeline, int bufSize, int* length, char* infoLog);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribL1d_t(uint index, double x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribL2d_t(uint index, double x, double y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribL3d_t(uint index, double x, double y, double z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribL4d_t(uint index, double x, double y, double z, double w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribL1dv_t(uint index, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribL2dv_t(uint index, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribL3dv_t(uint index, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribL4dv_t(uint index, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribLPointer_t(uint index, int size, VertexAttribPointerType type, int stride, void* pointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexAttribLdv_t(uint index, VertexAttribEnum pname, double* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glViewportArrayv_t(uint first, int count, float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glViewportIndexedf_t(uint index, float x, float y, float w, float h);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glViewportIndexedfv_t(uint index, float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glScissorArrayv_t(uint first, int count, int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glScissorIndexed_t(uint index, int left, int bottom, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glScissorIndexedv_t(uint index, int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDepthRangeArrayv_t(uint first, int count, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDepthRangeIndexed_t(uint index, double n, double f);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetFloati_v_t(GLEnum target, uint index, float* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetDoublei_v_t(GLEnum target, uint index, double* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawArraysInstancedBaseInstance_t(PrimitiveType mode, int first, int count, int instancecount, uint baseinstance);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawElementsInstancedBaseInstance_t(PrimitiveType mode,
            int count,
            PrimitiveType type,
            void* indices,
            int instancecount,
            uint baseinstance);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawElementsInstancedBaseVertexBaseInstance_t(PrimitiveType mode,
            int count,
            DrawElementsType type,
            void* indices,
            int instancecount,
            int basevertex,
            uint baseinstance);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetInternalformativ_t(TextureTarget target,
            InternalFormat internalformat,
            InternalFormatPName pname,
            int bufSize,
            int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetActiveAtomicCounterBufferiv_t(uint program, uint bufferIndex, AtomicCounterBufferPName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindImageTexture_t(uint unit,
            uint texture,
            int level,
            bool layered,
            int layer,
            BufferAccessARB access,
            InternalFormat format);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMemoryBarrier_t(uint barriers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexStorage1D_t(TextureTarget target, int levels, InternalFormat internalformat, int width);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexStorage2D_t(TextureTarget target, int levels, InternalFormat internalformat, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexStorage3D_t(TextureTarget target, int levels, InternalFormat internalformat, int width, int height, int depth);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawTransformFeedbackInstanced_t(PrimitiveType mode, uint id, int instancecount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDrawTransformFeedbackStreamInstanced_t(PrimitiveType mode, uint id, uint stream, int instancecount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearBufferData_t(BufferStorageTarget target, InternalFormat internalformat, PixelFormat format, PixelType type, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearBufferSubData_t(uint target,
            InternalFormat internalformat,
            IntPtr offset,
            int size,
            PixelFormat format,
            PixelType type,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDispatchCompute_t(uint num_groups_x, uint num_groups_y, uint num_groups_z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDispatchComputeIndirect_t(IntPtr indirect);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyImageSubData_t(uint srcName,
            CopyBufferSubDataTarget srcTarget,
            int srcLevel,
            int srcX,
            int srcY,
            int srcZ,
            uint dstName,
            CopyBufferSubDataTarget dstTarget,
            int dstLevel,
            int dstX,
            int dstY,
            int dstZ,
            int srcWidth,
            int srcHeight,
            int srcDepth);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFramebufferParameteri_t(FramebufferTarget target, FramebufferParameterName pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetFramebufferParameteriv_t(FramebufferTarget target, FramebufferAttachmentParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetInternalformati64v_t(TextureTarget target,
            InternalFormat internalformat,
            InternalFormatPName pname,
            int bufSize,
            long* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glInvalidateTexSubImage_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int width,
            int height,
            int depth);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glInvalidateTexImage_t(uint texture, int level);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glInvalidateBufferSubData_t(uint buffer, IntPtr offset, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glInvalidateBufferData_t(uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glInvalidateFramebuffer_t(FramebufferTarget target, int numAttachments, uint* attachments);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glInvalidateSubFramebuffer_t(uint target,
            int numAttachments,
            uint* attachments,
            int x,
            int y,
            int width,
            int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiDrawArraysIndirect_t(PrimitiveType mode, void* indirect, int drawcount, int stride);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiDrawElementsIndirect_t(PrimitiveType mode, DrawElementsType type, void* indirect, int drawcount, int stride);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetProgramInterfaceiv_t(uint program, ProgramInterface programInterface, ProgramInterfacePName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint glGetProgramResourceIndex_t(uint program, ProgramInterface programInterface, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetProgramResourceName_t(uint program, ProgramInterface programInterface, uint index, int bufSize, int* length, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetProgramResourceiv_t(uint program,
            ProgramInterface programInterface,
            uint index,
            int propCount,
            uint* props,
            int bufSize,
            int* length,
            int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int glGetProgramResourceLocation_t(uint program, ProgramInterface programInterface, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int glGetProgramResourceLocationIndex_t(uint program, ProgramInterface programInterface, char* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glShaderStorageBlockBinding_t(uint program, uint storageBlockIndex, uint storageBlockBinding);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexBufferRange_t(TextureTarget target, InternalFormat internalformat, uint buffer, IntPtr offset, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexStorage2DMultisample_t(TextureTarget target,
            int samples,
            InternalFormat internalformat,
            int width,
            int height,
            bool fixedsamplelocations);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTexStorage3DMultisample_t(TextureTarget target,
            int samples,
            InternalFormat internalformat,
            int width,
            int height,
            int depth,
            bool fixedsamplelocations);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureView_t(uint texture,
            TextureTarget target,
            uint origtexture,
            InternalFormat internalformat,
            uint minlevel,
            uint numlevels,
            uint minlayer,
            uint numlayers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindVertexBuffer_t(uint bindingindex, uint buffer, IntPtr offset, int stride);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribFormat_t(uint attribindex, int size, uint type, bool normalized, uint relativeoffset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribIFormat_t(uint attribindex, int size, uint type, uint relativeoffset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribLFormat_t(uint attribindex, int size, VertexAttribType type, uint relativeoffset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexAttribBinding_t(uint attribindex, uint bindingindex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexBindingDivisor_t(uint bindingindex, uint divisor);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDebugMessageControl_t(DebugSource source, DebugType type, DebugSeverity severity, int count, uint* ids, bool enabled);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDebugMessageInsert_t(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, char* buf);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDebugMessageCallback_t(IntPtr callback, void* userParam);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate uint glGetDebugMessageLog_t(uint count,
            int bufSize,
            uint* sources,
            uint* types,
            uint* ids,
            uint* severities,
            int* lengths,
            char* messageLog);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPushDebugGroup_t(DebugSource source, uint id, int length, char* message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPopDebugGroup_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glObjectLabel_t(ObjectIdentifier identifier, uint name, int length, char* label);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetObjectLabel_t(uint identifier, uint name, int bufSize, int* length, char* label);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glObjectPtrLabel_t(void* ptr, int length, char* label);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetObjectPtrLabel_t(void* ptr, int bufSize, int* length, char* label);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetPointerv_t(GetPointervPName pname, void** @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBufferStorage_t(BufferStorageTarget target, int size, void* data, uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearTexImage_t(uint texture, int level, PixelFormat format, PixelType type, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearTexSubImage_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int width,
            int height,
            int depth,
            PixelFormat format,
            PixelType type,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindBuffersBase_t(BufferTargetARB target, uint first, int count, uint* buffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindBuffersRange_t(BufferTargetARB target, uint first, int count, uint* buffers, IntPtr offsets, int* sizes);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindTextures_t(uint first, int count, uint* textures);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindSamplers_t(uint first, int count, uint* samplers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindImageTextures_t(uint first, int count, uint* textures);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindVertexBuffers_t(uint first, int count, uint* buffers, IntPtr offsets, int* strides);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClipControl_t(ClipControlOrigin origin, ClipControlDepth depth);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCreateTransformFeedbacks_t(int n, uint* ids);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTransformFeedbackBufferBase_t(uint xfb, uint index, uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTransformFeedbackBufferRange_t(uint xfb, uint index, uint buffer, IntPtr offset, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTransformFeedbackiv_t(uint xfb, TransformFeedbackPName pname, int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTransformFeedbacki_v_t(uint xfb, TransformFeedbackPName pname, uint index, int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTransformFeedbacki64_v_t(uint xfb, TransformFeedbackPName pname, uint index, long* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCreateBuffers_t(int n, uint* buffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedBufferStorage_t(uint buffer, int size, void* data, uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedBufferData_t(uint buffer, int size, void* data, BufferUsageARB usage);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedBufferSubData_t(uint buffer, IntPtr offset, int size, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyNamedBufferSubData_t(uint readBuffer, uint writeBuffer, IntPtr readOffset, IntPtr writeOffset, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearNamedBufferData_t(uint buffer, InternalFormat internalformat, PixelFormat format, PixelType type, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearNamedBufferSubData_t(uint buffer,
            InternalFormat internalformat,
            IntPtr offset,
            int size,
            PixelFormat format,
            PixelType type,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void* glMapNamedBuffer_t(uint buffer, BufferAccessARB access);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void* glMapNamedBufferRange_t(uint buffer, IntPtr offset, int length, uint access);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool glUnmapNamedBuffer_t(uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glFlushMappedNamedBufferRange_t(uint buffer, IntPtr offset, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetNamedBufferParameteriv_t(uint buffer, GLEnum pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetNamedBufferParameteri64v_t(uint buffer, GLEnum pname, long* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetNamedBufferPointerv_t(uint buffer, GLEnum pname, void** @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetNamedBufferSubData_t(uint buffer, IntPtr offset, int size, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCreateFramebuffers_t(int n, uint* framebuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedFramebufferRenderbuffer_t(uint framebuffer,
            FramebufferAttachment attachment,
            RenderbufferTarget renderbuffertarget,
            uint renderbuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedFramebufferParameteri_t(uint framebuffer, FramebufferParameterName pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedFramebufferTexture_t(uint framebuffer, FramebufferAttachment attachment, uint texture, int level);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedFramebufferTextureLayer_t(uint framebuffer, FramebufferAttachment attachment, uint texture, int level, int layer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedFramebufferDrawBuffer_t(uint framebuffer, ColorBuffer buf);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedFramebufferDrawBuffers_t(uint framebuffer, int n, uint* bufs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedFramebufferReadBuffer_t(uint framebuffer, ColorBuffer src);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glInvalidateNamedFramebufferData_t(uint framebuffer, int numAttachments, uint* attachments);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glInvalidateNamedFramebufferSubData_t(uint framebuffer,
            int numAttachments,
            uint* attachments,
            int x,
            int y,
            int width,
            int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearNamedFramebufferiv_t(uint framebuffer, GLEnum buffer, int drawbuffer, int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearNamedFramebufferuiv_t(uint framebuffer, GLEnum buffer, int drawbuffer, uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearNamedFramebufferfv_t(uint framebuffer, GLEnum buffer, int drawbuffer, float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glClearNamedFramebufferfi_t(uint framebuffer, GLEnum buffer, int drawbuffer, float depth, int stencil);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBlitNamedFramebuffer_t(uint readFramebuffer,
            uint drawFramebuffer,
            int srcX0,
            int srcY0,
            int srcX1,
            int srcY1,
            int dstX0,
            int dstY0,
            int dstX1,
            int dstY1,
            uint mask,
            BlitFramebufferFilter filter);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate FramebufferStatus glCheckNamedFramebufferStatus_t(uint framebuffer, FramebufferTarget target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetNamedFramebufferParameteriv_t(uint framebuffer, GetFramebufferParameter pname, int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetNamedFramebufferAttachmentParameteriv_t(uint framebuffer,
            FramebufferAttachment attachment,
            FramebufferAttachmentParameterName pname,
            int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCreateRenderbuffers_t(int n, uint* renderbuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedRenderbufferStorage_t(uint renderbuffer, InternalFormat internalformat, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glNamedRenderbufferStorageMultisample_t(uint renderbuffer, int samples, InternalFormat internalformat, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetNamedRenderbufferParameteriv_t(uint renderbuffer, RenderbufferParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCreateTextures_t(TextureTarget target, int n, uint* textures);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureBuffer_t(uint texture, InternalFormat internalformat, uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureBufferRange_t(uint texture, InternalFormat internalformat, uint buffer, IntPtr offset, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureStorage1D_t(uint texture, int levels, InternalFormat internalformat, int width);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureStorage2D_t(uint texture, int levels, InternalFormat internalformat, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureStorage3D_t(uint texture, int levels, InternalFormat internalformat, int width, int height, int depth);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureStorage2DMultisample_t(uint texture,
            int samples,
            InternalFormat internalformat,
            int width,
            int height,
            bool fixedsamplelocations);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureStorage3DMultisample_t(uint texture,
            int samples,
            InternalFormat internalformat,
            int width,
            int height,
            int depth,
            bool fixedsamplelocations);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureSubImage1D_t(uint texture,
            int level,
            int xoffset,
            int width,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureSubImage2D_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int width,
            int height,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureSubImage3D_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int width,
            int height,
            int depth,
            PixelFormat format,
            PixelType type,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompressedTextureSubImage1D_t(uint texture,
            int level,
            int xoffset,
            int width,
            PixelFormat format,
            int imageSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompressedTextureSubImage2D_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int width,
            int height,
            PixelFormat format,
            int imageSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCompressedTextureSubImage3D_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int width,
            int height,
            int depth,
            PixelFormat format,
            int imageSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyTextureSubImage1D_t(uint texture, int level, int xoffset, int x, int y, int width);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyTextureSubImage2D_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int x,
            int y,
            int width,
            int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCopyTextureSubImage3D_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int x,
            int y,
            int width,
            int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureParameterf_t(uint texture, TextureParameterName pname, float param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureParameterfv_t(uint texture, TextureParameterName pname, float* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureParameteri_t(uint texture, TextureParameterName pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureParameterIiv_t(uint texture, TextureParameterName pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureParameterIuiv_t(uint texture, TextureParameterName pname, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureParameteriv_t(uint texture, TextureParameterName pname, int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGenerateTextureMipmap_t(uint texture);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glBindTextureUnit_t(uint unit, uint texture);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTextureImage_t(uint texture, int level, PixelFormat format, PixelType type, int bufSize, void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetCompressedTextureImage_t(uint texture, int level, int bufSize, void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTextureLevelParameterfv_t(uint texture, int level, GetTextureParameter pname, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTextureLevelParameteriv_t(uint texture, int level, GetTextureParameter pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTextureParameterfv_t(uint texture, GetTextureParameter pname, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTextureParameterIiv_t(uint texture, GetTextureParameter pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTextureParameterIuiv_t(uint texture, GetTextureParameter pname, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTextureParameteriv_t(uint texture, GetTextureParameter pname, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCreateVertexArrays_t(int n, uint* arrays);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glDisableVertexArrayAttrib_t(uint vaobj, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glEnableVertexArrayAttrib_t(uint vaobj, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexArrayElementBuffer_t(uint vaobj, uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexArrayVertexBuffer_t(uint vaobj, uint bindingindex, uint buffer, IntPtr offset, int stride);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexArrayVertexBuffers_t(uint vaobj, uint first, int count, uint* buffers, IntPtr offsets, int* strides);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexArrayAttribBinding_t(uint vaobj, uint attribindex, uint bindingindex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexArrayAttribFormat_t(uint vaobj,
            uint attribindex,
            int size,
            VertexAttribType type,
            bool normalized,
            uint relativeoffset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexArrayAttribIFormat_t(uint vaobj, uint attribindex, int size, VertexAttribType type, uint relativeoffset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexArrayAttribLFormat_t(uint vaobj, uint attribindex, int size, VertexAttribType type, uint relativeoffset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glVertexArrayBindingDivisor_t(uint vaobj, uint bindingindex, uint divisor);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexArrayiv_t(uint vaobj, VertexArrayPName pname, int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexArrayIndexediv_t(uint vaobj, uint index, VertexArrayPName pname, int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetVertexArrayIndexed64iv_t(uint vaobj, uint index, VertexArrayPName pname, long* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCreateSamplers_t(int n, uint* samplers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCreateProgramPipelines_t(int n, uint* pipelines);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glCreateQueries_t(QueryTarget target, int n, uint* ids);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryBufferObjecti64v_t(uint id, uint buffer, QueryObjectParameterName pname, IntPtr offset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryBufferObjectiv_t(uint id, uint buffer, QueryObjectParameterName pname, IntPtr offset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryBufferObjectui64v_t(uint id, uint buffer, QueryObjectParameterName pname, IntPtr offset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetQueryBufferObjectuiv_t(uint id, uint buffer, QueryObjectParameterName pname, IntPtr offset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMemoryBarrierByRegion_t(uint barriers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetTextureSubImage_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int width,
            int height,
            int depth,
            PixelFormat format,
            PixelType type,
            int bufSize,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetCompressedTextureSubImage_t(uint texture,
            int level,
            int xoffset,
            int yoffset,
            int zoffset,
            int width,
            int height,
            int depth,
            int bufSize,
            void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate GraphicsResetStatus glGetGraphicsResetStatus_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnCompressedTexImage_t(TextureTarget target, int lod, int bufSize, void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnTexImage_t(TextureTarget target, int level, PixelFormat format, PixelType type, int bufSize, void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnUniformdv_t(uint program, int location, int bufSize, double* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnUniformfv_t(uint program, int location, int bufSize, float* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnUniformiv_t(uint program, int location, int bufSize, int* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnUniformuiv_t(uint program, int location, int bufSize, uint* @params);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glReadnPixels_t(int x,
            int y,
            int width,
            int height,
            PixelFormat format,
            PixelType type,
            int bufSize,
            void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnMapdv_t(MapTarget target, MapQuery query, int bufSize, double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnMapfv_t(MapTarget target, MapQuery query, int bufSize, float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnMapiv_t(MapTarget target, MapQuery query, int bufSize, int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnPixelMapfv_t(PixelMap map, int bufSize, float* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnPixelMapuiv_t(PixelMap map, int bufSize, uint* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnPixelMapusv_t(PixelMap map, int bufSize, short* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnPolygonStipple_t(int bufSize, byte* pattern);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnColorTable_t(ColorTableTarget target, PixelFormat format, PixelType type, int bufSize, void* table);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnConvolutionFilter_t(ConvolutionTarget target, PixelFormat format, PixelType type, int bufSize, void* image);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnSeparableFilter_t(SeparableTargetEXT target,
            PixelFormat format,
            PixelType type,
            int rowBufSize,
            void* row,
            int columnBufSize,
            void* column,
            void* span);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnHistogram_t(HistogramTargetEXT target, bool reset, PixelFormat format, PixelType type, int bufSize, void* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glGetnMinmax_t(MinmaxTargetEXT target, bool reset, PixelFormat format, PixelType type, int bufSize, void* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glTextureBarrier_t();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glSpecializeShader_t(uint shader,
            char* pEntryPoint,
            uint numSpecializationConstants,
            uint* pConstantIndex,
            uint* pConstantValue);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiDrawArraysIndirectCount_t(PrimitiveType mode, void* indirect, IntPtr drawcount, int maxdrawcount, int stride);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glMultiDrawElementsIndirectCount_t(PrimitiveType mode,
            uint type,
            void* indirect,
            IntPtr drawcount,
            int maxdrawcount,
            int stride);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void glPolygonOffsetClamp_t(float factor, float units, float clamp);
    }
}