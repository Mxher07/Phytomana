namespace Engine.Browser {
    public class AL {
        public enum GetSourceInteger {
            ByteOffset = 0x1026,
            SampleOffset = 0x1025,
            Buffer = 0x1009,
            SourceState = 0x1010,
            BuffersQueued = 0x1015,
            BuffersProcessed = 0x1016,
            SourceType = 0x1027
        }

        public enum SourceFloat {
            ReferenceDistance = 0x1020,
            MaxDistance = 0x1023,
            RolloffFactor = 0x1021,
            Pitch = 0x1003,
            Gain = 0x100A,
            MinGain = 0x100D,
            MaxGain = 0x100E,
            ConeInnerAngle = 0x1001,
            ConeOuterAngle = 0x1002,
            ConeOuterGain = 0x1022,
            SecOffset = 0x1024
        }

        public enum SourceVector3 {
            Position = 0x1004,
            Velocity = 0x1006,
            Direction = 0x1005
        }

        public enum SourceInteger {
            ByteOffset = 0x1026, // AL_EXT_OFFSET extension.
            SampleOffset = 0x1025, // AL_EXT_OFFSET extension.
            Buffer = 0x1009,
            SourceType = 0x1027
        }

        public enum SourceBoolean {
            SourceRelative = 0x202,
            Looping = 0x1007
        }

        public enum BufferFormat {
            Mono8 = 0x1100,
            Mono16 = 0x1101,
            Stereo8 = 0x1102,
            Stereo16 = 0x1103
        }

        public enum ListenerFloat {
            Gain = 0x100A
        }

        public enum DistanceModelEnum {
            None = 0,
            InverseDistance = 0xD001,
            InverseDistanceClamped = 0xD002,
            LinearDistance = 0xD003,
            LinearDistanceClamped = 0xD004,
            ExponentDistance = 0xD005,
            ExponentDistanceClamped = 0xD006
        }

        public enum AudioError {
            NoError = 0,
            InvalidName = 0xA001,
            IllegalEnum = 0xA002,
            InvalidEnum = 0xA002,
            InvalidValue = 0xA003,
            IllegalCommand = 0xA004,
            InvalidOperation = 0xA004,
            OutOfMemory = 0xA005
        }

        public enum SourceState {
            Initial = 0x1011,
            Playing = 0x1012,
            Paused = 0x1013,
            Stopped = 0x1014
        }

        public uint GenSource() {
            OAL.alGenSources(1, out uint source);
            return source;
        }

        public void DeleteSource(uint source) => OAL.alDeleteSources(1, ref source);
        public void GetSourceProperty(uint source, GetSourceInteger param, out int value) => OAL.alGetSourcei(source, (int)param, out value);

        public void SetSourceProperty(uint source, SourceFloat param, float value) => OAL.alSourcef(source, (int)param, value);

        public void SetSourceProperty(uint source, SourceVector3 param, float value1, float value2, float value3) =>
            OAL.alSource3f(source, (int)param, value1, value2, value3);

        public void SetSourceProperty(uint source, SourceInteger param, int value) => OAL.alSourcei(source, (int)param, value);

        public void SetSourceProperty(uint source, SourceBoolean param, bool value) => OAL.alSourcei(source, (int)param, value ? 1 : 0);

        public void SourcePlay(uint source) => OAL.alSourcePlay(source);

        public void SourcePause(uint source) => OAL.alSourcePause(source);

        public void SourceStop(uint source) => OAL.alSourceStop(source);

        public void SourceRewind(uint source) => OAL.alSourceRewind(source);

        public uint GenBuffer() {
            OAL.alGenBuffers(1, out uint buffer);
            return buffer;
        }

        public void DeleteBuffer(uint buffer) => OAL.alDeleteBuffers(1, ref buffer);

        public unsafe void BufferData(uint buffer, BufferFormat format, void* data, int size, int frequency) =>
            OAL.alBufferData(buffer, (int)format, data, size, frequency);

        public unsafe void SourceUnqueueBuffers(uint source, int count, uint* buffers) => OAL.alSourceUnqueueBuffers(source, count, buffers);

        public unsafe void SourceQueueBuffers(uint source, int count, uint* buffers) => OAL.alSourceQueueBuffers(source, count, buffers);
        public void SetListenerProperty(ListenerFloat param, float value) => OAL.alListenerf((int)param, value);

        public void DistanceModel(DistanceModelEnum model) => OAL.alDistanceModel((int)model);
        public AudioError GetError() => (AudioError)OAL.alGetError();
    }
}