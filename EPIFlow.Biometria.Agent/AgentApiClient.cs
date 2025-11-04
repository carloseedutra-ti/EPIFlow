using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EPIFlow.Biometria.Agent.Models;
using Newtonsoft.Json;

namespace EPIFlow.Biometria.Agent
{
    internal sealed class AgentApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Guid _apiKey;
        private readonly Uri _pollUri = new Uri("api/biometria/agent/poll", UriKind.Relative);
        private readonly Uri _completeUri = new Uri("api/biometria/agent/complete", UriKind.Relative);
        private readonly Uri _failUri = new Uri("api/biometria/agent/fail", UriKind.Relative);
        private readonly Uri _configUri = new Uri("api/biometria/agent/config", UriKind.Relative);

        public AgentApiClient()
        {
            _apiKey = AgentSettings.ApiKey;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = AgentSettings.BaseUri;
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public async Task<AgentTaskMessage> PollAsync(CancellationToken cancellationToken)
        {
            if (_apiKey == Guid.Empty)
            {
                return null;
            }

            var request = new
            {
                apiKey = _apiKey
            };

            using (var content = CreateJsonContent(request))
            {
                using (var response = await _httpClient.PostAsync(_pollUri, content, cancellationToken).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        return null;
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new InvalidOperationException("Agente nao autorizado. Verifique a chave configurada.");
                    }

                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<AgentTaskMessage>(json);
                }
            }
        }

        public async Task CompleteTaskAsync(Guid taskId, string templateBase64, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_apiKey == Guid.Empty)
            {
                return;
            }

            var request = new
            {
                apiKey = _apiKey,
                taskId = taskId,
                templateBase64 = templateBase64
            };

            using (var content = CreateJsonContent(request))
            {
                using (var response = await _httpClient.PostAsync(_completeUri, content, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public async Task FailTaskAsync(Guid taskId, string reason, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_apiKey == Guid.Empty)
            {
                return;
            }

            var request = new
            {
                apiKey = _apiKey,
                taskId = taskId,
                reason = reason
            };

            using (var content = CreateJsonContent(request))
            {
                using (var response = await _httpClient.PostAsync(_failUri, content, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public async Task<AgentConfigurationMessage> GetConfigurationAsync(CancellationToken cancellationToken)
        {
            if (_apiKey == Guid.Empty)
            {
                return new AgentConfigurationMessage { Success = false, Message = "Chave do agente não configurada." };
            }

            var request = new
            {
                apiKey = _apiKey
            };

            using (var content = CreateJsonContent(request))
            {
                using (var response = await _httpClient.PostAsync(_configUri, content, cancellationToken).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return new AgentConfigurationMessage { Success = false, Message = "Agente não autorizado." };
                    }

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new AgentConfigurationMessage { Success = false, Message = "Configuração não encontrada." };
                    }

                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonConvert.DeserializeObject<AgentConfigurationMessage>(json);
                    return result ?? new AgentConfigurationMessage { Success = false, Message = "Resposta inválida do servidor." };
                }
            }
        }

        private static StringContent CreateJsonContent(object value)
        {
            var json = JsonConvert.SerializeObject(value);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}






