namespace Game {
    public class UserInfo {
        public readonly string UniqueId;

        public readonly string DisplayName;

        public UserInfo(string uniqueId, string displayName) {
            UniqueId = uniqueId;
            DisplayName = displayName;
        }
    }
}