using System.Runtime.InteropServices;
using Engine.Serialization;

namespace Engine.Media {
    public static class Wav {
        public class WavStreamingSource : StreamingSource {
            public Stream m_stream;

            public bool m_leaveOpen;

            public int m_channelsCount;

            public int m_samplingFrequency;

            public long m_bytesCount;

            long m_position;

            public override int ChannelsCount => m_channelsCount;

            public override int SamplingFrequency => m_samplingFrequency;

            public override long BytesCount => m_bytesCount;

            public override long Position {
                get => m_position;
                set {
                    if (m_stream.CanSeek) {
                        long num = value * ChannelsCount * 2;
                        if (num < 0
                            || num > BytesCount) {
                            throw new ArgumentOutOfRangeException();
                        }
                        m_stream.Position = Utilities.SizeOf<WavHeader>() + num;
                        m_position = value;
                        return;
                    }
                    throw new NotSupportedException("Underlying stream cannot be seeked.");
                }
            }

            public WavStreamingSource(Stream stream, bool leaveOpen = false) {
                MemoryStream memoryStream = new();
                stream.Position = 0L;
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0L;
                m_stream = memoryStream;
                m_leaveOpen = leaveOpen;
                ReadHeaders(m_stream, out FmtHeader fmtHeader, out DataHeader dataHeader, out long dataStart);
                m_channelsCount = fmtHeader.ChannelsCount;
                m_samplingFrequency = fmtHeader.SamplingFrequency;
                m_bytesCount = dataHeader.DataSize;
                m_stream.Position = dataStart;
            }

            public override void Dispose() {
                if (!m_leaveOpen) {
                    m_stream.Dispose();
                }
                m_stream = null;
            }

            public override int Read(byte[] buffer, int offset, int count) {
                if (count % (2 * ChannelsCount) != 0) {
                    throw new InvalidOperationException("Cannot read partial samples.");
                }
                count = (int)Math.Min(count, m_bytesCount - m_position * 2 * ChannelsCount);
                int num = m_stream.Read(buffer, offset, count);
                m_position += num / 2 / ChannelsCount;
                return num;
            }

            public override StreamingSource Duplicate() {
                MemoryStream memoryStream = new();
                m_stream.Position = 0L;
                m_stream.CopyTo(memoryStream);
                memoryStream.Position = 0L;
                return new WavStreamingSource(memoryStream);
            }
        }

        public struct WavInfo {
            public int ChannelsCount;

            public int SamplingFrequency;

