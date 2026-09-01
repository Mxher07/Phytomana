using System.Runtime.InteropServices;
using Silk.NET.OpenXR;

namespace Engine.VR {
    public unsafe class WindowsOpenXrVrBackend : OpenXrVrBackend {

        protected override StructureType GraphicsBindingType => StructureType.GraphicsBindingOpenglWin32Khr;

        protected override int GetGraphicsBindingSize() => sizeof(GraphicsBindingOpenGLWin32KHR);

        protected override void PopulateGraphicsBinding(void* bindingPtr) {
            IntPtr hdc = wglGetCurrentDC();
            IntPtr hglrc = wglGetCurrentContext();
            if (hdc == IntPtr.Zero || hglrc == IntPtr.Zero) {
                throw new InvalidOperationException("OpenXR requires a current WGL device context and OpenGL context.");
            }
            GraphicsBindingOpenGLWin32KHR* binding = (GraphicsBindingOpenGLWin32KHR*)bindingPtr;
            binding->HDC = hdc;
            binding->HGlrc = hglrc;
        }

        [DllImport("opengl32.dll")]
        static extern IntPtr wglGetCurrentDC();

        [DllImport("opengl32.dll")]
        static extern IntPtr wglGetCurrentContext();
    }
}
