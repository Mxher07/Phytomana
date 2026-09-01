using System;
using System.Runtime.InteropServices;

namespace Engine.Browser {
    public static class OAL {
        // On WASM/Emscripten, this is often "openal" or "__Internal" depending on linking
        private const string LibOal = "openal32";

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern unsafe IntPtr alcOpenDevice(byte* devicename);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern unsafe IntPtr alcCreateContext(IntPtr device, int* attrlist);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern bool alcMakeContextCurrent(IntPtr context);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alcDestroyContext(IntPtr context);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern bool alcCloseDevice(IntPtr device);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int alcGetError(IntPtr device);

        // --- AL Functions (Core) ---

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern int alGetError();

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alGenSources(int n, out uint sources);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alDeleteSources(int n, ref uint sources);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alSourcei(uint source, int param, int value);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alSourcef(uint source, int param, float value);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alSource3f(uint source, int param, float v1, float v2, float v3);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alSourcePlay(uint source);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alSourcePause(uint source);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alSourceStop(uint source);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alSourceRewind(uint source);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alGetSourcei(uint source, int param, out int value);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alGenBuffers(int n, out uint buffers);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alDeleteBuffers(int n, ref uint buffers);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern unsafe void alBufferData(uint buffer, int format, void* data, int size, int freq);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alListenerf(int param, float value);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern void alDistanceModel(int model);

        // Queueing functions for Streaming
        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern unsafe void alSourceQueueBuffers(uint source, int nb, uint* buffers);

        [DllImport(LibOal, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern unsafe void alSourceUnqueueBuffers(uint source, int nb, uint* buffers);
    }
}