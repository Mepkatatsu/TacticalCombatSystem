using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Script.CommonLib;

namespace Script.ClientLib.Network.Api
{
    public sealed class ApiClient
    {
        private readonly HttpClient _http;

        public ApiClient(string baseUrl)
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public async Task<T> PostAsync<TRequest, T>(string url, TRequest body, bool skipError = false)
        {
            var json = body.SerializeToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerialize.DeserializeObject<T>(responseString);
                
                return result;
            }

            if (!skipError)
                LogHelper.Error($"PostAsync failed. url:{url}, StatusCode: {response.StatusCode}");

            return default;
        }

        public async Task<T> GetAsync<T>(string url, bool skipError = false)
        {
            var response = await _http.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerialize.DeserializeObject<T>(responseString);
                
                return result;
            }

            if (!skipError)
                Console.Error.WriteLine($"GetAsync failed. url:{url}, StatusCode: {response.StatusCode}");

            return default;
        }
    }
}
