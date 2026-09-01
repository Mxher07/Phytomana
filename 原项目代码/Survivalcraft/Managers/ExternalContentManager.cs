using System.Text;
using Engine;

namespace Game {
    public static class ExternalContentManager {
        public static List<IExternalContentProvider> m_providers;
        public static string fName = "ExternalContentManager";

        //在游戏进入主菜单之前，如果openFilePath为一个文件（存档、模组）路径，那么在进入游戏后，会进行对文件的安装操作
        [Obsolete]
        public static string openFilePath = string.Empty;
        public static IExternalContentProvider DefaultProvider => Providers.Count <= 0 ? null : Providers[0];

        public static ReadOnlyList<IExternalContentProvider> Providers => new(m_providers);

        public static void Initialize() {
            m_providers = [
#if !ANDROID
                new DiskExternalContentProvider(),
#endif
#if ANDROID
                new AndroidSdCardExternalContentProvider(),
#endif
                new SchubExternalContentProvider(),
                new DropboxExternalContentProvider()
            ];
        }

        public static ExternalContentType ExtensionToType(string extension) {
            extension = extension.ToLower();
            foreach (ExternalContentType value in Enum.GetValues<ExternalContentType>()) {
                if (GetEntryTypeExtensions(value).FirstOrDefault(e => e == extension) != null) {
                    return value;
                }
            }
            return ExternalContentType.Unknown;
        }

        public static IEnumerable<string> GetEntryTypeExtensions(ExternalContentType type) {
            switch (type) {
                case ExternalContentType.World: yield return ".scworld"; break;
                case ExternalContentType.BlocksTexture:
                    yield return ".scbtex";
                    yield return ".webp";
                    yield return ".png";
                    yield return ".astc";
                    yield return ".astcsrgb";
                    break;
                case ExternalContentType.CharacterSkin: yield return ".scskin"; break;
                case ExternalContentType.FurniturePack: yield return ".scfpack"; break;
                case ExternalContentType.Mod: yield return ".scmod"; break;
                case ExternalContentType.ModList: yield return ".scmodList"; break;
            }
        }

        public static Subtexture GetEntryTypeIcon(ExternalContentType type) {
            return type switch {
                ExternalContentType.Directory => ContentManager.Get<Subtexture>("Textures/Atlas/FolderIcon"),
                ExternalContentType.World => ContentManager.Get<Subtexture>("Textures/Atlas/WorldIcon"),
                ExternalContentType.BlocksTexture => ContentManager.Get<Subtexture>("Textures/Atlas/TexturePackIcon"),
                ExternalContentType.CharacterSkin => ContentManager.Get<Subtexture>("Textures/Atlas/CharacterSkinIcon"),
                ExternalContentType.FurniturePack => ContentManager.Get<Subtexture>("Textures/Atlas/FurnitureIcon"),
                ExternalContentType.Mod => ContentManager.Get<Subtexture>("Textures/Gui/DefaultModIcon"),
                _ => ContentManager.Get<Subtexture>("Textures/Atlas/QuestionMarkIcon")
            };
        }

        public static string GetEntryTypeDescription(ExternalContentType type) {
            return type switch {
                ExternalContentType.Directory => LanguageControl.Get(fName, "Directory"),
                ExternalContentType.World => LanguageControl.Get(fName, "World"),
                ExternalContentType.BlocksTexture => LanguageControl.Get(fName, "Blocks Texture"),
                ExternalContentType.CharacterSkin => LanguageControl.Get(fName, "Character Skin"),
                ExternalContentType.FurniturePack => LanguageControl.Get(fName, "Furniture Pack"),
                ExternalContentType.Mod => LanguageControl.Get(fName, "Mod"),
                _ => string.Empty
            };
        }

        public static bool IsEntryTypeDownloadSupported(ExternalContentType type) {
            return type switch {
                ExternalContentType.World => true,
                ExternalContentType.BlocksTexture => true,
                ExternalContentType.CharacterSkin => true,
                ExternalContentType.FurniturePack => true,
                ExternalContentType.Mod => true,
                _ => false
            };
        }

        public static bool DoesEntryTypeRequireName(ExternalContentType type) {
            return type switch {
                ExternalContentType.BlocksTexture => true,
                ExternalContentType.CharacterSkin => true,
                ExternalContentType.FurniturePack => true,
                _ => false
            };
        }

