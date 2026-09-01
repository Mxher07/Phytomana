using System.Runtime.InteropServices;

namespace Engine.Browser {
    public static class Emscripten {
        [DllImport("emscripten", CharSet = CharSet.Ansi, EntryPoint = "emscripten_request_animation_frame_loop")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern unsafe void RequestAnimationFrameLoop(void* f, nint userDataPtr);
    }
}