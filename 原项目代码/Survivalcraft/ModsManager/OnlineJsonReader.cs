//通常用于下载并解析 git 平台在线接口的 json 文件

using System.Net.Http;
using System.Text.Json;
using Game.IContentReader;

namespace Game {
    public static class OnlineJsonReader {
        public static readonly HttpClient m_client = new();

        /// <summary>
        ///     从链接获取 Json 文档
        /// </summary>
        /// <param name="url">Json文件链接</param>
        /// <returns>Json文档</returns>
        public static async Task<JsonDocument> GetJsonFromUrlAsync(string url) {
            HttpResponseMessage response = await m_client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string jsonString = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(jsonString, JsonDocumentReader.DefaultJsonOptions);
        }
    }
}