namespace Engine.Browser {
    public class ALContext : IDisposable {
        public static IntPtr m_device;
        public static IntPtr m_context;

        public unsafe ALContext() {
            m_device = OAL.alcOpenDevice(null);
            if (m_device == IntPtr.Zero) {
                Log.Error("Could not create audio device");
                return;
            }
            m_context = OAL.alcCreateContext(m_device, null);
            OAL.alcMakeContextCurrent(m_context);
        }

        public void Dispose() {
            if (m_context != IntPtr.Zero) {
                OAL.alcMakeContextCurrent(IntPtr.Zero);
                OAL.alcDestroyContext(m_context);
                m_context = IntPtr.Zero;
            }
            if (m_device != IntPtr.Zero) {
                OAL.alcCloseDevice(m_device);
                m_device = IntPtr.Zero;
            }
        }
    }
}