using System.Runtime.InteropServices;
using Engine;
using Silk.NET.OpenXR;

namespace Engine.VR {
    public unsafe class AndroidOpenXrVrBackend : OpenXrVrBackend {
        protected override StructureType GraphicsBindingType => StructureType.GraphicsBindingOpenglESAndroidKhr;

        protected override int GetGraphicsBindingSize() => sizeof(GraphicsBindingOpenGLESAndroidKHR);

        protected override void PopulateGraphicsBinding(void* bindingPtr) {
            GraphicsBindingOpenGLESAndroidKHR* binding = (GraphicsBindingOpenGLESAndroidKHR*)bindingPtr;
            binding->Display = eglGetCurrentDisplay();
            binding->Context = eglGetCurrentContext();
            binding->Config = GetEglConfig();
        }

        static nint GetEglConfig() {
            nint display = eglGetCurrentDisplay();
            nint surface = eglGetCurrentSurface(EGL_DRAW);
            if (surface == nint.Zero) return nint.Zero;
            int configId = 0;
            eglQuerySurface(display, surface, EGL_CONFIG_ID, out configId);
            int[] attribs = [EGL_CONFIG_ID, configId, EGL_NONE];
            nint config = nint.Zero;
            int numConfigs;
            eglChooseConfig(display, attribs, out config, 1, out numConfigs);
            return config;
        }

        const int EGL_DRAW = 0x3059;
        const int EGL_CONFIG_ID = 0x3028;
        const int EGL_NONE = 0x3038;

        [DllImport("libEGL.so")]
        static extern nint eglGetCurrentDisplay();

        [DllImport("libEGL.so")]
        static extern nint eglGetCurrentContext();

        [DllImport("libEGL.so")]
        static extern nint eglGetCurrentSurface(int readdraw);

        [DllImport("libEGL.so")]
        static extern bool eglQuerySurface(nint dpy, nint surface, int attribute, out int value);

        [DllImport("libEGL.so")]
        static extern bool eglChooseConfig(nint dpy, int[] attrib_list, out nint configs, int config_size, out int num_config);
    }
}
