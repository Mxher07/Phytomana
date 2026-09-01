using System.Xml.Linq;
using Engine;
using Engine.Serialization;
using XmlUtilities;

namespace Game {
    public class VersionConverter21To22 : VersionConverter {
        public override string SourceVersion => "2.1";

        public override string TargetVersion => "2.2";

        public override void ConvertProjectXml(XElement projectNode) {
            XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
            _ = string.Empty;
            foreach (XElement item in from e in projectNode.Element("Subsystems").Elements()
                where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "GameInfo"
                select e) {
                foreach (XElement item2 in from e in item.Elements("Value")
                    where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "TerrainGenerationMode"
                    select e) {
                    if (XmlUtils.GetAttributeValue(item2, "Value", "") == "Flat") {
                        XmlUtils.SetAttributeValue(item2, "Value", "FlatContinent");
                    }
                }
            }
            foreach (XElement item3 in from e in projectNode.Element("Subsystems").Elements()
                where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Pickables"
                select e) {
                foreach (XElement item4 in from e in item3.Elements("Values")
                    where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Pickables"
                    select e) {
                    foreach (XElement item5 in item4.Elements("Values")) {
                        foreach (XElement item6 in from e in item5.Elements("Value")
                            where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Value"
                            select e) {
                            int num = ConvertValue(XmlUtils.GetAttributeValue<int>(item6, "Value"));
                            XmlUtils.SetAttributeValue(item6, "Value", num);
                        }
                    }
                }
            }
            foreach (XElement item7 in from e in projectNode.Element("Subsystems").Elements()
                where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Projectiles"
                select e) {
                foreach (XElement item8 in from e in item7.Elements("Values")
                    where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Projectiles"
                    select e) {
                    foreach (XElement item9 in item8.Elements("Values")) {
                        foreach (XElement item10 in from e in item9.Elements("Value")
                            where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Value"
                            select e) {
                            int num2 = ConvertValue(XmlUtils.GetAttributeValue<int>(item10, "Value"));
                            XmlUtils.SetAttributeValue(item10, "Value", num2);
                        }
                    }
                }
            }
            foreach (XElement item11 in from e in projectNode.Element("Subsystems").Elements()
                where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "CollapsingBlockBehavior"
                select e) {
                foreach (XElement item12 in from e in item11.Elements("Values")
                    where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "CollapsingBlocks"
                    select e) {
                    foreach (XElement item13 in item12.Elements("Values")) {
                        foreach (XElement item14 in from e in item13.Elements("Value")
                            where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Value"
                            select e) {
                            int num3 = ConvertValue(XmlUtils.GetAttributeValue<int>(item14, "Value"));
                            XmlUtils.SetAttributeValue(item14, "Value", num3);
                        }
                    }
                }
            }
            foreach (XElement item15 in projectNode.Element("Entities").Elements()) {
                foreach (XElement item16 in from e in item15.Elements("Values")
                    where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Clothing"
                    select e) {
                    foreach (XElement item17 in from e in item16.Elements("Values")
                        where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Clothes"
                        select e) {
                        foreach (XElement item18 in item17.Elements()) {
                            string attributeValue = XmlUtils.GetAttributeValue<string>(item18, "Value");
                            int[] array = HumanReadableConverter.ValuesListFromString<int>(';', attributeValue);
                            for (int i = 0; i < array.Length; i++) {
                                array[i] = ConvertValue(array[i]);
                            }
                            string value = HumanReadableConverter.ValuesListToString(';', array);
                            XmlUtils.SetAttributeValue(item18, "Value", value);
                        }
                    }
                }
            }
            string[] inventoryNames = ["Inventory", "CreativeInventory", "CraftingTable", "Chest", "Furnace", "Dispenser"];
            foreach (XElement item19 in projectNode.Element("Entities").Elements()) {
                foreach (XElement item20 in from e in item19.Elements("Values")
                    where inventoryNames.Contains(XmlUtils.GetAttributeValue(e, "Name", string.Empty))
                    select e) {
                    foreach (XElement item21 in from e in item20.Elements("Values")
                        where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Slots"
                        select e) {
                        foreach (XElement item22 in item21.Elements()) {
                            foreach (XElement item23 in from e in item22.Elements("Value")
                                where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Contents"
                                select e) {
                                int num4 = ConvertValue(XmlUtils.GetAttributeValue<int>(item23, "Value"));
                                XmlUtils.SetAttributeValue(item23, "Value", num4);
                            }
                        }
                    }
                }
            }
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
                foreach (string item2 in from f in Storage.ListFileNames(directoryName) where Storage.GetExtension(f) == ".old" select f) {
                    Storage.DeleteFile(Storage.CombinePaths(directoryName, item2));
                }
            }
            catch (Exception) {
                foreach (string item3 in from f in Storage.ListFileNames(directoryName) where Storage.GetExtension(f) == ".old" select f) {
                    string sourcePath2 = Storage.CombinePaths(directoryName, item3);
                    string destinationPath2 = Storage.CombinePaths(directoryName, Storage.GetFileNameWithoutExtension(item3));
                    Storage.MoveFile(sourcePath2, destinationPath2);
                }
                foreach (string item4 in from f in Storage.ListFileNames(directoryName) where Storage.GetExtension(f) == ".new" select f) {
                    Storage.DeleteFile(Storage.CombinePaths(directoryName, item4));
                }
                throw;
            }
        }

