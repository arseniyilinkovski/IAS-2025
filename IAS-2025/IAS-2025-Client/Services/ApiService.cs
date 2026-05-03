using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IAS_2025_Client.Services
{
    public class CodeExecutionRequest
    {
        public string code { get; set; } = string.Empty;
    }

    public class CodeExecutionResponse
    {
        public bool success { get; set; }
        public string output { get; set; } = string.Empty;
        public string error { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
    }

    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8000";

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(65);
        }

        public async Task<CodeExecutionResponse> ExecuteCode(string code)
        {
            try
            {
                var request = new CodeExecutionRequest { code = code };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/execute", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<CodeExecutionResponse>(responseJson)
                       ?? new CodeExecutionResponse { success = false, error = "Empty response" };
            }
            catch (Exception ex)
            {
                return new CodeExecutionResponse
                {
                    success = false,
                    error = $"API Error: {ex.Message}",
                    message = "Connection failed"
                };
            }
        }

        public async Task<bool> HealthCheck()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}