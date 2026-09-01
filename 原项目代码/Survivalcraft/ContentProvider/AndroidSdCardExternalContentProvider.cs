#if ANDROID
using Engine;
using Game;
public class AndroidSdCardExternalContentProvider : IExternalContentProvider {
    string m_rootDirectory;
    public static string fName = "AndroidSdCardExternalContentProvider";
    public string DisplayName => LanguageControl.Get(fName, 1);

    public string Description {
        get {
            InitializeFilesystemAccess();
            return m_rootDirectory;
        }
    }

    public bool SupportsListing => true;

    public bool SupportsLinks => false;

    public bool RequiresLogin => false;

    public bool IsLoggedIn => true;

    public bool IsLocalProvider => true;

    public void Dispose() { }

    public void Login(CancellableProgress progress, Action success, Action<Exception> failure) => failure(new NotSupportedException());

    public void Logout() {
        throw new NotSupportedException();
    }

    public void List(string path, CancellableProgress progress, Action<ExternalContentEntry> success, Action<Exception> failure) {
        ThreadPool.QueueUserWorkItem(
            delegate {
                try {
                    InitializeFilesystemAccess();
                    string internalPath = ToInternalPath(path);
                    ExternalContentEntry entry = GetDirectoryEntry(internalPath, true);
                    Dispatcher.Dispatch(delegate { success(entry); });
                }
                catch (Exception ex) {
                    Dispatcher.Dispatch(delegate { failure(ex); });
                }
            }
        );
    }

    public void Download(string path, CancellableProgress progress, Action<Stream> success, Action<Exception> failure) {
        ThreadPool.QueueUserWorkItem(
            delegate {
                try {
                    InitializeFilesystemAccess();
                    string path2 = ToInternalPath(path);
                    FileStream stream = new(path2, FileMode.Open, FileAccess.Read, FileShare.Read);
                    Dispatcher.Dispatch(delegate { success(stream); });
                }
                catch (Exception ex) {
                    Dispatcher.Dispatch(delegate { failure(ex); });
                }
            }
        );
    }

    public void Upload(string path, Stream stream, CancellableProgress progress, Action<string> success, Action<Exception> failure) {
        ThreadPool.QueueUserWorkItem(
            delegate {
                try {
                    InitializeFilesystemAccess();
                    string uniquePath = GetUniquePath(ToInternalPath(path));
                    string po = uniquePath;
                    if (po.StartsWith("android:")) {
                        po = Storage.GetSystemPath(po);
                    }
                    string pp = Storage.GetDirectoryName(po);
                    Directory.CreateDirectory(pp);
                    using (FileStream destination = new(po, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        stream.CopyTo(destination);
                    }
                    Dispatcher.Dispatch(delegate { success(uniquePath); });
                }
                catch (Exception ex) {
                    Dispatcher.Dispatch(delegate { failure(ex); });
                }
            }
        );
    }

    public void Link(string path, CancellableProgress progress, Action<string> success, Action<Exception> failure) {
        failure(new NotSupportedException());
    }

    public ExternalContentEntry GetDirectoryEntry(string internalPath, bool scanContents) {
        ExternalContentEntry externalContentEntry = new() {
            Type = ExternalContentType.Directory, Path = ToExternalPath(internalPath), Time = new DateTime(1970, 1, 1)
        };
        if (scanContents) {
            string[] directories = Directory.GetDirectories(internalPath);
            foreach (string internalPath2 in directories) {
                externalContentEntry.ChildEntries.Add(GetDirectoryEntry(internalPath2, false));
            }
            directories = Directory.GetFiles(internalPath);
            foreach (string text in directories) {
                FileInfo fileInfo = new(text);
                ExternalContentEntry externalContentEntry2 = new() {
                    Type = ExternalContentManager.ExtensionToType(Path.GetExtension(text)),
                    Path = ToExternalPath(text),
                    Size = fileInfo.Length,
                    Time = fileInfo.CreationTime
                };
                externalContentEntry.ChildEntries.Add(externalContentEntry2);
            }
        }
        return externalContentEntry;
    }

    public static string GetUniquePath(string path) {
        int num = 1;
        string text = path;
        string directoryName = Path.GetDirectoryName(path);
        if (directoryName == null) {
            return path;
        }
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        while (Storage.FileExists(text)
            && num < 1000) {
            string path2 = fileNameWithoutExtension + num + extension;
            text = Path.Combine(directoryName, path2);
            num++;
        }
        return text;
    }

    public string ToExternalPath(string internalPath) => Path.GetFullPath(internalPath);

    public string ToInternalPath(string externalPath) => Path.Combine(m_rootDirectory, externalPath);

    public void InitializeFilesystemAccess() {
        //Java.IO.File externalFilesDir = ((Context)Window.Activity).GetExternalFilesDir((string)null);
        m_rootDirectory = $"{RunPath.AndroidFilePath}/files";
        if (!Storage.DirectoryExists(m_rootDirectory)) {
            Storage.CreateDirectory(m_rootDirectory);
        }
    }
}
#endif