        public static Exception VerifyExternalContentName(string name) {
            if (string.IsNullOrEmpty(name)) {
                return new InvalidOperationException(LanguageControl.Get(fName, 1));
            }
            if (name.Length > 50) {
                return new InvalidOperationException(LanguageControl.Get(fName, 2));
            }
            if (name[0] == ' '
                || name[^1] == ' ') {
                return new InvalidOperationException(LanguageControl.Get(fName, 3));
            }
            return null;
        }

        public static void DeleteExternalContent(ExternalContentType type, string name) {
            switch (type) {
                case ExternalContentType.World: WorldsManager.DeleteWorld(name); break;
                case ExternalContentType.BlocksTexture: BlocksTexturesManager.DeleteBlocksTexture(name); break;
                case ExternalContentType.CharacterSkin: CharacterSkinsManager.DeleteCharacterSkin(name); break;
                case ExternalContentType.FurniturePack: FurniturePacksManager.DeleteFurniturePack(name); break;
                default: throw new InvalidOperationException(LanguageControl.Get(fName, 4));
            }
        }

        public static void ImportExternalContent(Stream stream,
            ExternalContentType type,
            string name,
            Action<string> success,
            Action<Exception> failure) {
            Task.Run(
                delegate {
                    try {
                        success?.Invoke(ImportExternalContentSync(stream, type, name));
                    }
                    catch (Exception obj) {
                        failure?.Invoke(obj);
                    }
                }
            );
        }

        public static string ImportExternalContentSync(Stream stream, ExternalContentType type, string name) {
            switch (type) {
                case ExternalContentType.World: return WorldsManager.ImportWorld(stream);
                case ExternalContentType.BlocksTexture: return BlocksTexturesManager.ImportBlocksTexture(name, stream);
                case ExternalContentType.CharacterSkin: return CharacterSkinsManager.ImportCharacterSkin(name, stream);
                case ExternalContentType.FurniturePack: return FurniturePacksManager.ImportFurniturePack(name, stream);
                case ExternalContentType.Mod: return ModsManager.ImportMod(name, stream);
                default: throw new InvalidOperationException(LanguageControl.Get(fName, 4));
            }
        }

        public static async Task ImportExternalContentsAsync(List<(Stream stream, string fileName)> files, bool showResultDialog) {
            string successFormat = LanguageControl.Get(fName, 18);
            string failureFormat = LanguageControl.Get(fName, 19);
            string unsupportedString = LanguageControl.Get(fName, 12);

            async Task<string> Import(Stream stream, string fileName) {
                return await Task.Run(() => {
                        ExternalContentType type = ExtensionToType(Storage.GetExtension(fileName));
                        if (IsEntryTypeDownloadSupported(type)) {
                            try {
                                ImportExternalContentSync(stream, type, fileName);
                            }
                            catch (Exception e) {
                                return string.Format(failureFormat, fileName, e.Message);
                            }
                            finally {
                                stream.Dispose();
                            }
                            return string.Format(successFormat, fileName);
                        }
                        return string.Format(failureFormat, fileName, unsupportedString);
                    }
                );
            }

            Task<string>[] tasks = new Task<string>[files.Count];
            for (int i = 0; i < files.Count; i++) {
                (Stream stream, string fileName) = files[i];
                tasks[i] = Import(stream, fileName);
            }
            string[] results = await Task.WhenAll(tasks);
            StringBuilder sb = new();
            foreach (string result in results) {
                sb.AppendLine(result);
            }
            string finalResult = sb.ToString();
            Log.Information(finalResult);
            if (showResultDialog) {
                Dispatcher.Dispatch(() => {
                        DialogsManager.ShowDialog(
                            null,
                            new MessageDialog(LanguageControl.Get(fName, 20), finalResult, LanguageControl.Ok, null, null)
                        );
                    }
                );
            }
        }

