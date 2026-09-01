using NAudio.Wave;
using NLayer.NAudioSupport;

namespace Engine.Media {
    public static class Mp3 {
        public class Mp3StreamingSource : StreamingSource {
            public Mp3FileReaderBase m_reader;

            public Stream m_stream;

            long m_position;

            public override int ChannelsCount => m_reader.WaveFormat.Channels;

            public override int SamplingFrequency => m_reader.WaveFormat.SampleRate;

            public override long Position {
                get => m_position;
                set {
                    if (m_reader.CanSeek) {
                        long num = value * ChannelsCount * 4; //NLayer获取到的是32位float拆分成的8位byte数组
                        if (num < 0
                            || num > BytesCount) {
                            throw new ArgumentOutOfRangeException();
                        }
                        m_reader.Position = num;
                        m_position = value;
                        return;
                    }
                    throw new NotSupportedException("Underlying stream cannot be seeked.");
                }
            }

            public override long BytesCount => m_reader.Length / 2;

            public Mp3StreamingSource(Stream stream) {
                m_stream = new MemoryStream();
                stream.Position = 0L;
                stream.CopyTo(m_stream);
                m_stream.Position = 0L;
                m_reader = new Mp3FileReaderBase(m_stream, wf => new Mp3FrameDecompressor(wf));
            }

            public override void Dispose() {
                m_reader.Dispose();
                m_stream.Dispose();
            }

            public override int Read(byte[] buffer, int offset, int count) {
                ArgumentNullException.ThrowIfNull(buffer);
                if (offset < 0
                    || count < 0
                    || offset + count > buffer.Length) {
                    throw new InvalidOperationException("Invalid range.");
                }
                count = (int)Math.Min(count, BytesCount - Position);
                byte[] sample = new byte[count * 2];
                int num = m_reader.Read(sample, 0, count * 2);
                for (int i = 0; i < num; i += 4) {
                    float sample32Bit = BitConverter.ToSingle(sample, i);
                    short sample16Bit = (short)(sample32Bit * short.MaxValue);
                    byte[] sampleBytes = BitConverter.GetBytes(sample16Bit);
                    buffer[offset++] = sampleBytes[0];
                    buffer[offset++] = sampleBytes[1];
                }
                m_position += num / 2 / ChannelsCount;
                return num / 2;
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
                return new Mp3StreamingSource(memoryStream);
            }
        }

        public static bool IsFlacStream(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            long position = stream.Position;
            stream.Position = 0;
            bool result = Id3v2Tag.ReadTag(stream) != null;
            stream.Position = position;
            return result;
        }

        public static StreamingSource Stream(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            return new Mp3StreamingSource(stream);
        }

        public static SoundData Load(Stream stream) {
            using (StreamingSource streamingSource = Stream(stream)) {
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