using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EPIFLOW.API.MOCK.Controllers
{
    public class BiometriaController : ControllerBase
    {

        private readonly string _logFilePath = Path.Combine(AppContext.BaseDirectory, "biometria_log.txt");

        [HttpPost("api/biometria/receber")]
        public IActionResult ReceberBiometria([FromBody] BiometriaRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ColaboradorId) || string.IsNullOrEmpty(request.TemplateBase64))
                return BadRequest("Dados inválidos. Verifique o corpo da requisição.");

            // Simula o processamento do template biométrico
            var sucesso = SimularProcessamento(request.TemplateBase64);

            if (!sucesso)
                return StatusCode(500, "Falha ao processar o template biométrico.");

            // Retorna uma resposta simulada de sucesso
            var response = new
            {
                Status = "OK",
                Mensagem = "Template biométrico recebido e processado com sucesso.",
                ColaboradorId = request.ColaboradorId,
                DataProcessamento = DateTime.Now
            };

            // Salva o conteúdo recebido no arquivo de log
            SalvarLog(request, response);

            return Ok(response);
        }

        private bool SimularProcessamento(string template)
        {
            // Aqui poderíamos validar ou salvar o template em banco.
            // Como é simulação, apenas retorna sucesso se o tamanho do template > 10
            return template.Length > 10;
        }

        private void SalvarLog(BiometriaRequest request, object response)
        {
            try
            {
                var logEntry = new
                {
                    RecebidoEm = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Request = request,
                    Response = response
                };

                var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });

                // Adiciona com separação de linha em branco entre logs
                System.IO.File.AppendAllText(_logFilePath, json + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gravar log: {ex.Message}");
            }
        }
    }


    public class BiometriaRequest
    {
        public string ColaboradorId { get; set; } = string.Empty;
        public string TemplateBase64 { get; set; } = string.Empty;
    }
}