        public void ConvertProject(string directoryName) {
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

        public void ConvertChunks(string directoryName) {
            string path = Storage.CombinePaths(directoryName, "Chunks32.dat");
            string path2 = Storage.CombinePaths(directoryName, "Chunks32h.dat.new");
            long num = 2 * Storage.GetFileSize(path) + 52428800;
            if (Storage.FreeSpace < num) {
                throw new InvalidOperationException($"Not enough free space to convert world. {num / 1024 / 1024}MB required.");
            }
            using (Stream stream2 = Storage.OpenFile(path, OpenFileMode.Read)) {
                using (Stream stream = Storage.OpenFile(path2, OpenFileMode.Create)) {
                    byte[] array = new byte[131072];
                    byte[] array2 = new byte[262144];
                    for (int i = 0; i < 65537; i++) {
                        TerrainSerializer22.WriteTOCEntry(stream, 0, 0, -1);
                    }
                    int num2 = 0;
                    while (true) {
                        stream2.Position = 12 * num2;
                        TerrainSerializer129.ReadTOCEntry(stream2, out int cx, out int cz, out int index);
                        if (index < 0) {
                            break;
                        }
                        stream.Position = 12 * num2;
                        TerrainSerializer22.WriteTOCEntry(stream, cx, cz, num2);
                        stream2.Position = 786444 + 132112L * index;
                        stream.Position = stream.Length;
                        TerrainSerializer129.ReadChunkHeader(stream2);
                        TerrainSerializer22.WriteChunkHeader(stream, cx, cz);
                        stream2.ReadExactly(array, 0, 131072);
                        int num3 = 0;
                        int num4 = 0;
                        for (int j = 0; j < 16; j++) {
                            for (int k = 0; k < 16; k++) {
                                for (int l = 0; l < 256; l++) {
                                    int num5;
                                    if (l <= 127) {
                                        num5 = ConvertValue(
                                            array[4 * num3] | (array[4 * num3 + 1] << 8) | (array[4 * num3 + 2] << 16) | (array[4 * num3 + 3] << 24)
                                        );
                                        num3++;
                                    }
                                    else {
                                        num5 = 0;
                                    }
                                    array2[4 * num4] = (byte)num5;
                                    array2[4 * num4 + 1] = (byte)(num5 >> 8);
                                    array2[4 * num4 + 2] = (byte)(num5 >> 16);
                                    array2[4 * num4 + 3] = (byte)(num5 >> 24);
                                    num4++;
                                }
                            }
                        }
                        stream.Write(array2, 0, 262144);
                        stream2.ReadExactly(array, 0, 1024);
                        stream.Write(array, 0, 1024);
                        num2++;
                    }
                }
            }
            Storage.MoveFile(Storage.CombinePaths(directoryName, "Chunks32.dat"), Storage.CombinePaths(directoryName, "Chunks32.dat.old"));
        }

        public static int ConvertValue(int value) {
            int contents = value & 0x3FF;
            int light = (value >> 10) & 0xF;
            int data = value >> 14;
            ConvertContentsLightData(ref contents, ref light, ref data);
            return contents | (light << 10) | (data << 14);
        }

        public static void ConvertContentsLightData(ref int contents, ref int light, ref int data) {
            if (contents == 30) {
                contents = 29;
            }
            if (contents == 34) {
                contents = 29;
            }
            if (contents == 32) {
                contents = 29;
            }
            if (contents == 35) {
                contents = 29;
            }
            if (contents == 33) {
                contents = 29;
            }
            if (contents == 170) {
                contents = 169;
            }
            if (contents == 122) {
                contents = 122;
            }
            if (contents == 123) {
                contents = 123;
            }
        }
    }
}