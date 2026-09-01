using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Game {
    public static class APIUpdateManager {
        /// <summary>
        ///     API是否需要更新？ture：需要；false：不需要；null：正在获取
        /// </summary>
        public static bool? IsNeedUpdate {
            get;
            // ReSharper disable UnusedAutoPropertyAccessor.Local
            private set;
            // ReSharper restore UnusedAutoPropertyAccessor.Local
        }

        /// <summary>
        ///     当前API版本
        /// </summary>
        public static string CurrentVersion => ModsManager.APIVersionString;

        /// <summary>
        ///     网络上最新的API的版本
        /// </summary>
        public static string LatestVersion { get; private set; }

        public static void Initialize() {
#if !DEBUG
            Task.Run(async () => { IsNeedUpdate = await GetIsNeedUpdate(); });
#endif
        }

        /// <summary>
        ///     对比API版本，判断是否需要更新
        /// </summary>
        /// <returns>API统一链接发布的最新版本</returns>
        public static async Task<bool> GetIsNeedUpdate() {
            LatestVersion = await GetLatestVersion(true);
            string currentVersion = ModsManager.APIVersionString;
            return CompareVersion(currentVersion, LatestVersion) == -1;
        }

        /// <summary>
        ///     获取 Gitee release最后一个版本的Json文件数据
        /// </summary>
        /// <returns></returns>
        public static async Task<JsonDocument> GetLatestAPIJsonDocument() =>
            await OnlineJsonReader.GetJsonFromUrlAsync(ModsManager.APILatestReleaseLink_API);

        /// <summary>
        ///     获取 Gitee 所有release版本的Json文件数据
        /// </summary>
        /// <returns></returns>
        public static async Task<JsonDocument> GetAPIReleasesJsonDocument() =>
            await OnlineJsonReader.GetJsonFromUrlAsync(ModsManager.APIReleasesLink_API);

        /// <summary>
        ///     版本号X.X.X.X的正则表达式s
        /// </summary>
        public static Regex VersionRegex = new(@"(\d+)\.?(\d+)?\.?(\d+)?\.?(\d+)?");

        /// <summary>
        ///     将API版本字符串以点分十进制数转为uint
        /// </summary>
        /// <param name="version"></param>
        /// <returns>无符号整数的版本</returns>
        /// <exception cref="FormatException">字符串格式不正确</exception>
        public static uint ParseVersionFromString(string version) {
            Match match = VersionRegex.Match(version);
            if (match.Success) {
                uint result = 0;
                for (int i = 1; i < Math.Min(5, match.Groups.Count); i++) {
                    Group group = match.Groups[i];
                    if (group.Success) {
                        if (byte.TryParse(group.Value, out byte value)) {
                            result |= (uint)value << (8 * (4 - i));
                        }
                        else {
                            throw new FormatException($"Invalid version format: {version}");
                        }
                    }
                }
                return result;
            }
            throw new FormatException($"Invalid version format: {version}");
        }

        /// <summary>
        ///     获取 Gitee release最后一个版本的版本号
        /// </summary>
        /// <returns>最新版本号</returns>
        //当 direct 为真，将获取的 json 直接当做数组中的一个元素
        //常用于链接自带 latest 标识的情况
        public static async Task<string> GetLatestVersion(bool direct) {
            using (JsonDocument remoteDoc = await GetLatestAPIJsonDocument()) {
                JsonElement root = remoteDoc.RootElement;
                // 假设 API 返回的版本信息在第一个 release 的 tag_name 字段
                string input = direct
                    ? root.GetProperty("tag_name").GetString()
                    : root[root.GetArrayLength() - 1].GetProperty("tag_name").GetString();
                return input;
            }
        }

        /// <summary>
        ///     比较两个版本的新旧关系。
        ///     current大于target，返回1
        ///     current小于target，返回-1
        ///     版本相等，返回0
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static int CompareVersion(string current, string target) {
            if (target == "API_OLD") {
                return current == "API_OLD" ? 0 : 1;
            }
            if (current == "API_OLD") {
                return -1;
            }
            uint currentVersion = ParseVersionFromString(current);
            uint targetVersion = ParseVersionFromString(target);
            return currentVersion.CompareTo(targetVersion);
        }
    }

    /*
     这里面的字段名称由于涉及到反序列化解析千万不能改！！
     这里面的字段名称由于涉及到反序列化解析千万不能改！！
     这里面的字段名称由于涉及到反序列化解析千万不能改！！
    */
    public class ReleaseInfo {
        public long id { get; set; }
        public string tag_name { get; set; }
        public string target_commitish { get; set; }
        public bool prerelease { get; set; }
        public string name { get; set; }
        public string body { get; set; }
        public Author author { get; set; }
        public string created_at { get; set; }
        public List<Asset> assets { get; set; }
    }

    /*
     这里面的字段名称由于涉及到反序列化解析千万不能改！！
     这里面的字段名称由于涉及到反序列化解析千万不能改！！
     这里面的字段名称由于涉及到反序列化解析千万不能改！！
    */
    public class Author {
        public long id { get; set; }
        public string login { get; set; }
        public string name { get; set; }
        public string avatar_url { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string remark { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
    }

    /*
     这里面的字段名称由于涉及到反序列化解析千万不能改！！
     这里面的字段名称由于涉及到反序列化解析千万不能改！！
     这里面的字段名称由于涉及到反序列化解析千万不能改！！
    */
    public class Asset {
        public string browser_download_url { get; set; }
        public string name { get; set; }
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
    )]
    [JsonSerializable(typeof(List<ReleaseInfo>))]
    [JsonSerializable(typeof(ReleaseInfo))]
    [JsonSerializable(typeof(Author))]
    [JsonSerializable(typeof(List<Asset>))]
    [JsonSerializable(typeof(Asset))]
    public partial class GiteeReleaseInfoJsonContext : JsonSerializerContext
    {
    }
}