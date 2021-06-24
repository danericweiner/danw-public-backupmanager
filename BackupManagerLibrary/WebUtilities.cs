using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using static BackupManagerLibrary.IOUtilities;

namespace BackupManagerLibrary
{
    public class HeaderItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public static class WebUtilities
    {
        private static HttpClient _httpClient = null;
        private static HttpClient HttpClientInstance {
            get {
                if (_httpClient != null) { return _httpClient; }

                HttpClientHandler httpClientHandler = new HttpClientHandler() { MaxConnectionsPerServer = Constants.HttpClientOptions.MaxConnectionsPerServer };
                _httpClient = new HttpClient(httpClientHandler) { Timeout = TimeSpan.FromSeconds(Constants.HttpClientOptions.TimeOutSeconds) };
                return _httpClient;
            }
        }

        public static async Task DownloadFileAsync(string url, string filename, DateTime? lastWriteTime = null, params HeaderItem[] headers) {
            HttpContent httpContent = await GetHttpContentAsync(url, headers);
            Stream stream = await httpContent.ReadAsStreamAsync();
            WriteStreamToFile(stream, filename, lastWriteTime);
        }

        public static async Task<TResponse> GetJsonObjectAsync<TResponse>(string url, params HeaderItem[] headers) {
            HttpContent httpContent = await GetHttpContentAsync(url, headers);
            string jsonString = await httpContent.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(jsonString);
        }

        private static async Task<HttpContent> GetHttpContentAsync(string url, params HeaderItem[] headers) {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            foreach (HeaderItem headerItem in headers) {
                request.Headers.Add(headerItem.Key, headerItem.Value);
            }
            HttpResponseMessage httpResponseMessage = await HttpClientInstance.SendAsync(request).ConfigureAwait(false);
            if (!httpResponseMessage.IsSuccessStatusCode) {
                string responseContent = "";
                try { responseContent = await httpResponseMessage.Content.ReadAsStringAsync(); } catch { }
                throw new Exception($"HttpClientInstance returned an unsuccessful status code '{(int)httpResponseMessage.StatusCode}:{httpResponseMessage.StatusCode.ToString()}' and the following content:\n{responseContent}");
            }
            return httpResponseMessage.Content;
        }
    }
}
