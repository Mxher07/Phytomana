using System.Xml.Linq;
using Engine;
using TemplatesDatabase;

namespace Game {
    public static class FurniturePacksManager {
        public static List<string> m_furniturePackNames;

        public static ReadOnlyList<string> FurniturePackNames => new(m_furniturePackNames);

        public static string FurniturePacksDirectoryName => ModsManager.FurniturePacksDirectoryName;

        public static event Action<string> FurniturePackDeleted;

        static FurniturePacksManager() => m_furniturePackNames = [];

        public static void Initialize() {
            Storage.CreateDirectory(FurniturePacksDirectoryName);
        }

        public static string GetFileName(string name) => Storage.CombinePaths(FurniturePacksDirectoryName, name);

        public static string GetDisplayName(string name) => Storage.GetFileNameWithoutExtension(name);

        public static DateTime GetCreationDate(string name) {
            try {
                return Storage.GetFileLastWriteTime(GetFileName(name));
            }
            catch {
                return new DateTime(2000, 1, 1);
            }
        }

        public static string ImportFurniturePack(string name, Stream stream) {
            if (MarketplaceManager.IsTrialMode) {
                throw new InvalidOperationException("Cannot import furniture packs in trial mode.");
            }
            ValidateFurniturePack(stream);
            stream.Position = 0L;
            string fileNameWithoutExtension = Storage.GetFileNameWithoutExtension(name);
            name = $"{fileNameWithoutExtension}.scfpack";
            string fileName = GetFileName(name);
            int num = 0;
            while (Storage.FileExists(fileName)) {
                num++;
                if (num > 9) {
                    throw new InvalidOperationException("Duplicate name. Delete existing content with conflicting names.");
                }
                name = $"{fileNameWithoutExtension} ({num}).scfpack";
                fileName = GetFileName(name);
            }
            using (Stream destination = Storage.OpenFile(fileName, OpenFileMode.Create)) {
                stream.CopyTo(destination);
                return name;
            }
        }

        public static void ExportFurniturePack(string name, Stream stream) {
            using (Stream stream2 = Storage.OpenFile(GetFileName(name), OpenFileMode.Read)) {
                stream2.CopyTo(stream);
            }
        }

        public static string CreateFurniturePack(string name, ICollection<FurnitureDesign> designs) {
            MemoryStream memoryStream = new();
            using (ZipArchive zipArchive = ZipArchive.Create(memoryStream, true)) {
                ValuesDictionary valuesDictionary = new();
                SubsystemFurnitureBlockBehavior.SaveFurnitureDesigns(valuesDictionary, designs);
                XElement xElement = new("FurnitureDesigns");
                valuesDictionary.Save(xElement);
                MemoryStream memoryStream2 = new();
                xElement.Save(memoryStream2);
                memoryStream2.Position = 0L;
                zipArchive.AddStream("FurnitureDesigns.xml", memoryStream2);
            }
            memoryStream.Position = 0L;
            return ImportFurniturePack(name, memoryStream);
        }

        public static void DeleteFurniturePack(string name) {
            try {
                Storage.DeleteFile(GetFileName(name));
                FurniturePackDeleted?.Invoke(name);
            }
            catch (Exception e) {
                ExceptionManager.ReportExceptionToUser($"Unable to delete furniture pack \"{name}\"", e);
            }
        }

        public static void UpdateFurniturePacksList() {
            m_furniturePackNames.Clear();
            foreach (string item in Storage.ListFileNames(FurniturePacksDirectoryName)) {
                if (Storage.GetExtension(item).ToLower() == ".scfpack") {
                    m_furniturePackNames.Add(item);
                }
            }
        }

        public static List<FurnitureDesign> LoadFurniturePack(SubsystemTerrain subsystemTerrain, string name) {
            using (Stream stream = Storage.OpenFile(GetFileName(name), OpenFileMode.Read)) {
                return LoadFurniturePack(subsystemTerrain, stream);
            }
        }

        public static void ValidateFurniturePack(Stream stream) {
            LoadFurniturePack(null, stream);
        }

        public static List<FurnitureDesign> LoadFurniturePack(SubsystemTerrain subsystemTerrain, Stream stream) {
            using (ZipArchive zipArchive = ZipArchive.Open(stream, true)) {
                List<ZipArchiveEntry> list = zipArchive.ReadCentralDir();
                if (list.Count != 1
                    || list[0].FilenameInZip != "FurnitureDesigns.xml") {
                    throw new InvalidOperationException("Invalid furniture pack.");
                }
                MemoryStream memoryStream = new();
                zipArchive.ExtractFile(list[0], memoryStream);
                memoryStream.Position = 0L;
                XElement overridesNode = XElement.Load(memoryStream);
                ValuesDictionary valuesDictionary = new();
                valuesDictionary.ApplyOverrides(overridesNode);
                return SubsystemFurnitureBlockBehavior.LoadFurnitureDesigns(subsystemTerrain, valuesDictionary);
            }
        }
    }
}