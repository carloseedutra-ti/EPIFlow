using DPFP;
using DPFP.Capture;
using DPFP.Processing;
using DPFP.Verification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Windows.Forms; // Necessário para Application.Run()
using System.Linq;

namespace EPIFlow.BiometriaSvc
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private WebApplication? _app;
        private HiddenCaptureForm? _form;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Cria uma thread STA separada para o leitor biométrico
                Thread staThread = new Thread(() =>
                {
                    try
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);

                        _form = new HiddenCaptureForm(_logger);
                        IniciarServidor(stoppingToken);
                        Application.Run(_form); // Mantém o loop de mensagens ativo
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro na thread STA do leitor biométrico");
                    }
                });

                // ⚠️ Importante: define STA ANTES de iniciar
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.IsBackground = true;
                staThread.Start();

                // Mantém o worker ativo
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar o serviço biométrico");
            }
        }

        private void IniciarServidor(CancellationToken stoppingToken)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://localhost:5050");
            var app = builder.Build();

            // Endpoint: iniciar leitura biométrica
            app.MapGet("/api/biometria/iniciar", () =>
            {
                _form?.BeginInvoke(new Action(() => _form.StartCapture()));
                File.WriteAllText("status.json", "{\"status\":\"aguardando\",\"mensagem\":\"Posicione o dedo no leitor.\"}");
                return Results.Ok(new
                {
                    status = "ok",
                    mensagem = "Leitura iniciada, posicione o dedo no leitor."
                });
            });

            // Endpoint: verificar status da leitura
            app.MapGet("/api/biometria/status", () =>
            {
                var statusFile = Path.Combine(AppContext.BaseDirectory, "status.json");
                if (!File.Exists(statusFile))
                    return Results.Json(new { status = "idle", mensagem = "Aguardando leitura." });

                var json = File.ReadAllText(statusFile);
                return Results.Text(json, "application/json");
            });

            _app = app;
            _ = _app.RunAsync(stoppingToken);
            _logger.LogInformation("Servidor HTTP local iniciado em http://localhost:5050");
        }
    }

    // ======================================================
    // FORM INVISÍVEL QUE MANTÉM O CONTEXTO DO LEITOR
    // ======================================================
    public class HiddenCaptureForm : Form, DPFP.Capture.EventHandler
    {
        private readonly ILogger _logger;
        private Capture? _capturador;
        private Verification? _verificador;

        public HiddenCaptureForm(ILogger logger)
        {
            _logger = logger;
            this.Visible = false;
            this.ShowInTaskbar = false;
            this.Load += HiddenCaptureForm_Load;
        }

        private void HiddenCaptureForm_Load(object? sender, EventArgs e)
        {
            try
            {
                var readers = new ReadersCollection();
                if (readers.Count == 0)
                {
                    _logger.LogError("Nenhum leitor biométrico encontrado. Verifique a conexão USB ou o driver.");
                    return;
                }

                _logger.LogInformation("Leitores detectados:");
                foreach (var pair in readers)
                {
                    var r = pair.Value;
                    _logger.LogInformation($"  - {r.SerialNumber} - {r.ProductName}");
                }

                var selected = readers.First().Value;
                _logger.LogInformation($"Usando leitor: {selected.SerialNumber} - {selected.ProductName}");

                _capturador = new Capture(selected.SerialNumber, Priority.Normal);
                _verificador = new Verification();
                _capturador.EventHandler = this;

                _logger.LogInformation("Leitor biométrico inicializado e aguardando leitura.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar o leitor biométrico");
            }
        }

        public void StartCapture()
        {
            try
            {
                _capturador?.StartCapture();
                _logger.LogInformation("Captura iniciada. Posicione o dedo no leitor.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar captura");
            }
        }

        // ======================================================
        // EVENTOS DO SDK DIGITALPERSONA
        // ======================================================
        public void OnComplete(object Capture, string ReaderSerialNumber, Sample Sample)
        {
            try
            {
                _logger.LogInformation("Amostra recebida do leitor: {Serial}", ReaderSerialNumber);

                var extractor = new FeatureExtraction();
                var feedback = CaptureFeedback.None;
                var features = new FeatureSet();
                extractor.CreateFeatureSet(Sample, DataPurpose.Verification, ref feedback, ref features);

                if (feedback != CaptureFeedback.Good)
                {
                    File.WriteAllText("status.json", "{\"status\":\"erro\",\"mensagem\":\"Amostra ruim. Tente novamente.\"}");
                    _logger.LogWarning("Amostra ruim, tente novamente.");
                    return;
                }

                var templatePath = Path.Combine(AppContext.BaseDirectory, "template_colaborador.dpft");
                if (!File.Exists(templatePath))
                {
                    File.WriteAllText("status.json", "{\"status\":\"erro\",\"mensagem\":\"Template não encontrado.\"}");
                    _logger.LogWarning("Template não encontrado em {Path}", templatePath);
                    return;
                }

                var templateBytes = File.ReadAllBytes(templatePath);
                var template = new Template(new MemoryStream(templateBytes));
                var result = new Verification.Result();
                _verificador!.Verify(features, template, ref result);

                var json = result.Verified
                    ? "{\"status\":\"ok\",\"mensagem\":\"Digital verificada com sucesso.\"}"
                    : "{\"status\":\"falha\",\"mensagem\":\"Digital não reconhecida.\"}";

                File.WriteAllText("status.json", json);
                _capturador?.StopCapture();
                _logger.LogInformation("Resultado: {Resultado}", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar amostra biométrica");
            }
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {
            _logger.LogInformation("Dedo detectado no leitor.");
        }

        public void OnFingerGone(object Capture, string ReaderSerialNumber)
        {
            _logger.LogInformation("Dedo removido do leitor.");
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {
            _logger.LogInformation("Leitor conectado: {Serial}", ReaderSerialNumber);
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {
            _logger.LogWarning("Leitor desconectado: {Serial}", ReaderSerialNumber);
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, CaptureFeedback CaptureFeedback)
        {
            _logger.LogInformation("Qualidade da amostra: {Qualidade}", CaptureFeedback);
        }
    }
}
