using System.Globalization;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game {
    public class VersionConverter10To14 : VersionConverter {
        public override string SourceVersion => "1.0";

        public override string TargetVersion => "1.4";

        public override void ConvertProjectXml(XElement projectNode) {
            XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
        }

        public override void ConvertWorld(string directoryName) {
            string[] array = Storage.ListFileNames(Storage.CombinePaths(directoryName, "Chunks")).ToArray();
            string[] array2;
            using (Stream stream = Storage.OpenFile(Storage.CombinePaths(directoryName, "Chunks.dat"), OpenFileMode.Create)) {
                for (int i = 0; i < 65537; i++) {
                    TerrainSerializer14.WriteTOCEntry(stream, 0, 0, 0);
                }
                int num = 0;
                array2 = array;
                foreach (string text in array2) {
                    try {
                        if (num >= 65536) {
                            throw new InvalidOperationException("Too many chunks.");
                        }
                        string[] array3 = Storage.GetFileNameWithoutExtension(text).Split('_');
                        int cx = int.Parse(array3[1], CultureInfo.InvariantCulture);
                        int cz = int.Parse(array3[2], CultureInfo.InvariantCulture);
                        using (Stream stream2 = Storage.OpenFile(
                                Storage.CombinePaths(directoryName, Storage.CombinePaths("Chunks", text)),
                                OpenFileMode.Read
                            )) {
                            byte[] array4 = new byte[stream2.Length];
                            stream2.ReadExactly(array4);
                            int num2 = (int)stream.Length;
                            stream.Position = num2;
                            TerrainSerializer14.WriteChunkHeader(stream, cx, cz);
                            stream.Write(array4, 0, array4.Length);
                            stream.Position = num * 4 * 3;
                            TerrainSerializer14.WriteTOCEntry(stream, cx, cz, num2);
                            num++;
                        }
                    }
                    catch (Exception ex) {
                        Log.Error($"Error converting chunk file \"{text}\". Skipping chunk. Reason: {ex.Message}");
                    }
                }
                stream.Flush();
                Log.Information($"Converted {num} chunk(s).");
            }
            string path = Storage.CombinePaths(directoryName, "Project.xml");
            XElement xElement;
            using (Stream stream3 = Storage.OpenFile(path, OpenFileMode.Read)) {
                xElement = XmlUtils.LoadXmlFromStream(stream3, null, true);
            }
            ConvertProjectXml(xElement);
            using (Stream stream4 = Storage.OpenFile(path, OpenFileMode.Create)) {
                XmlUtils.SaveXmlToStream(xElement, stream4, null, true);
            }
            array2 = array;
            foreach (string text2 in array2) {
                Storage.DeleteFile(Storage.CombinePaths(directoryName, Storage.CombinePaths("Chunks", text2)));
            }
            Storage.DeleteDirectory(Storage.CombinePaths(directoryName, "Chunks"));
        }
    }
}