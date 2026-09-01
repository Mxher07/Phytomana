using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using Engine;

namespace Game {
    public class TerrainSerializer23 : IDisposable {
        public interface IStorage : IDisposable {
            void Open(string directoryName, string suffix);

            int Load(Point2 coords, byte[] buffer);

            void Save(Point2 coords, byte[] buffer, int size);
        }

        public class SingleFileStorage : IStorage {
            public struct ChunkDescriptor {
                public int Index;

                public Point2 Coords;

                public int StartNode;
            }

            const string FileName = "Chunks32fs.dat";

            const uint FileHeaderMagic = 3735923200u;

            const int FileHeaderSize = 786444;

            const int FileHeaderFreeNodeOffset = 8;

            const int FileHeaderChunkDescriptorsOffset = 12;

            const int FileHeaderChunkDescriptorsCount = 65536;

            const int FileHeaderChunkDescriptorSize = 12;

            const uint NodeHeaderMagic = 3735927296u;

            const int NodeHeaderSize = 8;

            public Stream Stream;

            BinaryReader Reader;

            BinaryWriter Writer;

            Dictionary<Point2, ChunkDescriptor> ChunkDescriptors = [];

            int FreeNode;

            int NodeSize;

            int NodeDataSize => NodeSize - NodeHeaderSize;

            public virtual void Open(string directoryName, string suffix) {
                string path = Storage.CombinePaths(directoryName, FileName + suffix);
                try {
                    Stream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
                    Reader = new BinaryReader(Stream);
                    Writer = new BinaryWriter(Stream);
                    if (Stream.Length == 0L) {
                        FreeNode = -1;
                        NodeSize = 1024;
                        Writer.Write(ReverseEndianness(FileHeaderMagic));
                        Writer.Write(NodeSize);
                        Writer.Write(FreeNode);
                        for (int i = 0; i < FileHeaderChunkDescriptorsCount; i++) {
                            WriteChunkDescriptor(new ChunkDescriptor { Index = i, StartNode = -1 });
                        }
                    }
                }
                catch (Exception) {
                    Storage.DeleteFile(path);
                    throw;
                }
                Stream.Position = 0L;
                if (ReverseEndianness(Reader.ReadUInt32()) != FileHeaderMagic) {
                    throw new InvalidOperationException("Invalid chunks file header magic.");
                }
                NodeSize = Reader.ReadInt32();
                if (NodeSize < 64
                    || NodeSize > FileHeaderChunkDescriptorsCount) {
                    throw new InvalidOperationException("Invalid chunks file header node size.");
                }
                FreeNode = Reader.ReadInt32();
                for (int j = 0; j < FileHeaderChunkDescriptorsCount; j++) {
                    ChunkDescriptor value = ReadChunkDescriptor(j);
                    if (value.StartNode >= 0) {
                        ChunkDescriptors.Add(value.Coords, value);
                    }
                }
            }

            public virtual void Dispose() {
                if (Stream != null) {
                    Stream.Dispose();
                }
            }

            public virtual int Load(Point2 p, byte[] buffer) {
                if (!ChunkDescriptors.TryGetValue(p, out ChunkDescriptor value)) {
                    return -1;
                }
                int nextNode = value.StartNode;
                int num = 0;
                while (nextNode >= 0) {
                    num += ReadNode(nextNode, buffer, num, out nextNode);
                }
                return num;
            }

            public virtual void Save(Point2 p, byte[] buffer, int size) {
                int count = Math.Max((size + NodeDataSize - 1) / NodeDataSize, 1);
                List<int> freeNodes = GetFreeNodes(count);
                ReadNode(freeNodes.Last(), null, 0, out int nextNode);
                int num = 0;
                for (int i = 0; i < freeNodes.Count; i++) {
                    int num2 = Math.Min(size - num, NodeDataSize);
                    WriteNode(freeNodes[i], buffer, num, num2, i < freeNodes.Count - 1 ? freeNodes[i + 1] : -1);
                    num += num2;
                }
                if (!ChunkDescriptors.TryGetValue(p, out ChunkDescriptor value)) {
                    ChunkDescriptor chunkDescriptor = default;
                    chunkDescriptor.Index = ChunkDescriptors.Count % FileHeaderChunkDescriptorsCount;
                    chunkDescriptor.Coords = p;
                    chunkDescriptor.StartNode = freeNodes.First();
                    value = chunkDescriptor;
                    SetAndWriteFreeNode(nextNode);
                }
                else {
                    int node = FindLastNode(value.StartNode);
                    WriteNode(node, null, 0, 0, nextNode);
                    SetAndWriteFreeNode(value.StartNode);
                    value.StartNode = freeNodes.First();
                }
                WriteChunkDescriptor(value);
                ChunkDescriptors[p] = value;
            }

