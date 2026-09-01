using Engine;
using Engine.Graphics;
using SixLabors.ImageSharp;
using Image = Engine.Media.Image;

namespace Game {
    public static class BlocksTexturesManager {
        public const string fName = "BlocksTexturesManager";
        public static List<string> m_blockTextureNames = [];

        public static Texture2D DefaultBlocksTexture { get; set; }

        public static ReadOnlyList<string> BlockTexturesNames => new(m_blockTextureNames);

        public static string BlockTexturesDirectoryName => ModsManager.BlockTexturesDirectoryName;

        public static event Action<string> BlocksTextureDeleted;

        public static void Initialize() {
            Storage.CreateDirectory(BlockTexturesDirectoryName);
            DefaultBlocksTexture = ContentManager.Get<Texture2D>("Textures/Blocks");
        }

        public static bool IsBuiltIn(string name) => string.IsNullOrEmpty(name);

        public static string GetFileName(string name) {
            if (IsBuiltIn(name)) {
                return null;
            }
            return Storage.CombinePaths(BlockTexturesDirectoryName, name);
        }

        public static string GetDisplayName(string name) {
            if (IsBuiltIn(name)) {
                return LanguageControl.Get("Usual", "gameName");
            }
            return Storage.GetFileNameWithoutExtension(name);
        }

        public static DateTime GetCreationDate(string name) {
            try {
                if (!IsBuiltIn(name)) {
                    return Storage.GetFileLastWriteTime(GetFileName(name));
                }
            }
            catch {
                // ignored
            }
            return new DateTime(2000, 1, 1);
        }

        public static Texture2D LoadTexture(string name) {
            Texture2D texture2D = null;
            if (!IsBuiltIn(name)) {
                try {
                    string fileName = GetFileName(name);
                    if (Storage.FileExists(fileName)) {
                        string extension = Storage.GetExtension(fileName.Replace(".scbtex", "")).ToLower();
                        if (extension == ".astc"
                            || extension == ".astcsrgb") {
                            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                                if (CompressedTexture2D.GetParameters(stream, out int width, out int height, out _, out _)) {
                                    ValidateBlocksTexture(width, height);
                                    texture2D = CompressedTexture2D.Load(stream);
                                }
                            }
                        }
                        else {
                            Image image = Image.Load(fileName);
                            ValidateBlocksTexture(image);
                            texture2D = Texture2D.Load(image);
                            texture2D.Tag = image;
                        }
                    }
                    else {
                        Log.Warning(string.Format(LanguageControl.Get(fName, "1"), name));
                    }
                }
                catch (Exception ex) {
                    Log.Warning(string.Format(LanguageControl.Get(fName, "2"), name, ex.Message));
                }
            }
            texture2D ??= DefaultBlocksTexture;
            return texture2D;
        }

        public static string ImportBlocksTexture(string name, Stream stream) {
            Exception ex = ExternalContentManager.VerifyExternalContentName(name);
            if (ex != null) {
                throw ex;
            }
            string extension = Storage.GetExtension(name).ToLower();
            if (extension != ".scbtex") {
                name += ".scbtex";
            }
            if (extension == ".astc"
                || extension == ".astcsrgb") {
                if (CompressedTexture2D.GetParameters(stream, out int width, out int height, out _, out _)) {
                    ValidateBlocksTexture(width, height);
                }
                else {
                    throw new InvalidOperationException("Invalid ASTC file.");
                }
            }
            else {
                ValidateBlocksTexture(stream);
            }
            stream.Position = 0L;
            using (Stream destination = Storage.OpenFile(GetFileName(name), OpenFileMode.Create)) {
                stream.CopyTo(destination);
                return name;
            }
        }

        public static void DeleteBlocksTexture(string name) {
            try {
                string fileName = GetFileName(name);
                if (!string.IsNullOrEmpty(fileName)) {
                    Storage.DeleteFile(fileName);
                    BlocksTextureDeleted?.Invoke(name);
                }
            }
            catch (Exception e) {
                ExceptionManager.ReportExceptionToUser(string.Format(LanguageControl.Get(fName, "3"), name), e);
            }
        }

        public static void UpdateBlocksTexturesList() {
            m_blockTextureNames.Clear();
            m_blockTextureNames.Add(string.Empty);
            foreach (string item in Storage.ListFileNames(BlockTexturesDirectoryName)) {
                m_blockTextureNames.Add(item);
            }
        }

        /// <summary>
        /// Only for Image type stream.
        /// </summary>
        /// <param name="stream"></param>
        public static void ValidateBlocksTexture(Stream stream) {
            ImageInfo imageInfo = SixLabors.ImageSharp.Image.Identify(stream);
            ValidateBlocksTexture(imageInfo.Width, imageInfo.Height);
        }

        public static void ValidateBlocksTexture(Image image) {
            ValidateBlocksTexture(image.Width, image.Height);
        }

        public static void ValidateBlocksTexture(int width, int height) {
            if (width > 65536
                || height > 65536) {
                throw new InvalidOperationException(string.Format(LanguageControl.Get(fName, "4"), width, height));
            }
            if (!MathUtils.IsPowerOf2(width)
                || !MathUtils.IsPowerOf2(height)) {
                throw new InvalidOperationException(string.Format(LanguageControl.Get(fName, "5"), width, height));
            }
        }
    }
}