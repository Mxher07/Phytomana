using NVorbis;

namespace Engine.Media {
    public static class Ogg {
        public class OggStreamingSource : StreamingSource {
            public VorbisReader m_reader;

            public Stream m_stream;

            public float[] m_samples = new float[1024];

            public override int ChannelsCount => m_reader.Channels;

            public override int SamplingFrequency => m_reader.SampleRate;

            public override long Position {
                get => m_reader.SamplePosition;
                set => m_reader.SamplePosition = value;
            }

            public override long BytesCount => m_reader.TotalSamples * 2;

            public OggStreamingSource(Stream stream, bool leaveOpen = false) {
                MemoryStream memoryStream = new();
                stream.Position = 0L;
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0L;
                m_stream = memoryStream;
                m_reader = new VorbisReader(m_stream, leaveOpen);
            }

            public override void Dispose() {
                m_reader.Dispose();
            }

            public override int Read(byte[] buffer, int offset, int count) {
                ArgumentNullException.ThrowIfNull(buffer);
                if (offset < 0
                    || count < 0
                    || offset + count > buffer.Length) {
                    throw new InvalidOperationException("Invalid range.");
                }
                int num = 0;
                while (count >= 2) {
                    int count2 = Math.Min(count / 2, m_samples.Length);
                    int num2 = m_reader.ReadSamples(m_samples, 0, count2);
                    if (num2 == 0) {
                        break;
                    }
                    num += num2;
                    if (BitConverter.IsLittleEndian) {
                        for (int i = 0; i < num2; i++) {
                            short num3 = (short)(m_samples[i] * 32767f);
                            buffer[offset++] = (byte)num3;
                            buffer[offset++] = (byte)(num3 >> 8);
                        }
                    }
                    else {
                        for (int j = 0; j < num2; j++) {
                            short num4 = (short)(m_samples[j] * 32767f);
                            buffer[offset++] = (byte)(num4 >> 8);
                            buffer[offset++] = (byte)num4;
                        }
                    }
                    count -= num2 * 2;
                }
                return num * 2;
            }

            /// <summary>
            ///     复制出一个新的流
            /// </summary>
            /// <returns></returns>
            public override StreamingSource Duplicate() {
                MemoryStream memoryStream = new();
                m_stream.Position = 0L;
                m_stream.CopyTo(memoryStream);
                memoryStream.Position = 0L;
                return new OggStreamingSource(memoryStream);
            }
        }

        public static bool IsOggStream(Stream stream) {
            long position = stream.Position;
            int num = stream.ReadByte();
            int num2 = stream.ReadByte();
            int num3 = stream.ReadByte();
            int num4 = stream.ReadByte();
            stream.Position = position;
            return num == 79 && num2 == 103 && num3 == 103 && num4 == 83;
        }

        public static StreamingSource Stream(Stream stream, bool leaveOpen = false) {
            ArgumentNullException.ThrowIfNull(stream);
            return new OggStreamingSource(stream, leaveOpen);
        }

        public static SoundData Load(Stream stream) {
            using (StreamingSource streamingSource = Stream(stream, true)) {
                if (streamingSource.BytesCount > int.MaxValue) {
                    throw new InvalidOperationException("Sound data too long.");
                }
                byte[] array = new byte[(int)streamingSource.BytesCount];
                streamingSource.Read(array, 0, array.Length);
                SoundData soundData = new(streamingSource.ChannelsCount, streamingSource.SamplingFrequency, array.Length);
                Buffer.BlockCopy(array, 0, soundData.Data, 0, array.Length);
                return soundData;
            }
        }
    }
}