            public virtual List<int> GetFreeNodes(int count) {
                List<int> list = [];
                int nextNode = FreeNode;
                while (nextNode >= 0
                    && list.Count < count) {
                    list.Add(nextNode);
                    ReadNode(nextNode, null, 0, out nextNode);
                }
                if (list.Count < count) {
                    int num = count - list.Count;
                    int num2 = (int)((Stream.Length - FileHeaderSize) / NodeSize);
                    int num3 = num2 + num - 1;
                    Stream.SetLength(Stream.Length + NodeSize * num);
                    WriteNode(num3, null, 0, 0, -1);
                    if (list.Count > 0) {
                        WriteNode(list.Last(), null, 0, 0, num2);
                    }
                    else {
                        SetAndWriteFreeNode(num2);
                    }
                    for (int i = num2; i <= num3; i++) {
                        list.Add(i);
                    }
                }
                return list;
            }

            public virtual int FindLastNode(int startNode) {
                int num = startNode;
                while (true) {
                    ReadNode(num, null, 0, out int nextNode);
                    if (nextNode < 0) {
                        break;
                    }
                    num = nextNode;
                }
                return num;
            }

            public virtual void SetAndWriteFreeNode(int freeNode) {
                Stream.Position = NodeHeaderSize;
                Writer.Write(freeNode);
                FreeNode = freeNode;
            }

            public virtual ChunkDescriptor ReadChunkDescriptor(int i) {
                Stream.Position = FileHeaderChunkDescriptorsOffset + i * FileHeaderChunkDescriptorSize;
                ChunkDescriptor result = default;
                result.Index = i;
                result.Coords.X = Reader.ReadInt32();
                result.Coords.Y = Reader.ReadInt32();
                result.StartNode = Reader.ReadInt32();
                return result;
            }

            public virtual void WriteChunkDescriptor(ChunkDescriptor desc) {
                Stream.Position = FileHeaderChunkDescriptorsOffset + desc.Index * FileHeaderChunkDescriptorSize;
                Writer.Write(desc.Coords.X);
                Writer.Write(desc.Coords.Y);
                Writer.Write(desc.StartNode);
            }

            public virtual int ReadNode(int node, byte[] data, int offset, out int nextNode) {
                if (node < 0
                    || node >= (Stream.Length - FileHeaderSize) / NodeSize) {
                    throw new InvalidOperationException("Invalid node.");
                }
                Stream.Position = FileHeaderSize + node * NodeSize;
                if (ReverseEndianness(Reader.ReadUInt32()) != NodeHeaderMagic) {
                    throw new InvalidOperationException("Invalid node magic.");
                }
                int nodeHeader = Reader.ReadInt32();
                ParseNodeHeader(node, nodeHeader, out int dataSize, out nextNode);
                if (data != null
                    && Stream.Read(data, offset, dataSize) != dataSize) {
                    throw new InvalidOperationException("Truncated ChunksFile.");
                }
                return dataSize;
            }

            public virtual void WriteNode(int node, byte[] data, int offset, int size, int nextNode) {
                if (node < 0
                    || node >= (Stream.Length - FileHeaderSize) / NodeSize) {
                    throw new InvalidOperationException("Invalid node.");
                }
                Stream.Position = FileHeaderSize + node * NodeSize;
                int value = MakeNodeHeader(node, size, nextNode);
                Writer.Write(ReverseEndianness(NodeHeaderMagic));
                Writer.Write(value);
                if (data != null) {
                    Stream.Write(data, offset, size);
                }
            }