            public int BytesCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct WavHeader {
            public int Riff;

            public int FileSize;

            public int Wave;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FmtHeader {
            public int Fmt;

            public int FormatSize;

            public short Type;

            public short ChannelsCount;

            public int SamplingFrequency;

            public int BytesPerSecond;

            public short BytesPerSample;

            public short BitsPerChannel;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DataHeader {
            public int Data;

            public int DataSize;
        }

        public static bool IsWavStream(Stream stream) {
            BinaryReader binaryReader = new(stream);
            if (stream.Length - stream.Position >= Utilities.SizeOf<WavHeader>()) {
                int num = binaryReader.ReadInt32();
                int num2 = binaryReader.ReadInt32();
                int num3 = binaryReader.ReadInt32();
                stream.Position -= 12L;
                if (num == MakeFourCC("RIFF")
                    && num2 != 0
                    && num3 == MakeFourCC("WAVE")) {
                    return true;
                }
            }
            return false;
        }

        public static WavInfo GetInfo(Stream stream) {
            ReadHeaders(stream, out FmtHeader fmtHeader, out DataHeader dataHeader, out long _);
            WavInfo result = default;
            result.ChannelsCount = fmtHeader.ChannelsCount;
            result.SamplingFrequency = fmtHeader.SamplingFrequency;
            result.BytesCount = dataHeader.DataSize;
            return result;
        }

        public static StreamingSource Stream(Stream stream) => new WavStreamingSource(stream);

        public static SoundData Load(Stream stream) {
            ReadHeaders(stream, out FmtHeader fmtHeader, out DataHeader dataHeader, out long dataStart);
            stream.Position = dataStart;
            SoundData soundData = new(fmtHeader.ChannelsCount, fmtHeader.SamplingFrequency, dataHeader.DataSize);
            byte[] array = new byte[dataHeader.DataSize];
            if (stream.Read(array, 0, array.Length) != array.Length) {
                throw new InvalidOperationException("Truncated WAV data.");
            }
            Buffer.BlockCopy(array, 0, soundData.Data, 0, array.Length);
            return soundData;
        }

        public static void Save(SoundData soundData, Stream stream) {
            ArgumentNullException.ThrowIfNull(soundData);
            ArgumentNullException.ThrowIfNull(stream);
            EngineBinaryWriter engineBinaryWriter = new(stream);
            WavHeader structure = default;
            structure.Riff = MakeFourCC("RIFF");
            structure.FileSize = Utilities.SizeOf<WavHeader>()
                + Utilities.SizeOf<FmtHeader>()
                + Utilities.SizeOf<DataHeader>()
                + soundData.Data.Length;
            structure.Wave = MakeFourCC("WAVE");
            engineBinaryWriter.WriteStruct(structure);
            FmtHeader structure2 = default;
            structure2.Fmt = MakeFourCC("fmt ");
            structure2.FormatSize = 16;
            structure2.Type = 1;
            structure2.ChannelsCount = (short)soundData.ChannelsCount;
            structure2.SamplingFrequency = soundData.SamplingFrequency;
            structure2.BytesPerSecond = soundData.ChannelsCount * 2 * soundData.SamplingFrequency;
            structure2.BytesPerSample = (short)(soundData.ChannelsCount * 2);
            structure2.BitsPerChannel = 16;
            engineBinaryWriter.WriteStruct(structure2);
            DataHeader structure3 = default;
            structure3.Data = MakeFourCC("data");
            structure3.DataSize = soundData.Data.Length * 2;
            engineBinaryWriter.WriteStruct(structure3);
            byte[] array = new byte[soundData.Data.Length * 2];
            Buffer.BlockCopy(soundData.Data, 0, array, 0, array.Length);
            stream.Write(array, 0, array.Length);
        }

        public static void ReadHeaders(Stream stream, out FmtHeader fmtHeader, out DataHeader dataHeader, out long dataStart) {
            ArgumentNullException.ThrowIfNull(stream);
            if (!BitConverter.IsLittleEndian) {
                throw new InvalidOperationException("Unsupported system endianness.");
            }
            if (!IsWavStream(stream)) {
                throw new InvalidOperationException("Invalid WAV header.");
            }
            EngineBinaryReader engineBinaryReader = new(stream);
            fmtHeader = default;
            dataHeader = default;
            dataStart = 0L;
            stream.Position += 12L;
            bool flag = false;
            bool flag2 = false;
            while (!flag
                || !flag2) {
                int num = engineBinaryReader.ReadInt32();
                if (num == MakeFourCC("fmt ")) {
                    stream.Position -= 4L;
                    fmtHeader = engineBinaryReader.ReadStruct<FmtHeader>();
                    flag = true;
                }
                else if (num == MakeFourCC("data")) {
                    stream.Position -= 4L;
                    dataHeader = engineBinaryReader.ReadStruct<DataHeader>();
                    dataStart = stream.Position;
                    flag2 = true;
                }
                else {
                    int num2 = engineBinaryReader.ReadInt32();
                    stream.Position += num2;
                }
            }
            if (fmtHeader.Type != 1
                || fmtHeader.ChannelsCount < 1
                || fmtHeader.ChannelsCount > 2
                || fmtHeader.SamplingFrequency < 8000
                || fmtHeader.SamplingFrequency > 48000
                || fmtHeader.BitsPerChannel != 16) {
                throw new InvalidOperationException("Unsupported WAV format.");
            }
        }

        public static int MakeFourCC(string text) => (int)(((uint)text[3] << 24) | ((uint)text[2] << 16) | ((uint)text[1] << 8) | text[0]);
    }
}