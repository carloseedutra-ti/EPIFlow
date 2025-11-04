using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EPIFlow.Biometria.Agent.Models;
using Newtonsoft.Json;

namespace EPIFlow.Biometria.Agent
{
    internal sealed class AgentConfigurationTester
    {
        private readonly string _baseUrl;
        private readonly Guid _apiKey;

        public AgentConfigurationTester(string baseUrl, Guid apiKey)
        {
            _baseUrl = NormalizeBaseUrl(baseUrl);
            _apiKey = apiKey;
        }

        public async Task<AgentConfigurationMessage> ExecuteAsync()
        {
            if (_apiKey == Guid.Empty)
            {
                return new AgentConfigurationMessage { Success = false, Message = "Chave inválida." };
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_baseUrl);
                client.Timeout = TimeSpan.FromSeconds(15);

                var payload = new { apiKey = _apiKey };
                var json = JsonConvert.SerializeObject(payload);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    var response = await client.PostAsync("api/biometria/agent/config", content).ConfigureAwait(false);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return new AgentConfigurationMessage { Success = false, Message = "Agente não autorizado." };
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        return new AgentConfigurationMessage { Success = false, Message = "Falha ao conectar: " + response.StatusCode };
                    }

                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonConvert.DeserializeObject<AgentConfigurationMessage>(body);
                    return result ?? new AgentConfigurationMessage { Success = false, Message = "Resposta inválida." };
                }
            }
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return "https://localhost:5001/";
            }

            baseUrl = baseUrl.Trim();
            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                baseUrl += "/";
            }

            return baseUrl;
        }
    }
}