            public virtual int MakeNodeHeader(int node, int dataSize, int nextNode) {
                if (nextNode < 0) {
                    return (dataSize << 1) | 1;
                }
                return (nextNode - (node + 1)) << 1;
            }

            public virtual void ParseNodeHeader(int node, int nodeHeader, out int dataSize, out int nextNode) {
                if (((uint)nodeHeader & 1u) != 0) {
                    dataSize = nodeHeader >> 1;
                    nextNode = -1;
                }
                else {
                    dataSize = NodeDataSize;
                    nextNode = node + 1 + (nodeHeader >> 1);
                }
            }

            public static uint ReverseEndianness(uint n) => ((n & 0xFF000000u) >> 24)
                | ((n & 0xFF0000) >> FileHeaderFreeNodeOffset)
                | ((n & 0xFF00) << FileHeaderFreeNodeOffset)
                | (n << 24);

            public virtual void LogDebugInfo() {
                Log.Information("{0} chunks:", ChunkDescriptors.Count);
                foreach (KeyValuePair<Point2, ChunkDescriptor> chunkDescriptor in ChunkDescriptors) {
                    Log.Information(
                        "    Chunk {0}: p=({1}), startNode={2}",
                        chunkDescriptor.Value.Index,
                        chunkDescriptor.Key,
                        chunkDescriptor.Value.StartNode
                    );
                }
                int num = (int)((Stream.Length - FileHeaderSize) / NodeSize);
                Log.Information("{0} nodes, FreeNode={1:0}:", num, FreeNode);
                for (int i = 0; i < num; i++) {
                    int num2 = ReadNode(i, null, 0, out int nextNode);
                    Log.Information("    Node {0:0}: next={1:0}, dataSize={2}", i, nextNode, num2);
                }
            }
        }

        public class RegionFileStorage : IStorage {
            public struct DirectoryEntry {
                public int Offset;

                public int Size;
            }

            const int MaxOpenedStreams = 100;

            const int ExtraSpaceBytes = 1024;

            // ReSharper disable UnusedMember.Local
            const int RegionChunksBits = 4;

            const int RegionChunksCount = 16;

            const int RegionDirectoryOffset = 4;

            const int RegionDirectoryEntrySize = 8;

            const int RegionChunksCountMinusOne = 15;

            const int RegionDataOffset = 2052;

            const int RegionChunkDataOffset = 4;
            // ReSharper restore UnusedMember.Local

            static uint RegionMagic = MakeFourCC("RGN1");

            static uint RegionChunkMagic = MakeFourCC("CHK1");

            public string RegionsDirectoryName;

            string TmpFilePath;

            Dictionary<Point2, Stream> StreamsByRegion = [];

            Queue<Stream> OpenedStreams = new();

            public virtual void Dispose() {
                while (OpenedStreams.Count > 0) {
                    OpenedStreams.Dequeue().Dispose();
                }
            }

            public virtual void Open(string directoryName, string suffix) {
                RegionsDirectoryName = Storage.CombinePaths(directoryName, $"Regions{suffix}");
                Storage.CreateDirectory(RegionsDirectoryName);
                TmpFilePath = Storage.CombinePaths(RegionsDirectoryName, "tmp");
                Storage.DeleteFile(TmpFilePath);
                foreach (string item in Storage.ListFileNames(RegionsDirectoryName)) {
                    if (Storage.GetExtension(item) == ".new") {
                        string text = Storage.CombinePaths(RegionsDirectoryName, item);
                        string text2 = Storage.ChangeExtension(text, "");
                        if (!Storage.FileExists(text2)) {
                            Storage.MoveFile(text, text2);
                        }
                        else {
                            Storage.DeleteFile(text);
                        }
                    }
                }
            }

            public virtual int Load(Point2 coords, byte[] buffer) {
                Point2 region = new(coords.X >> 4, coords.Y >> 4);
                Point2 chunk = new(coords.X & 0xF, coords.Y & 0xF);
                Stream regionStream = GetRegionStream(region, false);
                if (regionStream != null) {
                    using (BinaryReader reader = new(regionStream, Encoding.UTF8, true)) {
                        DirectoryEntry directoryEntry = ReadDirectoryEntry(reader, chunk);
                        if (directoryEntry.Offset > 0) {
                            ReadData(reader, directoryEntry.Offset, buffer, directoryEntry.Size);
                            return directoryEntry.Size;
                        }
                    }
                }
                return -1;
            }

