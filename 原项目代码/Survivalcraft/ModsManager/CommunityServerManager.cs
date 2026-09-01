namespace Game {
    public static class CommunityServerManager {
        public enum Type {
            Original = 0,
            Chinese = 1
        }

        public class Info : IEquatable<Info> {
            public Type Type;
            public string Name;
            public string ApiUrl;
            public string WebsiteUrl;

            public Info(Type type, string name, string apiUrl, string websiteUrl = null) {
                Type = type;
                Name = name;
                ApiUrl = apiUrl;
                WebsiteUrl = websiteUrl;
            }

            public bool Equals(Info other) {
                if (other is null) {
                    return false;
                }
                if (ReferenceEquals(this, other)) {
                    return true;
                }
                return Type == other.Type && Name == other.Name && ApiUrl == other.ApiUrl && WebsiteUrl == other.WebsiteUrl;
            }

            public override string ToString() {
                return $"{(int)Type}^^{Name}^^{ApiUrl}^^{WebsiteUrl}";
            }

            public static Info FromString(string str) {
                string[] parts = str.Split("^^");
                if (parts.Length == 4 && int.TryParse(parts[0], out int type)) {
                    return new Info((Type)type, parts[1], parts[2], parts[3]);
                }
                return null;
            }

            public Info Clone() => new(Type, Name, ApiUrl, WebsiteUrl);
        }
#if BROWSER
        public static Info DefaultOriginalInfo = new(Type.Original, string.Empty, "https://cloudflare-cors-anywhere.weathered-shadow-6c41.workers.dev/?https://scresdir.appspot.com/resource");
        public static Info DefaultChineseInfo = new(Type.Chinese, string.Empty, "https://m.suancaixianyu.cn/api/", "https://test.suancaixianyu.cn/#/modList/0");
#else
        public static Info DefaultOriginalInfo = new(Type.Original, string.Empty, "https://scresdir.appspot.com/resource");
        public static Info DefaultChineseInfo = new(Type.Chinese, string.Empty, "https://m.suancaixianyu.cn/api/", "https://test.suancaixianyu.cn/#/modList/0");
#endif

        public static List<Info> UserInfos = [];
        public static Info CurrentOriginalInfo = DefaultOriginalInfo;
        public static Info CurrentChineseInfo = DefaultChineseInfo;

        public static IEnumerable<Info> GetAllInfos() {
            if (SettingsManager.OriginalCommunityContentMode == CommunityContentMode.Disabled) {
                if (SettingsManager.CommunityContentMode != CommunityContentMode.Disabled) {
                    yield return DefaultChineseInfo;
                }
            }
            else if (SettingsManager.CommunityContentMode == CommunityContentMode.Disabled) {
                yield return DefaultOriginalInfo;
            }
            else {
                yield return DefaultOriginalInfo;
                yield return DefaultChineseInfo;
            }
            foreach (Info info in UserInfos) {
                yield return info;
            }
        }

        public static void ChangeOriginalInfo(Info info) {
            if (CurrentOriginalInfo != info && info.Type == Type.Original) {
                OriginalCommunityContentManager.m_feedbackCache.Clear();
                OriginalCommunityContentManager.m_idToAddressMap.Clear();
                CurrentOriginalInfo = info;
            }
        }

        public static void ChangeChineseInfo(Info info) {
            if (CurrentChineseInfo != info && info.Type == Type.Chinese) {
                CommunityContentManager.m_feedbackCache.Clear();
                CommunityContentManager.m_idToAddressMap.Clear();
                foreach (IExternalContentProvider provider in ExternalContentManager.m_providers) {
                    if (provider is SchubExternalContentProvider schubProvider) {
                        provider.Logout();
                    }
                }
                CurrentChineseInfo = info;
            }
        }
    }
}