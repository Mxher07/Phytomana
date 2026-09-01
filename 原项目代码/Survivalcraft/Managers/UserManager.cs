using Engine;

namespace Game {
    public static class UserManager {
        public static List<UserInfo> m_users;

        public static UserInfo ActiveUser {
            get => GetUser(SettingsManager.UserId) ?? GetUsers().FirstOrDefault();
            set => SettingsManager.UserId = value != null ? value.UniqueId : string.Empty;
        }

        static UserManager() {
            m_users = [];
            string text;
            try {
                string path = ModsManager.UserDataPath;
                if (!Storage.FileExists(path)) {
                    text = Guid.NewGuid().ToString();
                    Storage.WriteAllText(path, text);
                }
                else {
                    text = Storage.ReadAllText(path);
                }
            }
            catch (Exception) {
                text = Guid.NewGuid().ToString();
            }
            m_users.Add(new UserInfo(text, "Windows User"));
        }

        public static IEnumerable<UserInfo> GetUsers() => new ReadOnlyList<UserInfo>(m_users);

        public static UserInfo GetUser(string uniqueId) {
            return GetUsers().FirstOrDefault(u => u.UniqueId == uniqueId);
        }
    }
}