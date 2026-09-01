namespace Engine.Graphics {
    public class LockOnFirstUse {
        internal bool IsLocked;

        protected void ThrowIfLocked() {
            if (IsLocked) {
                throw new InvalidOperationException("Object was attached to a device and can no longer be modified.");
            }
        }
    }
}