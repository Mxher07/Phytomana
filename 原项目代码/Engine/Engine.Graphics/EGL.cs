using System.Runtime.InteropServices;

namespace Engine.Graphics {
    public static class Egl {
#if BROWSER
        public const string LibEgl = "libEGL";
#else
        public const string LibEgl = "libEGL.dll";
#endif
        public const int None = 0x3038;
        public const int RedSize = 0x3024;
        public const int GreenSize = 0x3023;
        public const int BlueSize = 0x3022;
        public const int AlphaSize = 0x3021;
        public const int DepthSize = 0x3025;
        public const int StencilSize = 0x3026;
        public const int SurfaceType = 0x3033;
        public const int RenderableType = 0x3040;
        public const int Samples = 0x3031;
        public const int WindowBit = 0x0004;
        public const int PbufferBit = 0x0001;
        public const int OpenglEs2Bit = 0x0004;
        public const int OpenglEs3Bit = 0x00000040;
        public const int Width = 0x3057;
        public const int Height = 0x3056;
        public const int ContextClientVersion = 0x3098;
        public const int NoContext = 0x0;
        public const int NativeVisualId = 0x302E;
        public const int OpenglEsApi = 0x30A0;

        [DllImport(LibEgl, EntryPoint = "eglGetDisplay", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr GetDisplay(IntPtr displayId);

        [DllImport(LibEgl, EntryPoint = "eglInitialize", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern bool Initialize(IntPtr dpy, out int major, out int minor);

        [DllImport(LibEgl, EntryPoint = "eglChooseConfig", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern bool ChooseConfig(IntPtr dpy, int[] attribList, IntPtr[] configs, int configSize, out int numConfig);

        [DllImport(LibEgl, EntryPoint = "eglCreateWindowSurface", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr CreateWindowSurface(IntPtr dpy, IntPtr config, IntPtr nativeWindow, int[] attribList);

#if ANGLE
        [DllImport(LibEgl, EntryPoint = "eglCreatePbufferSurface", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr CreatePbufferSurface(IntPtr dpy, IntPtr config, int[] attribList);
#endif

        [DllImport(LibEgl, EntryPoint = "eglCreateContext", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr CreateContext(IntPtr dpy, IntPtr config, IntPtr shareContext, int[] attribList);

        [DllImport(LibEgl, EntryPoint = "eglMakeCurrent", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern bool MakeCurrent(IntPtr dpy, IntPtr draw, IntPtr read, IntPtr ctx);

        [DllImport(LibEgl, EntryPoint = "eglSwapBuffers", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern bool SwapBuffers(IntPtr dpy, IntPtr surface);

        [DllImport(LibEgl, EntryPoint = "eglSwapInterval", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern bool SwapInterval(IntPtr dpy, int interval);

        [DllImport(LibEgl, EntryPoint = "eglGetError", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int GetError();

        // wrapper to get proc addresses used by GL.GetApi
        [DllImport(LibEgl, EntryPoint = "eglGetProcAddress", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr GetProcAddress(string proc);
    }
}