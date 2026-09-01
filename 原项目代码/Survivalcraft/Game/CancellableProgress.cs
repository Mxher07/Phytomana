namespace Game {
    public class CancellableProgress : Progress {
        public readonly CancellationToken CancellationToken;

        public readonly CancellationTokenSource CancellationTokenSource = new();

        public event Action Cancelled;

        public CancellableProgress() => CancellationToken = CancellationTokenSource.Token;

        public void Cancel() {
            CancellationTokenSource.Cancel();
            Cancelled?.Invoke();
        }
    }
}