        public static void ImportExternalContentsSync(List<(Stream stream, string fileName)> files, bool showResultDialog) {
            string successFormat = LanguageControl.Get(fName, 18);
            string failureFormat = LanguageControl.Get(fName, 19);
            string unsupportedString = LanguageControl.Get(fName, 12);
            StringBuilder sb = new();
            foreach ((Stream stream, string fileName) in files) {
                ExternalContentType type = ExtensionToType(Storage.GetExtension(fileName));
                if (IsEntryTypeDownloadSupported(type)) {
                    try {
                        ImportExternalContentSync(stream, type, fileName);
                        sb.AppendLine(string.Format(successFormat, fileName));
                    }
                    catch (Exception e) {
                        sb.AppendLine(string.Format(failureFormat, fileName, e.Message));
                    }
                }
                else {
                    sb.AppendLine(string.Format(failureFormat, fileName, unsupportedString));
                }
            }
            string finalResult = sb.ToString();
            Log.Information(finalResult);
            if (showResultDialog) {
                DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Get(fName, 20), finalResult, LanguageControl.Ok, null, null));
            }
        }

        public static void ShowLoginUiIfNeeded(IExternalContentProvider provider, bool showWarningDialog, Action handler) {
            if (provider.RequiresLogin
                && !provider.IsLoggedIn) {
                void LoginAction() {
                    CancellableBusyDialog busyDialog = new(LanguageControl.Get(fName, 5), true);
                    DialogsManager.ShowDialog(null, busyDialog);
                    provider.Login(
                        busyDialog.Progress,
                        delegate {
                            DialogsManager.HideDialog(busyDialog);
                            handler?.Invoke();
                        },
                        delegate(Exception error) {
                            DialogsManager.HideDialog(busyDialog);
                            if (error != null) {
                                DialogsManager.ShowDialog(
                                    null,
                                    new MessageDialog(LanguageControl.Error, error.Message, LanguageControl.Ok, null, null)
                                );
                            }
                        }
                    );
                }

                if (showWarningDialog) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(
                            LanguageControl.Get(fName, 6),
                            string.Format(LanguageControl.Get(fName, 7), provider.DisplayName),
                            LanguageControl.Get(fName, 8),
                            LanguageControl.Cancel,
                            delegate(MessageDialogButton b) {
                                if (b == MessageDialogButton.Button1) {
                                    LoginAction();
                                }
                            }
                        )
                    );
                }
                else {
                    LoginAction();
                }
            }
            else {
                handler?.Invoke();
            }
        }

        public static void ShowUploadUi(ExternalContentType type, string name) {
            DialogsManager.ShowDialog(
                null,
                new SelectExternalContentProviderDialog(
                    LanguageControl.Get(fName, 9),
                    false,
                    delegate(IExternalContentProvider provider) {
                        try {
                            if (provider != null) {
                                ShowLoginUiIfNeeded(
                                    provider,
                                    true,
                                    delegate {
                                        CancellableBusyDialog busyDialog = new(LanguageControl.Get(fName, 10), false);
                                        DialogsManager.ShowDialog(null, busyDialog);
                                        Task.Run(
                                            delegate {
                                                bool needsDelete = false;
                                                string sourcePath = null;
                                                Stream stream = null;

                                                void Cleanup() {
                                                    // ReSharper disable AccessToModifiedClosure
                                                    Utilities.Dispose(ref stream);
                                                    // ReSharper restore AccessToModifiedClosure
                                                    if (needsDelete && sourcePath != null) {
                                                        try {
                                                            Storage.DeleteFile(sourcePath);
                                                        }
                                                        catch {
                                                            // ignored
                                                        }
                                                    }
                                                }

                                                try {
                                                    string path;
                                                    if (type == ExternalContentType.BlocksTexture) {
                                                        sourcePath = BlocksTexturesManager.GetFileName(name);
                                                        if (sourcePath == null) {
                                                            throw new InvalidOperationException(LanguageControl.Get(fName, 11));
                                                        }
                                                        path = Storage.GetFileName(sourcePath);
                                                    }
                                                    else if (type == ExternalContentType.CharacterSkin) {
                                                        sourcePath = CharacterSkinsManager.GetFileName(name);
                                                        if (sourcePath == null) {
                                                            throw new InvalidOperationException(LanguageControl.Get(fName, 11));
                                                        }
                                                        path = Storage.GetFileName(sourcePath);
                                                    }
                                                    else if (type == ExternalContentType.FurniturePack) {
                                                        sourcePath = FurniturePacksManager.GetFileName(name);
                                                        if (sourcePath == null) {
                                                            throw new InvalidOperationException(LanguageControl.Get(fName, 11));
                                                        }
                                                        path = Storage.GetFileName(sourcePath);
                                                    }
                                                    else {
                                                        if (type != ExternalContentType.World) {
                                                            throw new InvalidOperationException(LanguageControl.Get(fName, 12));
                                                        }
                                                        busyDialog.LargeMessage = LanguageControl.Get(fName, 13);
                                                        sourcePath = Storage.CombinePaths(ModsManager.DocPath, "WorldUpload.tmp");
                                                        needsDelete = true;
                                                        string name2 = WorldsManager.GetWorldInfo(name).WorldSettings.Name;
                                                        path = $"{name2}.scworld";
                                                        using (Stream targetStream = Storage.OpenFile(sourcePath, OpenFileMode.Create)) {
                                                            WorldsManager.ExportWorld(name, targetStream);
                                                        }
                                                    }
                                                    busyDialog.LargeMessage = LanguageControl.Get(fName, 14);
                                                    stream = Storage.OpenFile(sourcePath, OpenFileMode.Read);
                                                    provider.Upload(
                                                        path,
                                                        stream,
                                                        busyDialog.Progress,
                                                        delegate(string link) {
                                                            // ReSharper disable AccessToModifiedClosure
                                                            long length = stream.Length;
                                                            // ReSharper restore AccessToModifiedClosure
                                                            Cleanup();
                                                            DialogsManager.HideDialog(busyDialog);
                                                            if (provider.IsLocalProvider) {
#if ANDROID || BROWSER
                                                                DialogsManager.ShowDialog(
                                                                    null,
                                                                    new MessageDialog(
                                                                        LanguageControl.Success,
                                                                        $"{string.Format(LanguageControl.Get(fName, 15), DataSizeFormatter.Format(length))}\n{LanguageControl.Get(fName, "21")}{link}",
                                                                        LanguageControl.Get(fName, "16"),
                                                                        LanguageControl.Get(fName, "17"),
                                                                        button => {
                                                                            if (button == MessageDialogButton.Button1) {
                                                                                Task.Run(async () => {
                                                                                    try {
                                                                                        await Storage.ShareFile(link);
                                                                                    }
                                                                                    catch (Exception e) {
                                                                                        Dispatcher.Dispatch(() => DialogsManager.ShowDialog(
                                                                                                null,
                                                                                                new MessageDialog(
                                                                                                    LanguageControl.Error,
                                                                                                    e.Message,
                                                                                                    LanguageControl.Ok,
                                                                                                    null,
                                                                                                    null
                                                                                                )
                                                                                            )
                                                                                        );
                                                                                    }
                                                                                });
                                                                            }
                                                                        }
                                                                    )
                                                                );
#else
                                                                DialogsManager.ShowDialog(
                                                                    null,
                                                                    new MessageDialog(
                                                                        LanguageControl.Success,
                                                                        $"{string.Format(LanguageControl.Get(fName, 15), DataSizeFormatter.Format(length))}\n{LanguageControl.Get(fName, "21")}{link}",
                                                                        LanguageControl.Ok,
                                                                        null,
                                                                        null
                                                                    )
                                                                );
#endif
                                                            }
                                                            else {
                                                                DialogsManager.ShowDialog(null, new ExternalContentLinkDialog(link));
                                                            }
                                                        },
                                                        delegate(Exception error) {
                                                            Cleanup();
                                                            DialogsManager.HideDialog(busyDialog);
                                                            DialogsManager.ShowDialog(
                                                                null,
                                                                new MessageDialog(
                                                                    LanguageControl.Error,
                                                                    error.Message,
                                                                    LanguageControl.Ok,
                                                                    null,
                                                                    null
                                                                )
                                                            );
                                                        }
                                                    );
                                                }
                                                catch (Exception ex2) {
                                                    Cleanup();
                                                    DialogsManager.HideDialog(busyDialog);
                                                    DialogsManager.ShowDialog(
                                                        null,
                                                        new MessageDialog(LanguageControl.Error, ex2.Message, LanguageControl.Ok, null, null)
                                                    );
                                                }
                                            }
                                        );
                                    }
                                );
                            }
                        }
                        catch (Exception ex) {
                            DialogsManager.ShowDialog(null, new MessageDialog(LanguageControl.Error, ex.Message, LanguageControl.Ok, null, null));
                        }
                    }
                )
            );
        }
    }
}