namespace Engine.Media {
    public class SoundData {
        public int ChannelsCount { get; set; }

        public int SamplingFrequency { get; set; }

        public short[] Data { get; set; }

        public SoundData(int channelsCount, int samplingFrequency, int bytesCount) {
            if (channelsCount < 1
                || channelsCount > 2) {
                throw new ArgumentOutOfRangeException(nameof(channelsCount));
            }
            if (samplingFrequency < 8000
                || samplingFrequency > 192000) {
                throw new ArgumentOutOfRangeException(nameof(samplingFrequency));
            }
            if (bytesCount < 0
                || bytesCount % (2 * channelsCount) != 0) {
                throw new ArgumentOutOfRangeException(nameof(bytesCount));
            }
            ChannelsCount = channelsCount;
            SamplingFrequency = samplingFrequency;
            Data = new short[bytesCount / 2];
        }

        public static SoundFileFormat DetermineFileFormat(string extension) {
            if (extension.Equals(".flac", StringComparison.OrdinalIgnoreCase)) {
                return SoundFileFormat.Flac;
            }
            if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)) {
                return SoundFileFormat.Wav;
            }
            return extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase) ? SoundFileFormat.Ogg :
                extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) ? SoundFileFormat.Mp3 :
                throw new InvalidOperationException("Unsupported sound file format.");
        }

        public static SoundFileFormat DetermineFileFormat(Stream stream) {
            if (Flac.IsFlacStream(stream)) {
                return SoundFileFormat.Flac;
            }
            if (Wav.IsWavStream(stream)) {
                return SoundFileFormat.Wav;
            }
            return Ogg.IsOggStream(stream) ? SoundFileFormat.Ogg :
                Mp3.IsFlacStream(stream) ? SoundFileFormat.Mp3 : throw new InvalidOperationException("Unsupported sound file format.");
        }

        public static StreamingSource Stream(Stream stream, SoundFileFormat format) {
            switch (format) {
                case SoundFileFormat.Flac: return Flac.Stream(stream);
                case SoundFileFormat.Wav: return Wav.Stream(stream);
                case SoundFileFormat.Ogg: return Ogg.Stream(stream);
                case SoundFileFormat.Mp3: return Mp3.Stream(stream);
                default: throw new InvalidOperationException("Unsupported sound file format.");
            }
        }

        public static StreamingSource Stream(string fileName, SoundFileFormat format) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Stream(stream, format);
            }
        }

        public static StreamingSource Stream(Stream stream) {
            SoundFileFormat format = DetermineFileFormat(stream);
            stream.Position = 0L;
            return Stream(stream, format);
        }

        public static StreamingSource Stream(string fileName) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Stream(stream);
            }
        }

        public static SoundData Load(Stream stream, SoundFileFormat format) {
            switch (format) {
                case SoundFileFormat.Wav: return Wav.Load(stream);
                case SoundFileFormat.Ogg: return Ogg.Load(stream);
                case SoundFileFormat.Flac: return Flac.Load(stream);
                case SoundFileFormat.Mp3: return Mp3.Load(stream);
                default: throw new InvalidOperationException("Unsupported sound file format.");
            }
        }

        public static SoundData Load(string fileName, SoundFileFormat format) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(stream, format);
            }
        }

        public static SoundData Load(Stream stream) {
            PeekStream peekStream = new(stream, 64);
            SoundFileFormat format = DetermineFileFormat(peekStream.GetInitialBytesStream());
            return Load(peekStream, format);
        }

        public static SoundData Load(string fileName) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(stream);
            }
        }

        public static void Save(SoundData soundData, Stream stream, SoundFileFormat format) {
            if (format == SoundFileFormat.Wav) {
                Wav.Save(soundData, stream);
                return;
            }
            throw new InvalidOperationException("Unsupported sound file format to save.");
        }

        public static void Save(SoundData soundData, string fileName, SoundFileFormat format) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Create)) {
                Save(soundData, stream, format);
            }
        }

        public static void StereoToMono(SoundData soundData) {
            if (soundData.ChannelsCount != 2) {
                throw new InvalidOperationException("SoundData is not stereo.");
            }
            short[] array = new short[soundData.Data.Length / 2];
            for (int i = 0; i < array.Length; i++) {
                array[i] = (short)((soundData.Data[2 * i] + soundData.Data[2 * i + 1]) / 2);
            }
            soundData.ChannelsCount = 1;
            soundData.Data = array;
        }
    }
}