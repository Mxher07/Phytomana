using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game {
    public class VersionConverter22To23 : VersionConverter {
        public override string SourceVersion => "2.2";

        public override string TargetVersion => "2.3";

        public override void ConvertProjectXml(XElement projectNode) {
            XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
        }

        public override void ConvertWorld(string directoryName) {
            try {
                ConvertChunks(directoryName);
                ConvertProject(directoryName);
                foreach (string item in from f in Storage.ListFileNames(directoryName) where Storage.GetExtension(f) == ".new" select f) {
                    string sourcePath = Storage.CombinePaths(directoryName, item);
                    string destinationPath = Storage.CombinePaths(directoryName, Storage.GetFileNameWithoutExtension(item));
                    Storage.MoveFile(sourcePath, destinationPath);
                }
                foreach (string item2 in from f in Storage.ListDirectoryNames(directoryName) where Storage.GetExtension(f) == ".new" select f) {
                    string sourcePath2 = Storage.CombinePaths(directoryName, item2);
                    string destinationPath2 = Storage.CombinePaths(directoryName, Storage.GetFileNameWithoutExtension(item2));
                    Storage.MoveDirectory(sourcePath2, destinationPath2);
                }
                foreach (string item3 in from f in Storage.ListFileNames(directoryName) where Storage.GetExtension(f) == ".old" select f) {
                    Storage.DeleteFile(Storage.CombinePaths(directoryName, item3));
                }
                foreach (string item4 in from f in Storage.ListDirectoryNames(directoryName) where Storage.GetExtension(f) == ".old" select f) {
                    Storage.DeleteDirectoryRecursive(Storage.CombinePaths(directoryName, item4));
                }
            }
            catch (Exception) {
                foreach (string item5 in from f in Storage.ListFileNames(directoryName) where Storage.GetExtension(f) == ".old" select f) {
                    string sourcePath3 = Storage.CombinePaths(directoryName, item5);
                    string destinationPath3 = Storage.CombinePaths(directoryName, Storage.GetFileNameWithoutExtension(item5));
                    Storage.MoveFile(sourcePath3, destinationPath3);
                }
                foreach (string item6 in from f in Storage.ListDirectoryNames(directoryName) where Storage.GetExtension(f) == ".old" select f) {
                    string sourcePath4 = Storage.CombinePaths(directoryName, item6);
                    string destinationPath4 = Storage.CombinePaths(directoryName, Storage.GetFileNameWithoutExtension(item6));
                    Storage.MoveDirectory(sourcePath4, destinationPath4);
                }
                foreach (string item7 in from f in Storage.ListFileNames(directoryName) where Storage.GetExtension(f) == ".new" select f) {
                    Storage.DeleteFile(Storage.CombinePaths(directoryName, item7));
                }
                foreach (string item8 in from f in Storage.ListDirectoryNames(directoryName) where Storage.GetExtension(f) == ".new" select f) {
                    Storage.DeleteDirectoryRecursive(Storage.CombinePaths(directoryName, item8));
                }
                throw;
            }
        }

        void ConvertProject(string directoryName) {
            string path = Storage.CombinePaths(directoryName, "Project.xml");
            string path2 = Storage.CombinePaths(directoryName, "Project.xml.new");
            XElement xElement;
            using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read)) {
                xElement = XmlUtils.LoadXmlFromStream(stream, null, true);
            }
            ConvertProjectXml(xElement);
            using (Stream stream2 = Storage.OpenFile(path2, OpenFileMode.Create)) {
                XmlUtils.SaveXmlToStream(xElement, stream2, null, true);
            }
        }

        void ConvertChunks(string directoryName) {
            long num = Storage.GetFileSize(Storage.CombinePaths(directoryName, "Chunks32h.dat")) / 10 + 52428800;
            if (Storage.FreeSpace < num) {
                throw new InvalidOperationException($"Not enough free space to convert world. {num / 1024 / 1024}MB required.");
            }
            using (TerrainSerializer22 terrainSerializer = new(null, directoryName)) {
                using (TerrainSerializer23 terrainSerializer2 = new(directoryName, ".new")) {
                    foreach (Point2 chunk2 in terrainSerializer.Chunks) {
                        TerrainChunk chunk = new(null, chunk2.X, chunk2.Y);
                        terrainSerializer.LoadChunk(chunk);
                        terrainSerializer2.SaveChunkData(chunk);
                    }
                }
            }
            Storage.MoveFile(Storage.CombinePaths(directoryName, "Chunks32h.dat"), Storage.CombinePaths(directoryName, "Chunks32h.dat.old"));
        }
    }
}