#if IOS

using Foundation;
using ObjCRuntime;
using System.Runtime.InteropServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Flac;

namespace Engine.Media {
    public static class Flac {
        public class FlacStreamingSource : StreamingSource {

            public Stream m_stream;

            private static Dictionary<nint, Stream> streamMap = new();

            private static nint globalStreamId;

            int m_channels;

            int m_sampleRate;

            MemoryStream pcm_stream;
            
            public override int ChannelsCount => m_channels;

            public override int SamplingFrequency => m_sampleRate;

            public override long Position => pcm_stream.Position;

            public override long BytesCount => pcm_stream.Length;
            
            public FlacStreamingSource(Stream stream) {
                m_stream = stream;
                pcm_stream = new MemoryStream();//解码后的pcm数据
                BASS_FILEPROCS fileProcs = new BASS_FILEPROCS(CloseProc, LengthProc, ReadProc, SeekProc);
                ++globalStreamId;
                m_stream.Position = 0L;
                streamMap[globalStreamId] = m_stream;

                int streamID = BassFlac.BASS_FLAC_StreamCreateFileUser(BASSStreamSystem.STREAMFILE_BUFFER, BASSFlag.BASS_STREAM_DECODE, fileProcs, globalStreamId);
                if (streamID == 0) throw new Exception("BASS_FLAC_StreamCreateFileUser failed: " + Bass.BASS_ErrorGetCode());
                BASS_CHANNELINFO info = Bass.BASS_ChannelGetInfo(streamID);
                m_channels = info.chans;
                m_sampleRate = info.freq;
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = Bass.BASS_ChannelGetData(streamID, buffer, buffer.Length)) > 0) {
                    pcm_stream.Write(buffer, 0, bytesRead);
                }
                pcm_stream.Position = 0L;
                Bass.BASS_StreamFree(streamID);
            }

            [MonoPInvokeCallback(typeof(FILELENPROC))]
            private static long LengthProc(IntPtr user) {
                var stream = streamMap[user];
                return stream.Length;
            }

            [MonoPInvokeCallback(typeof(FILESEEKPROC))]
            private static bool SeekProc(long offset, IntPtr user) {
                var stream = streamMap[user];
                stream.Seek(offset,SeekOrigin.Begin);
                return true;
            }

            [MonoPInvokeCallback(typeof(FILEREADPROC))]
            private static int ReadProc(IntPtr buffer, int length, IntPtr user) {
                var stream = streamMap[user];
                byte[] readBuffer = new byte[length];
                int bytesRead = stream.Read(readBuffer, 0, length);
                Marshal.Copy(readBuffer, 0, buffer, bytesRead);
                return bytesRead;
            }

            [MonoPInvokeCallback(typeof(FILECLOSEPROC))]
            private static void CloseProc(IntPtr user) {
                var stream = streamMap[user];
                stream.Position = 0L;
            }

            public override void Dispose() {
                m_stream.Dispose();
                pcm_stream.Dispose();
            }

            public override int Read(byte[] buffer, int offset, int count) {
                ArgumentNullException.ThrowIfNull(buffer);
                if (offset < 0
                    || count < 0
                    || offset + count > buffer.Length) {
                    throw new InvalidOperationException("Invalid range.");
                }
                int num = pcm_stream.Read(buffer, offset, (int)Math.Min(count, BytesCount - Position));
                return num;
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
                return new FlacStreamingSource(memoryStream);
            }
        }

        public static bool IsFlacStream(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            long position = stream.Position;
            stream.Position = 0;
            byte[] beginSync = new byte[4];
            int read = stream.Read(beginSync, 0, beginSync.Length);
            stream.Position = position;
            return read < beginSync.Length
                ? throw new EndOfStreamException("Can not read \"fLaC\" sync.")
                : beginSync[0] == 0x66 && beginSync[1] == 0x4C && beginSync[2] == 0x61 && beginSync[3] == 0x43;
        }

        public static StreamingSource Stream(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            return new FlacStreamingSource(stream);
        }

        public static SoundData Load(Stream stream) {
            using (StreamingSource streamingSource = new FlacStreamingSource(stream)) {
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



#else

using NAudio.Flac;

namespace Engine.Media {
    public static class Flac {
        public class FlacStreamingSource : StreamingSource {
            public FlacReader m_reader;

            public Stream m_stream;

            long m_position;

            public override int ChannelsCount => m_reader.WaveFormat.Channels;

            public override int SamplingFrequency => m_reader.WaveFormat.SampleRate;

            public override long Position {
                get => m_position;
                set {
                    m_reader.Position = value;
                    if (m_reader.CanSeek) {
                        long num = value * ChannelsCount * 2;
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

            public override long BytesCount => m_reader.Length;

            public FlacStreamingSource(Stream stream) {
                m_stream = new MemoryStream();
                stream.Position = 0L;
                stream.CopyTo(m_stream);
                m_stream.Position = 0L;
                m_reader = new FlacReader(m_stream);
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
                int num = m_reader.Read(buffer, offset, (int)Math.Min(count, BytesCount - Position));
                m_position += num / 2 / ChannelsCount;
                return num;
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
                return new FlacStreamingSource(memoryStream);
            }
        }

        public static bool IsFlacStream(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            long position = stream.Position;
            stream.Position = 0;
            ID3v2.SkipTag(stream);
            byte[] beginSync = new byte[4];
            int read = stream.Read(beginSync, 0, beginSync.Length);
            stream.Position = position;
            return read < beginSync.Length
                ? throw new EndOfStreamException("Can not read \"fLaC\" sync.")
                : beginSync[0] == 0x66 && beginSync[1] == 0x4C && beginSync[2] == 0x61 && beginSync[3] == 0x43;
        }

        public static StreamingSource Stream(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            return new FlacStreamingSource(stream);
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

#endif