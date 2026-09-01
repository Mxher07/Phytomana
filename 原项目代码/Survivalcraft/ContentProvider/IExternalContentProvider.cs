namespace Game {
    public interface IExternalContentProvider : IDisposable {
        string DisplayName { get; }

        bool SupportsLinks { get; }

        bool SupportsListing { get; }

        bool RequiresLogin { get; }

        bool IsLoggedIn { get; }

        string Description { get; }

        bool IsLocalProvider { get; }

        void Login(CancellableProgress progress, Action success, Action<Exception> failure);

        void Logout();

        void List(string path, CancellableProgress progress, Action<ExternalContentEntry> success, Action<Exception> failure);

        void Download(string path, CancellableProgress progress, Action<Stream> success, Action<Exception> failure);

        void Upload(string path, Stream stream, CancellableProgress progress, Action<string> success, Action<Exception> failure);

        void Link(string path, CancellableProgress progress, Action<string> success, Action<Exception> failure);
    }
}