using Engine;

namespace Game {
    public class DiskExternalContentProvider : IExternalContentProvider {
        public string DisplayName => LanguageControl.Get(fName, "DisplayName");

        public bool SupportsLinks => false;

        public bool SupportsListing => true;

        public bool RequiresLogin => false;

        public bool IsLoggedIn => true;

        public static string fName = "DiskExternalContentProvider";

        public static string LocalPath = "app:";

        public bool IsLocalProvider => true;

        public string Description => LanguageControl.Get(fName, "Description");

        public DiskExternalContentProvider() {
            if (!Storage.DirectoryExists(LocalPath)) {
                Storage.CreateDirectory(LocalPath);
            }
#if BROWSER
            if (!Storage.DirectoryExists(Path.Combine(LocalPath, "Uploads"))) {
                Storage.CreateDirectory(Path.Combine(LocalPath, "Uploads"));
            }
#endif
        }

        public void Dispose() { }

        public void Download(string path, CancellableProgress progress, Action<Stream> success, Action<Exception> failure) {
            if (!File.Exists(path)) {
                failure(new FileNotFoundException());
                return;
            }
            FileStream fileStream = File.OpenRead(path);
            ThreadPool.QueueUserWorkItem(
                delegate {
                    try {
                        success(fileStream);
                    }
                    catch (Exception ex) {
                        failure(ex);
                    }
                }
            );
        }

        public void Link(string path, CancellableProgress progress, Action<string> success, Action<Exception> failure) {
            failure(new NotSupportedException());
        }

        public void List(string path, CancellableProgress progress, Action<ExternalContentEntry> success, Action<Exception> failure) {
            ThreadPool.QueueUserWorkItem(
                delegate {
                    try {
                        string internalPath = path;
                        ExternalContentEntry entry = GetDirectoryEntry(internalPath, true);
                        success(entry);
                    }
                    catch (Exception ex) {
                        failure(ex);
                    }
                }
            );
        }

        public void Login(CancellableProgress progress, Action success, Action<Exception> failure) {
            failure(new NotSupportedException());
        }

        public void Logout() {
            throw new NotSupportedException();
        }

        public void Upload(string path, Stream stream, CancellableProgress progress, Action<string> success, Action<Exception> failure) {
            ThreadPool.QueueUserWorkItem(
                delegate {
                    try {
#if BROWSER
                        string destinationPath = Path.Combine(LocalPath, "Uploads", path);
#else
                        string destinationPath = Path.Combine(LocalPath, path);
#endif
                        using (Stream destination = Storage.OpenFile(destinationPath, OpenFileMode.Create)) {
                            stream.CopyTo(destination);
                        }
                        Dispatcher.Dispatch(delegate { success(destinationPath); });
                    }
                    catch (Exception ex) {
                        Dispatcher.Dispatch(delegate { failure(ex); });
                    }
                }
            );
        }

        public ExternalContentEntry GetDirectoryEntry(string internalPath, bool scanContents) {
            ExternalContentEntry externalContentEntry = new() {
                Type = ExternalContentType.Directory, Path = internalPath, Time = new DateTime(1970, 1, 1)
            };
            if (scanContents) {
                if (internalPath.Length == 2
                    && internalPath[1] == ':') {
                    internalPath += '/';
                }
                string[] directories = Directory.GetDirectories(internalPath);
                foreach (string internalPath2 in directories) {
                    externalContentEntry.ChildEntries.Add(GetDirectoryEntry(internalPath2, false));
                }
                directories = Directory.GetFiles(internalPath);
                foreach (string text in directories) {
                    FileInfo fileInfo = new(text);
                    ExternalContentEntry externalContentEntry2 = new() {
                        Type = ExternalContentManager.ExtensionToType(Path.GetExtension(text)),
                        Path = text,
                        Size = fileInfo.Length,
                        Time = fileInfo.CreationTime
                    };
                    externalContentEntry.ChildEntries.Add(externalContentEntry2);
                }
            }
            return externalContentEntry;
        }
    }
}