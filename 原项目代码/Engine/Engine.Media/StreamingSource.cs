namespace Engine.Media {
    public abstract class StreamingSource : IDisposable {
        public abstract int ChannelsCount { get; }

        public abstract int SamplingFrequency { get; }

        public virtual long Position { get; set; }

        public abstract long BytesCount { get; }

        public virtual void Dispose() { }

        public abstract int Read(byte[] buffer, int offset, int count);

        public abstract StreamingSource Duplicate();

        public void CopyTo(Stream stream) {
            byte[] array = new byte[4096];
            int num;
            do {
                num = Read(array, 0, array.Length);
                stream.Write(array, 0, num);
            }
            while (num >= array.Length);
        }
    }
}