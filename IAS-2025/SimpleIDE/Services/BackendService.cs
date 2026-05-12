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
                // Нормализуем переносы строк
                var normalizedCode = code.Replace("\r\n", "\n").Replace("\r", "\n");

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

                // Читаем как массив байтов
                var responseBytes = await response.Content.ReadAsByteArrayAsync();

                // Конвертируем из CP1251 в UTF-8
                var win1251 = Encoding.GetEncoding(1251);
                var responseText = win1251.GetString(responseBytes);

                var result = JsonConvert.DeserializeObject<ExecuteResponse>(responseText);

                if (result != null && result.success)
                {
                    // Ручная замена проблемных символов
                    var fixedOutput = FixRussianX(result.output);
                    return fixedOutput;
                }
                else if (result != null)
                {
                    var fixedError = FixRussianX(result.error);
                    var fixedOutput = FixRussianX(result.output);
                    return $"Ошибка: {fixedError}\n\n{fixedOutput}";
                }
                else
                {
                    return FixRussianX(responseText);
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }
        }

        private string FixRussianX(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            try
            {
                // Конвертируем обратно в байты CP1251, затем в UTF-8
                var win1251 = Encoding.GetEncoding(1251);
                var utf8 = Encoding.UTF8;

                // Получаем байты в CP1251
                var bytes = win1251.GetBytes(text);

                // Специальная обработка для буквы "х" (0xF5)
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] == 0xF5) // Буква "х" в CP1251
                    {
                        // Заменяем на правильную UTF-8 последовательность
                        var newBytes = new List<byte>();
                        newBytes.AddRange(bytes.Take(i));
                        newBytes.Add(0xD1); // Первый байт "х" в UTF-8
                        newBytes.Add(0x85); // Второй байт "х" в UTF-8
                        newBytes.AddRange(bytes.Skip(i + 1));
                        bytes = newBytes.ToArray();
                        i++; // Пропускаем добавленный байт
                    }
                }

                // Конвертируем в UTF-8 строку
                return utf8.GetString(bytes);
            }
            catch
            {
                return text;
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