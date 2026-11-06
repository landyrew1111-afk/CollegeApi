using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WpfApp1
{
    public class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
    }

    // Единая точка работы с REST API.
    public static class Api
    {
        private static string _base = "https://localhost:7264/api";

        private static readonly HttpClient Client = CreateClient();

        public static string BaseUrl => _base;

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(40) };
        }

        // Проверяем, на каком адресе запущен API (https или http профиль).
        public static async Task<bool> EnsureAvailableAsync()
        {
            foreach (var candidate in new[] { "https://localhost:7264/api", "http://localhost:5097/api" })
            {
                try
                {
                    var response = await Client.GetAsync(candidate + "/roles");
                    if (response.IsSuccessStatusCode)
                    {
                        _base = candidate;
                        return true;
                    }
                }
                catch
                {
                    // пробуем следующий адрес
                }
            }
            return false;
        }

        public static async Task<T> GetAsync<T>(string path)
        {
            var response = await Client.GetAsync(_base + path);
            var text = await response.Content.ReadAsStringAsync();
            await ThrowIfErrorAsync(response, text);
            return JsonConvert.DeserializeObject<T>(text)!;
        }

        public static async Task<T> PostAsync<T>(string path, object body)
        {
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await Client.PostAsync(_base + path, content);
            var text = await response.Content.ReadAsStringAsync();
            await ThrowIfErrorAsync(response, text);
            return string.IsNullOrWhiteSpace(text) ? default! : JsonConvert.DeserializeObject<T>(text)!;
        }

        public static async Task PostAsync(string path, object body)
        {
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await Client.PostAsync(_base + path, content);
            var text = await response.Content.ReadAsStringAsync();
            await ThrowIfErrorAsync(response, text);
        }

        public static async Task PutAsync(string path, object body)
        {
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await Client.PutAsync(_base + path, content);
            var text = await response.Content.ReadAsStringAsync();
            await ThrowIfErrorAsync(response, text);
        }

        public static async Task DeleteAsync(string path)
        {
            var response = await Client.DeleteAsync(_base + path);
            var text = await response.Content.ReadAsStringAsync();
            await ThrowIfErrorAsync(response, text);
        }

        private static async Task ThrowIfErrorAsync(HttpResponseMessage response, string text)
        {
            if (response.IsSuccessStatusCode) return;

            var message = text;
            try
            {
                var json = JObject.Parse(text);
                message = (string?)json["message"] ?? (string?)json["detail"] ?? (string?)json["title"] ?? text;
            }
            catch
            {
                // тело ответа не JSON — оставляем как есть
            }

            if (string.IsNullOrWhiteSpace(message))
                message = "Ошибка запроса (" + (int)response.StatusCode + ")";

            await Task.CompletedTask;
            throw new ApiException(message);
        }
    }
}
