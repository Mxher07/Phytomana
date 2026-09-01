using System.Runtime.InteropServices;
using Engine.Media;
#if BROWSER
using SourceInteger = Engine.Browser.AL.SourceInteger;
using GetSourceInteger = Engine.Browser.AL.GetSourceInteger;
using BufferFormat = Engine.Browser.AL.BufferFormat;
using SourceVector3 = Engine.Browser.AL.SourceVector3;
using SourceState = Engine.Browser.AL.SourceState;
#else
using Silk.NET.OpenAL;
#endif

namespace Engine.Audio {
    public class StreamingSound : BaseSound {
        Task m_task;
        ManualResetEvent m_stopTaskEvent = new(false);

        bool m_noMoreData;
        public readonly float m_bufferDuration;

        public StreamingSource StreamingSource { get; set; }

        public int ReadStreamingSource(byte[] buffer, int count) {
            int num = 0;
            if (StreamingSource.BytesCount > 0) {
                while (count > 0) {
                    int num2 = StreamingSource.Read(buffer, num, count);
                    if (num2 > 0) {
                        num += num2;
                        count -= num2;
                        continue;
                    }
                    if (!m_isLooped) {
                        break;
                    }
                    StreamingSource.Position = 0L;
                }
            }
            return num;
        }

        void VerifyStreamingSource(StreamingSource streamingSource) {
            ArgumentNullException.ThrowIfNull(streamingSource);
            if (streamingSource.ChannelsCount < 1
                || streamingSource.ChannelsCount > 2) {
                throw new InvalidOperationException("Unsupported channels count.");
            }
            if (streamingSource.SamplingFrequency < 8000
                || streamingSource.SamplingFrequency > 192000) {
                throw new InvalidOperationException("Unsupported frequency.");
            }
        }

        public StreamingSound(StreamingSource streamingSource,
            float volume = 1f,
            float pitch = 1f,
            float pan = 0f,
            bool isLooped = false,
            bool disposeOnStop = false,
            float bufferDuration = 0.3f) {
            VerifyStreamingSource(streamingSource);
            StreamingSource = streamingSource;
            ChannelsCount = streamingSource.ChannelsCount;
            SamplingFrequency = streamingSource.SamplingFrequency;
            Volume = volume;
            Pitch = pitch;
            Pan = pan;
            IsLooped = isLooped;
            DisposeOnStop = disposeOnStop;
            m_bufferDuration = Math.Clamp(bufferDuration, 0.01f, 10f);
            if (m_source == 0) {
                return;
            }
            m_task = Task.Run(
                delegate {
                    try {
                        StreamingThreadFunction();
                    }
                    catch (Exception message) {
                        Log.Error(message);
                    }
                }
            );
        }

        internal override void InternalPlay(Vector3 direction) {
            if (m_source != 0) {
                uint source = (uint)m_source;
                Mixer.AL.SetSourceProperty(source, SourceVector3.Position, direction.X, direction.Y, direction.Z);
                Mixer.AL.SourcePlay(source);
                Mixer.CheckALError();
            }
        }

        internal override void InternalPause() {
            if (m_source != 0) {
                Mixer.AL.SourcePause((uint)m_source);
                Mixer.CheckALError();
            }
        }

        internal override void InternalStop() {
            if (m_source != 0) {
                Mixer.AL.SourceStop((uint)m_source);
                Mixer.CheckALError();
                StreamingSource.Position = 0L;
                lock (m_lock) {
                    m_noMoreData = false;
                }
            }
        }

        internal override void InternalDispose() {
            if (m_stopTaskEvent != null
                && m_task != null) {
                m_stopTaskEvent.Set();
                m_task.Wait();
                m_task = null;
                m_stopTaskEvent.Dispose();
                m_stopTaskEvent = null;
            }
            if (StreamingSource != null) {
                StreamingSource.Dispose();
                StreamingSource = null;
            }
            base.InternalDispose();
        }

        unsafe void StreamingThreadFunction() {
            uint[] array = new uint[3];
            List<uint> list = new();
            int millisecondsTimeout = Math.Clamp((int)(0.5f * m_bufferDuration / array.Length * 1000f), 1, 100);
            byte[] array2 = new byte[2 * ChannelsCount * (int)(SamplingFrequency * m_bufferDuration / array.Length)];
            for (int i = 0; i < array.Length; i++) {
                uint num = Mixer.AL.GenBuffer();
                Mixer.CheckALError();
                array[i] = num;
                list.Add(num);
            }
            uint source = (uint)m_source;
            do {
                lock (m_lock) {
                    if (!m_noMoreData) {
                        Mixer.AL.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out int value);
                        Mixer.CheckALError();
                        for (int j = 0; j < value; j++) {
                            uint item = 0u;
                            Mixer.AL.SourceUnqueueBuffers(source, 1, &item);
                            Mixer.CheckALError();
                            list.Add(item);
                        }
                        if (list.Count > 0
                            && !m_noMoreData
                            && State == SoundState.Playing) {
                            int num2 = ReadStreamingSource(array2, array2.Length);
                            m_noMoreData = num2 < array2.Length;
                            if (num2 > 0) {
                                uint num3 = list[^1];
                                GCHandle gCHandle = GCHandle.Alloc(array2, GCHandleType.Pinned);
                                Mixer.AL.BufferData(
                                    num3,
                                    ChannelsCount == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16,
                                    gCHandle.AddrOfPinnedObject().ToPointer(),
                                    num2,
                                    SamplingFrequency
                                );
                                Mixer.CheckALError();
                                Mixer.AL.SourceQueueBuffers(source, 1, &num3);
                                Mixer.CheckALError();
                                list.RemoveAt(list.Count - 1);
                                Mixer.AL.GetSourceProperty(source, GetSourceInteger.SourceState, out int sourceState);
                                Mixer.CheckALError();
                                if (sourceState != (int)SourceState.Playing) {
                                    Mixer.AL.SourcePlay(source);
                                    Mixer.CheckALError();
                                }
                            }
                        }
                    }
                    else {
                        Mixer.AL.GetSourceProperty(source, GetSourceInteger.SourceState, out int sourceState);
                        if (sourceState == (int)SourceState.Stopped) {
                            Dispatcher.Dispatch(delegate { Stop(); });
                        }
                    }
                }
            }
            while (!m_stopTaskEvent.WaitOne(millisecondsTimeout));
            Mixer.AL.SourceStop(source);
            Mixer.CheckALError();
            Mixer.AL.SetSourceProperty(source, SourceInteger.Buffer, 0);
            Mixer.CheckALError();
            for (int k = 0; k < array.Length; k++) {
                if (array[k] != 0) {
                    Mixer.AL.DeleteBuffer(array[k]);
                    Mixer.CheckALError();
                    array[k] = 0;
                }
            }
        }
    }
}