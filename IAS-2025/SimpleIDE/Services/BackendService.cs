using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace SimpleIDE.Services
{
    public class BackendService
    {
        private readonly HttpClient _httpClient;
        private const string BASE_URL = "http://localhost:8000";

        public BackendService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CompileAndRunAsync(string code)
        {
            try
            {
                // Нормализуем переносы строк: заменяем \r\n на \n
                var normalizedCode = code.Replace("\r\n", "\n").Replace("\r", "\n");

                // Убираем лишние пробелы в конце строк
                var lines = normalizedCode.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].TrimEnd();
                }
                normalizedCode = string.Join("\n", lines);

                var requestData = new { code = normalizedCode };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BASE_URL}/api/execute", content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<ExecuteResponse>(responseText);

                    if (result != null && result.success)
                    {
                        return result.output;
                    }
                    else if (result != null)
                    {
                        return $"Ошибка: {result.error}\n\n{result.output}";
                    }
                    else
                    {
                        return responseText;
                    }
                }
                else
                {
                    return $"HTTP Error {response.StatusCode}: {responseText}";
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }
        }

        public async Task<bool> CheckHealth()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_URL}/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private class ExecuteResponse
        {
            public bool success { get; set; }
            public string output { get; set; } = "";
            public string error { get; set; } = "";
            public string message { get; set; } = "";
        }
    }
}