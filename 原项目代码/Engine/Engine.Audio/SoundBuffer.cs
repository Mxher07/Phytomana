using System.Runtime.InteropServices;
using Engine.Media;
#if BROWSER
using BufferFormat = Engine.Browser.AL.BufferFormat;
#else
using Silk.NET.OpenAL;
#endif

namespace Engine.Audio {
    public class SoundBuffer : IDisposable {
        public int m_buffer;

        public int ChannelsCount { get; set; }

        public int SamplingFrequency { get; set; }

        public int SamplesCount { get; set; }

        public int UseCount { get; set; }

        public SoundBuffer(byte[] data, int startIndex, int itemsCount, int channelsCount, int samplingFrequency) {
            Initialize(data, startIndex, itemsCount, channelsCount, samplingFrequency);
            CreateBuffer(data, startIndex, itemsCount, channelsCount, samplingFrequency);
        }

        public SoundBuffer(short[] data, int startIndex, int itemsCount, int channelsCount, int samplingFrequency) {
            Initialize(data, startIndex, itemsCount, channelsCount, samplingFrequency);
            CreateBuffer(data, startIndex, itemsCount, channelsCount, samplingFrequency);
        }

        public SoundBuffer(Stream stream, int bytesCount, int channelsCount, int samplingFrequency) {
            byte[] array = Initialize(stream, bytesCount, channelsCount, samplingFrequency);
            CreateBuffer(array, 0, array.Length, channelsCount, samplingFrequency);
        }

        public SoundBuffer() { }

        void InternalDispose() {
            if (m_buffer != 0) {
                Mixer.AL.DeleteBuffer((uint)m_buffer);
                Mixer.CheckALError();
                m_buffer = 0;
            }
        }

        unsafe void CreateBuffer<T>(T[] data, int startIndex, int itemsCount, int channelsCount, int samplingFrequency) where T : unmanaged {
            uint buffer = Mixer.AL.GenBuffer();
            m_buffer = (int)buffer;
            Mixer.CheckALError();
            GCHandle gCHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                int num = Utilities.SizeOf<T>();
                Mixer.AL.BufferData(
                    buffer,
                    channelsCount == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16,
                    (gCHandle.AddrOfPinnedObject() + startIndex * num).ToPointer(),
                    itemsCount * num,
                    samplingFrequency
                );
                Mixer.CheckALError();
            }
            finally {
                gCHandle.Free();
            }
        }

        public void Dispose() {
            if (UseCount != 0) {
                throw new InvalidOperationException("无法处置正在使用的SoundBuffer");
            }
            InternalDispose();
        }

        public static SoundBuffer Load(SoundData soundData) => new(
            soundData.Data,
            0,
            soundData.Data.Length,
            soundData.ChannelsCount,
            soundData.SamplingFrequency
        );

        public static SoundBuffer Load(Stream stream, SoundFileFormat format) => Load(SoundData.Load(stream, format));

        public static SoundBuffer Load(string fileName, SoundFileFormat format) => Load(SoundData.Load(fileName, format));

        public static SoundBuffer Load(Stream stream) => Load(SoundData.Load(stream));

        public static SoundBuffer Load(string fileName) => Load(SoundData.Load(fileName));

        public void InitializeProperties(int samplesCount, int channelsCount, int samplingFrequency) {
            if (samplesCount <= 0) {
                throw new InvalidOperationException("Buffer cannot have zero samples.");
            }
            if (channelsCount < 1
                || channelsCount > 2) {
                throw new ArgumentOutOfRangeException(nameof(channelsCount));
            }
            if (samplingFrequency < 8000
                || samplingFrequency > 192000) {
                throw new ArgumentOutOfRangeException(nameof(samplingFrequency));
            }
            ChannelsCount = channelsCount;
            SamplingFrequency = samplingFrequency;
            SamplesCount = samplesCount;
        }

        void Initialize<T>(T[] data, int startIndex, int itemsCount, int channelsCount, int samplingFrequency) where T : unmanaged {
            int num = Utilities.SizeOf<T>();
            InitializeProperties(itemsCount * num / channelsCount / 2, channelsCount, samplingFrequency);
            ArgumentNullException.ThrowIfNull(data);
            if (startIndex + itemsCount > data.Length) {
                throw new ArgumentOutOfRangeException(nameof(itemsCount));
            }
        }

        byte[] Initialize(Stream stream, int bytesCount, int channelsCount, int samplingFrequency) {
            byte[] array = new byte[bytesCount];
            if (stream.Read(array, 0, bytesCount) != bytesCount) {
                throw new InvalidOperationException("Not enough data in stream.");
            }
            Initialize(array, 0, bytesCount, channelsCount, samplingFrequency);
            return array;
        }
    }
}