            public virtual void Save(Point2 coords, byte[] buffer, int size) {
                Point2 region = new(coords.X >> 4, coords.Y >> 4);
                Point2 point = new(coords.X & 0xF, coords.Y & 0xF);
                Stream regionStream = GetRegionStream(region, true);
                string text = null;
                using (BinaryReader reader = new(regionStream, Encoding.UTF8, true)) {
                    using (BinaryWriter writer = new(regionStream, Encoding.UTF8, true)) {
                        int num = point.X + ChunkSizeX * point.Y;
                        DirectoryEntry[] array = ReadDirectoryEntries(reader);
                        DirectoryEntry directoryEntry = array[num];
                        DirectoryEntry entry;
                        if (directoryEntry.Offset > 0) {
                            int num2 = FindNextEntryIndex(array, num);
                            if (num2 >= 0) {
                                int num3 = array[num2].Offset - directoryEntry.Offset - 4;
                                if (size <= num3) {
                                    WriteData(writer, directoryEntry.Offset, buffer, size);
                                    Point2 chunk = point;
                                    entry = new DirectoryEntry { Offset = directoryEntry.Offset, Size = size };
                                    WriteDirectoryEntry(writer, chunk, entry);
                                    regionStream.Flush();
                                }
                                else {
                                    text = GetRegionPath(region);
                                    using (Stream stream = Storage.OpenFile(TmpFilePath, OpenFileMode.Create)) {
                                        using (BinaryWriter binaryWriter = new(stream)) {
                                            DirectoryEntry[] array2 = new DirectoryEntry[array.Length];
                                            int num4 = RegionDataOffset;
                                            for (int i = 0; i < array.Length; i++) {
                                                if (i == num) {
                                                    array2[i].Offset = num4;
                                                    array2[i].Size = size;
                                                    num4 += CalculateIdealEntrySpace(array2[i].Size);
                                                }
                                                else if (array[i].Offset > 0) {
                                                    array2[i].Offset = num4;
                                                    array2[i].Size = array[i].Size;
                                                    num4 += CalculateIdealEntrySpace(array2[i].Size);
                                                }
                                            }
                                            ResizeStream(stream, num4);
                                            binaryWriter.Write(RegionMagic);
                                            WriteDirectoryEntries(binaryWriter, array2);
                                            byte[] buffer2 = new byte[array.Max(e => e.Size)];
                                            for (int j = 0; j < array.Length; j++) {
                                                if (j == num) {
                                                    WriteData(binaryWriter, array2[j].Offset, buffer, size);
                                                }
                                                else if (array[j].Offset > 0) {
                                                    ReadData(reader, array[j].Offset, buffer2, array[j].Size);
                                                    WriteData(binaryWriter, array2[j].Offset, buffer2, array2[j].Size);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else {
                                if (directoryEntry.Offset + 4 + size > regionStream.Length) {
                                    ResizeStream(regionStream, directoryEntry.Offset + CalculateIdealEntrySpace(size));
                                }
                                WriteData(writer, directoryEntry.Offset, buffer, size);
                                Point2 chunk2 = point;
                                entry = new DirectoryEntry { Offset = directoryEntry.Offset, Size = size };
                                WriteDirectoryEntry(writer, chunk2, entry);
                                regionStream.Flush();
                            }
                        }
                        else {
                            int num5 = (int)regionStream.Length;
                            ResizeStream(regionStream, num5 + CalculateIdealEntrySpace(size));
                            WriteData(writer, num5, buffer, size);
                            Point2 chunk3 = point;
                            entry = new DirectoryEntry { Offset = num5, Size = size };
                            WriteDirectoryEntry(writer, chunk3, entry);
                            regionStream.Flush();
                        }
                    }
                }
                if (text != null) {
                    regionStream.Dispose();
                    string text2 = $"{text}.new";
                    Storage.MoveFile(TmpFilePath, text2);
                    Storage.MoveFile(text2, text);
                }
            }

            public virtual string GetRegionPath(Point2 region) => $"{RegionsDirectoryName}/Region {region.X},{region.Y}.dat";

            public virtual Stream GetRegionStream(Point2 region, bool createNew) {
                if (!StreamsByRegion.TryGetValue(region, out Stream value)
                    || value == null
                    || !value.CanRead) {
                    string regionPath = GetRegionPath(region);
                    if (Storage.FileExists(regionPath)) {
                        value = Storage.OpenFile(regionPath, OpenFileMode.ReadWrite);
                        using (BinaryReader binaryReader = new(value, Encoding.UTF8, true)) {
                            if (binaryReader.ReadUInt32() != RegionMagic) {
                                throw new InvalidOperationException($"Invalid region file {region} magic.");
                            }
                        }
                        OpenedStreams.Enqueue(value);
                    }
                    else if (createNew) {
                        value = Storage.OpenFile(regionPath, OpenFileMode.Create);
                        OpenedStreams.Enqueue(value);
                        using (BinaryWriter binaryWriter = new(value, Encoding.UTF8, true)) {
                            binaryWriter.Write(RegionMagic);
                            WriteDirectoryEntries(binaryWriter, new DirectoryEntry[256]);
                        }
                    }
                    else {
                        value = null;
                    }
                    StreamsByRegion[region] = value;
                    while (OpenedStreams.Count > MaxOpenedStreams) {
                        OpenedStreams.Dequeue().Dispose();
                    }
                }
                return value;
            }

            public static void ReadData(BinaryReader reader, int offset, byte[] buffer, int size) {
                reader.BaseStream.Position = offset;
                if (reader.ReadUInt32() != RegionChunkMagic) {
                    throw new InvalidOperationException("Invalid region file chunk magic.");
                }
                if (reader.BaseStream.Read(buffer, 0, size) != size) {
                    throw new InvalidOperationException("Region file is truncated.");
                }
            }

            public static DirectoryEntry ReadDirectoryEntry(BinaryReader reader) {
                DirectoryEntry result = default;
                result.Offset = reader.ReadInt32();
                result.Size = reader.ReadInt32();
                if (result.Size < 0
                    || result.Size > 1048576) {
                    throw new InvalidOperationException("Region file entry size out of bounds, likely corrupt region file.");
                }
                return result;
            }

            public static DirectoryEntry ReadDirectoryEntry(BinaryReader reader, Point2 chunk) {
                int num = chunk.X + ChunkSizeX * chunk.Y;
                reader.BaseStream.Position = 4 + num * RegionDirectoryEntrySize;
                return ReadDirectoryEntry(reader);
            }

            public static DirectoryEntry[] ReadDirectoryEntries(BinaryReader reader) {
                reader.BaseStream.Position = 4L;
                DirectoryEntry[] array = new DirectoryEntry[256];
                for (int i = 0; i < 256; i++) {
                    array[i] = ReadDirectoryEntry(reader);
                }
                return array;
            }

            public static void WriteData(BinaryWriter writer, int offset, byte[] buffer, int size) {
                writer.BaseStream.Position = offset;
                writer.Write(RegionChunkMagic);
                writer.BaseStream.Write(buffer, 0, size);
            }

            public static void WriteDirectoryEntry(BinaryWriter writer, DirectoryEntry entry) {
                writer.Write(entry.Offset);
                writer.Write(entry.Size);
            }

            public static void WriteDirectoryEntry(BinaryWriter writer, Point2 chunk, DirectoryEntry entry) {
                int num = chunk.X + 16 * chunk.Y;
                writer.BaseStream.Position = 4 + num * RegionDirectoryEntrySize;
                WriteDirectoryEntry(writer, entry);
            }

            public static void WriteDirectoryEntries(BinaryWriter writer, DirectoryEntry[] entries) {
                writer.BaseStream.Position = 4L;
                for (int i = 0; i < 256; i++) {
                    WriteDirectoryEntry(writer, entries[i]);
                }
            }

            public static void ResizeStream(Stream stream, int size) {
                if (size > 268435456) {
                    throw new InvalidOperationException("Region file too large.");
                }
                stream.SetLength(size);
            }

            public static int FindNextEntryIndex(DirectoryEntry[] entries, int index) {
                int result = -1;
                int num = int.MaxValue;
                for (int i = 0; i < entries.Length; i++) {
                    int num2 = entries[i].Offset - entries[index].Offset;
                    if (num2 > 0
                        && num2 < num) {
                        num = num2;
                        result = i;
                    }
                }
                return result;
            }

            public static int CalculateIdealEntrySpace(int size) => size + ExtraSpaceBytes + 4;

            public static uint MakeFourCC(string s) => ((uint)s[3] << 24) | ((uint)s[2] << 16) | ((uint)s[1] << 8) | s[0];
        }

        const int ChunkSizeX = 16;

        const int ChunkSizeY = 256;

        const int ChunkSizeZ = 16;

        const int WorstCaseChunkDataSize = 262400;

        object m_lock = new();

        IStorage m_storage;

        byte[] m_storageBuffer = new byte[WorstCaseChunkDataSize];

        byte[] m_compressBuffer = new byte[WorstCaseChunkDataSize];

        bool m_ioExceptionDealt;

        public TerrainSerializer23() {}

        public TerrainSerializer23(string directoryName, string suffix = "") {
            m_storage = new RegionFileStorage();
            m_storage.Open(directoryName, suffix);
        }

        public virtual bool LoadChunk(TerrainChunk chunk) => LoadChunkData(chunk);

        public virtual void SaveChunk(TerrainChunk chunk) {
            if (chunk.State > TerrainChunkState.InvalidContents4
                && chunk.ModificationCounter > 0) {
                SaveChunkData(chunk);
                chunk.ModificationCounter = 0;
            }
        }

        public virtual bool LoadChunkData(TerrainChunk chunk) {
            lock (m_lock) {
                _ = Time.RealTime;
                try {
                    int num = m_storage.Load(chunk.Coords, m_storageBuffer);
                    if (num < 0) {
                        return false;
                    }
                    DecompressChunkData(chunk, m_storageBuffer, num);
                }
                catch (IOException) {
                    Dispatcher.Dispatch(() => {
                            if (m_ioExceptionDealt) {
                                return;
                            }
                            m_ioExceptionDealt = true;
                            GameManager.DisposeProject();
                            ScreensManager.SwitchScreen("Play");
                            ViewGameLogDialog dialog = new();
                            dialog.SetErrorHead(11, 14);
                            DialogsManager.ShowDialog(null, dialog);
                        }
                    );
                    return false;
                }
                catch (Exception e) {
                    Log.Error(ExceptionManager.MakeFullErrorMessage($"Error loading chunk ({chunk.Coords.X},{chunk.Coords.Y}).", e));
                }
                _ = Time.RealTime;
                return true;
            }
        }

        public virtual void SaveChunkData(TerrainChunk chunk) {
            lock (m_lock) {
                _ = Time.RealTime;
                try {
                    int size = CompressChunkData(chunk, m_storageBuffer);
                    m_storage.Save(chunk.Coords, m_storageBuffer, size);
                }
                catch (Exception e) {
                    Log.Error(ExceptionManager.MakeFullErrorMessage($"Error saving chunk ({chunk.Coords.X},{chunk.Coords.Y}).", e));
                }
                _ = Time.RealTime;
            }
        }

        public virtual void Dispose() {
            Utilities.Dispose(ref m_storage);
        }

        public virtual int CompressChunkData(TerrainChunk chunk, byte[] buffer) {
            int num = 0;
            for (int i = 0; i < ChunkSizeX; i++) {
                for (int j = 0; j < ChunkSizeZ; j++) {
                    int shaftValueFast = chunk.GetShaftValueFast(i, j);
                    m_compressBuffer[num++] = (byte)((Terrain.ExtractTemperature(shaftValueFast) << 4) | Terrain.ExtractHumidity(shaftValueFast));
                }
            }
            int num2 = 0;
            int num3 = -1;
            for (int k = 0; k < ChunkSizeY; k++) {
                for (int l = 0; l < ChunkSizeX; l++) {
                    for (int m = 0; m < ChunkSizeZ; m++) {
                        int num4 = Terrain.ReplaceLight(chunk.GetCellValueFast(m, k, l), 0);
                        if (num2 == 0) {
                            num3 = num4;
                            num2 = 1;
                            continue;
                        }
                        if (num4 != num3) {
                            num = WriteRleValueToBuffer(m_compressBuffer, num, num3, num2);
                            num3 = num4;
                            num2 = 1;
                            continue;
                        }
                        num2++;
                        if (num2 == 271) {
                            num = WriteRleValueToBuffer(m_compressBuffer, num, num3, num2);
                            num2 = 0;
                        }
                    }
                }
            }
            if (num2 > 0) {
                num = WriteRleValueToBuffer(m_compressBuffer, num, num3, num2);
            }
            return Deflate(m_compressBuffer, 0, num, buffer);
        }

        public virtual void DecompressChunkData(TerrainChunk chunk, byte[] buffer, int size) {
            size = UnDeflate(buffer, 0, size, m_compressBuffer);
            int num = 0;
            for (int i = 0; i < ChunkSizeX; i++) {
                for (int j = 0; j < ChunkSizeZ; j++) {
                    byte b = m_compressBuffer[num++];
                    int value = Terrain.ReplaceTemperature(Terrain.ReplaceHumidity(0, b & 0xF), b >> 4);
                    chunk.SetShaftValueFast(i, j, value);
                }
            }
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            while (num < size) {
                num = ReadRleValueFromBuffer(m_compressBuffer, num, out int value2, out int count);
                for (int k = 0; k < count; k++) {
                    chunk.SetCellValueFast(num2, num3, num4, value2);
                    num2++;
                    if (num2 >= ChunkSizeX) {
                        num2 = 0;
                        num4++;
                        if (num4 >= ChunkSizeZ) {
                            num4 = 0;
                            num3++;
                        }
                    }
                }
            }
            if (num2 != 0
                || num3 != ChunkSizeY
                || num4 != 0) {
                throw new InvalidOperationException("Corrupt chunk data.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadIntFromBuffer(byte[] buffer, int i) => buffer[i] + (buffer[i + 1] << 8) + (buffer[i + 2] << 16) + (buffer[i + 3] << 24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadRleValueFromBuffer(byte[] buffer, int i, out int value, out int count) {
            int value2 = ReadIntFromBuffer(buffer, i);
            int num = Terrain.ExtractLight(value2);
            value = Terrain.ReplaceLight(value2, 0);
            if (num < 15) {
                count = num + 1;
                return i + 4;
            }
            count = buffer[i + 4] + 16;
            return i + 5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteIntToBuffer(byte[] buffer, int i, int data) {
            buffer[i] = (byte)data;
            buffer[i + 1] = (byte)(data >> 8);
            buffer[i + 2] = (byte)(data >> 16);
            buffer[i + 3] = (byte)(data >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteRleValueToBuffer(byte[] buffer, int i, int value, int count) {
            if (count < 16) {
                int data = Terrain.ReplaceLight(value, count - 1);
                WriteIntToBuffer(buffer, i, data);
                return i + 4;
            }
            if (count <= 271) {
                int data2 = Terrain.ReplaceLight(value, 15);
                WriteIntToBuffer(buffer, i, data2);
                buffer[i + 4] = (byte)(count - 16);
                return i + 5;
            }
            throw new InvalidOperationException("Count too large.");
        }

        public static int Deflate(byte[] input, int offset, int length, byte[] output) {
            MemoryStream memoryStream = new(input, offset, length);
            MemoryStream memoryStream2 = new(output);
            using (DeflateStream destination = new(memoryStream2, CompressionLevel.Fastest, true)) {
                memoryStream.CopyTo(destination);
            }
            return (int)memoryStream2.Position;
        }

        public static int UnDeflate(byte[] input, int offset, int length, byte[] output) {
            MemoryStream stream = new(input, offset, length);
            MemoryStream memoryStream = new(output);
            using (DeflateStream deflateStream = new(stream, CompressionMode.Decompress)) {
                deflateStream.CopyTo(memoryStream);
            }
            return (int)memoryStream.Position;
        }
    }
}