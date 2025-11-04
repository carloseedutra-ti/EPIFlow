using DPFP;
using DPFP.Capture;
using DPFP.Processing;
using DPFP.Verification;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPIFlow.Biometria.Agent.Models;

namespace EPIFlow.Biometria.Agent
{
    public partial class HiddenCaptureForm : Form, DPFP.Capture.EventHandler
    {
        private Capture _capturador;
        private Verification _verificador;
        private Enrollment _enrollment;
        private HttpAgent _http;
        private TrayIconManager _tray;
        private string _statusPath;
        private string _templatesDir;
        private bool _isCapturing;
        private bool _isRegistering;
        private bool _isTesting;
        private Label _labelStatus;
        private TextBox _logBox;
        private Button _retryButton;
        private string _colaboradorId;
        private string _colaboradorNome;
        private bool _capturaConcluida;
        private string _resultadoTemplate;
        private AgentApiClient _apiClient;
        private AgentPollingService _pollingService;
        private AgentTaskMessage _currentTask;

        public HiddenCaptureForm()
        {
            Text = "EPIFlow Agente Biometrico";
            Width = 500;
            Height = 420;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Visible = false;

            _statusPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "status.json");
            _templatesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EPIFlow", "Templates");
            Directory.CreateDirectory(_templatesDir);

            _labelStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Text = "Aguardando..."
            };

            _logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };

            _retryButton = new Button
            {
                Text = "Tentar novamente",
                Dock = DockStyle.Bottom,
                Height = 40,
                Visible = false,
                BackColor = Color.LightSteelBlue,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _retryButton.Click += (s, e) => RetryCapture();

            Controls.Add(_logBox);
            Controls.Add(_retryButton);
            Controls.Add(_labelStatus);

            Load += HiddenCaptureForm_Load;
            FormClosing += HiddenCaptureForm_Closing;
        }

        public bool IsBusy => _isCapturing || _isRegistering || _isTesting;

        private void HiddenCaptureForm_Load(object sender, EventArgs e)
        {
            _tray = new TrayIconManager(this);
            _tray.Initialize();

            BeginInvoke(new Action(() =>
            {
                InicializarLeitor();
                _http = new HttpAgent(this);
                _http.Start("http://localhost:5051/");
                SafeLog("Servidor HTTP iniciado em http://localhost:5051");
                Hide();
                InitializeRemoteServices();
            }));
        }

        private void HiddenCaptureForm_Closing(object sender, FormClosingEventArgs e)
        {
            _tray?.Dispose();
            _capturador?.StopCapture();
            _capturador.EventHandler = null;
            _capturador = null;
            _http?.Stop();
            _pollingService?.Dispose();
            _apiClient?.Dispose();
        }

        private void InitializeRemoteServices()
        {
            _pollingService?.Dispose();
            _pollingService = null;

            _apiClient?.Dispose();
            _apiClient = null;

            if (!AgentSettings.IsConfigured)
            {
                SafeLog("Configuracao do agente nao encontrada. Abra as configuracoes para informar a URL e a chave.");
                SafeSetStatus("Agente nao configurado.", true);
                return;
            }

            SafeSetStatus("Conectando ao EPIFlow...");
            _apiClient = new AgentApiClient();
            _pollingService = new AgentPollingService(this, _apiClient);
            Task.Run(new Func<Task>(InitializeRemoteAsync));
        }

        private async Task InitializeRemoteAsync()
        {
            try
            {
                var configuration = await _apiClient.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);
                if (configuration != null && configuration.Success)
                {
                    _pollingService.SetInterval(configuration.PollingIntervalSeconds);
                    _pollingService.Start();
                    SafeLog($"Polling ativo para {AgentSettings.BaseUri} (intervalo {configuration.PollingIntervalSeconds}s).");
                    SafeSetStatus("Aguardando solicitacoes do EPIFlow.");
                }
                else
                {
                    var message = configuration != null && !string.IsNullOrEmpty(configuration.Message)
                        ? configuration.Message
                        : "Falha ao conectar ao EPIFlow.";
                    SafeLog(message);
                    SafeSetStatus(message, true);
                }
            }
            catch (Exception ex)
            {
                SafeLog("Erro ao conectar ao EPIFlow: " + ex.Message);
                SafeSetStatus("Erro ao conectar ao EPIFlow.", true);
            }
        }

        public void ApplyConfigurationChange()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ApplyConfigurationChange));
                return;
            }

            SafeLog("Configuracao atualizada. Reiniciando conexao com o EPIFlow.");
            InitializeRemoteServices();
        }

        public void HandleRemoteTask(AgentTaskMessage task)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<AgentTaskMessage>(HandleRemoteTask), task);
                return;
            }

            if (IsBusy)
            {
                SafeLog("Ha uma captura em andamento. Ignorando solicitacao remota.");
                return;
            }

            _currentTask = task;
            _capturaConcluida = false;
            _resultadoTemplate = null;

            var identifier = task.ResolveEmployeeIdentifier();
            var operation = task.ResolveOperation();
            _isTesting = string.Equals(operation, "verify", StringComparison.OrdinalIgnoreCase);
            _isRegistering = !_isTesting;

            if (_isTesting && !EnsureVerificationTemplate(task))
            {
                SafeLog("Template de verificacao ausente. Informando falha ao servidor.");
                Task.Run(() => _apiClient.FailTaskAsync(task.TaskId, "Template nao encontrado no agente.", CancellationToken.None));
                _currentTask = null;
                _isTesting = false;
                return;
            }

            MostrarEAtivarJanela();
            if (_isTesting)
            {
                SafeSetStatus($"Teste de digital: posicione o dedo de {task.EmployeeName} ({task.FingerName}).");
                SafeLog($"Teste de digital solicitado: {task.EmployeeName} ({identifier}) - {task.FingerName}");
                StartCapture(false);
            }
            else
            {
                SafeSetStatus($"Capturando {task.FingerName} de {task.EmployeeName}");
                SafeLog($"Captura solicitada pelo EPIFlow: {task.EmployeeName} ({identifier}) - {task.FingerName}");
                StartCapture(true);
            }
        }

        private bool EnsureVerificationTemplate(AgentTaskMessage task)
        {
            var path = GetTemplateFilePath(task.EmployeeId, task.Finger);
            if (File.Exists(path))
            {
                return true;
            }

            var templateBase64 = task.ResolveTemplateBase64();
            if (string.IsNullOrWhiteSpace(templateBase64))
            {
                return false;
            }

            try
            {
                SaveTemplateToFile(task.EmployeeId, task.Finger, templateBase64);
                return true;
            }
            catch (Exception ex)
            {
                SafeLog("Erro ao salvar template para teste: " + ex.Message);
                return false;
            }
        }

        private string GetTemplateFilePath(Guid employeeId, int finger)
        {
            return Path.Combine(_templatesDir, $"{employeeId:D}_{finger}.dpft");
        }

        private void SaveTemplateToFile(Guid employeeId, int finger, string base64Template)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64Template);
                Directory.CreateDirectory(_templatesDir);
                File.WriteAllBytes(GetTemplateFilePath(employeeId, finger), bytes);
                SafeLog($"Template armazenado localmente para colaborador {employeeId}, dedo {finger}.");
            }
            catch (Exception ex)
            {
                SafeLog("Falha ao salvar template local: " + ex.Message);
            }
        }

        private Template LoadTemplateForCurrentTask()
        {
            if (_currentTask == null)
            {
                return null;
            }

            var path = GetTemplateFilePath(_currentTask.EmployeeId, _currentTask.Finger);
            try
            {
                if (File.Exists(path))
                {
                    return new Template(new MemoryStream(File.ReadAllBytes(path)));
                }

                var base64 = _currentTask.ResolveTemplateBase64();
                if (!string.IsNullOrWhiteSpace(base64))
                {
                    var bytes = Convert.FromBase64String(base64);
                    Directory.CreateDirectory(_templatesDir);
                    File.WriteAllBytes(path, bytes);
                    return new Template(new MemoryStream(bytes));
                }
            }
            catch (Exception ex)
            {
                SafeLog("Erro ao carregar template de verificacao: " + ex.Message);
            }

            return null;
        }

        private void InicializarLeitor()
        {
            try
            {
                var readers = new ReadersCollection();
                readers.Refresh();

                if (readers.Count == 0)
                {
                    SafeLog("Nenhum leitor encontrado.");
                    SafeSetStatus("Nenhum leitor encontrado.");
                    return;
                }

                var selected = readers.First().Value;
                _capturador = new Capture(selected.SerialNumber, Priority.Normal);
                _capturador.EventHandler = this;
                _verificador = new Verification();
                _enrollment = new Enrollment();

                SafeLog($"Leitor inicializado: {selected.ProductName} ({selected.SerialNumber})");
                SafeSetStatus("Leitor pronto.");
            }
            catch (Exception ex)
            {
                SafeLog("Erro ao inicializar leitor: " + ex.Message);
                SafeSetStatus("Erro ao inicializar leitor.");
            }
        }

        public void StartCapture(bool forRegistration)
        {
            try
            {
                if (_capturador != null)
                {
                    try { _capturador.StopCapture(); } catch { }
                    _capturador.EventHandler = null;
                    _capturador = null;
                }

                var readers = new ReadersCollection();
                readers.Refresh();

                if (readers.Count == 0)
                {
                    SafeLog("Nenhum leitor encontrado para iniciar captura.");
                    SafeSetStatus("Nenhum leitor encontrado.");
                    return;
                }

                var selected = readers.First().Value;
                _capturador = new Capture(selected.SerialNumber, Priority.Normal);
                _capturador.EventHandler = this;

            _capturaConcluida = false;
            _resultadoTemplate = null;
            _isRegistering = forRegistration;
            _isTesting = !forRegistration && _currentTask != null;

            MostrarEAtivarJanela();

            _capturador.StartCapture();
            _isCapturing = true;

            if (forRegistration)
            {
                SafeSetStatus("Capturando digital para cadastro...");
                SafeLog("Modo cadastro iniciado.");
            }
            else if (_isTesting)
            {
                SafeSetStatus("Teste de digital: posicione o dedo para validar.");
                SafeLog("Modo teste iniciado (verificacao contra template armazenado).");
            }
            else
            {
                SafeSetStatus("Aguardando dedo no leitor...");
                SafeLog("Modo verificacao iniciado.");
            }
            }
            catch (Exception ex)
            {
                SafeLog("Erro ao iniciar captura: " + ex.Message);
                SafeSetStatus("Erro ao iniciar captura.");
            }
        }

        public void StopCapture()
        {
            try { _capturador?.StopCapture(); } catch { }
            _isCapturing = false;
            SafeLog("Captura encerrada.");
        }

        public void OnComplete(object Capture, string ReaderSerialNumber, Sample sample)
        {
            try
            {
                if (_isRegistering)
                {
                    RegistrarDigital(sample);
                }
                else
                {
                    VerificarDigital(sample);
                }
            }
            catch (Exception ex)
            {
                SafeLog("Erro OnComplete: " + ex.Message);
                SafeSetStatus("Erro na leitura.");
                MostrarBotaoRetry();
            }
        }

        private void RegistrarDigital(Sample sample)
        {
            try
            {
                var extractor = new FeatureExtraction();
                var feedback = CaptureFeedback.None;
                var features = new FeatureSet();
                extractor.CreateFeatureSet(sample, DataPurpose.Enrollment, ref feedback, ref features);

                if (feedback != CaptureFeedback.Good)
                {
                    SafeLog("Amostra ruim, tente novamente.");
                    SafeSetStatus("Amostra ruim, tente novamente.");
                    MostrarBotaoRetry();
                    return;
                }

                _enrollment.AddFeatures(features);
                SafeLog($"Amostra recebida. Restantes: {_enrollment.FeaturesNeeded}.");

                if (_enrollment.TemplateStatus == Enrollment.Status.Ready)
                {
                    byte[] templateBytes;
                    using (var ms = new MemoryStream())
                    {
                        _enrollment.Template.Serialize(ms);
                        templateBytes = ms.ToArray();
                    }

                    var base64Template = Convert.ToBase64String(templateBytes);

                    if (_currentTask != null && _apiClient != null)
                    {
                        var taskSnapshot = _currentTask;
                        Task.Run(async () =>
                        {
                            try
                            {
                                await _apiClient.CompleteTaskAsync(taskSnapshot.TaskId, base64Template, CancellationToken.None);
                                SaveTemplateToFile(taskSnapshot.EmployeeId, taskSnapshot.Finger, base64Template);
                                SafeSetStatus("Digital enviada com sucesso!");
                                SafeLog($"Template de {_colaboradorNome} enviado ao EPIFlow (tarefa {taskSnapshot.TaskId}).");
                                FinalizarCaptura(base64Template);
                            }
                            catch (Exception ex)
                            {
                                SafeLog("Erro ao enviar template ao EPIFlow: " + ex.Message);
                                SafeSetStatus("Erro ao enviar template.", true);
                                MostrarBotaoRetry();
                            }
                            finally
                            {
                                _currentTask = null;
                            }
                        });
                    }
                    else
                    {
                        FinalizarCaptura(base64Template);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog("Erro ao enviar template: " + ex.Message);
                SafeSetStatus("Erro ao enviar template.");
                MostrarBotaoRetry();
            }
        }

        private void VerificarDigital(Sample sample)
        {
            var extractor = new FeatureExtraction();
            var feedback = CaptureFeedback.None;
            var features = new FeatureSet();
            extractor.CreateFeatureSet(sample, DataPurpose.Verification, ref feedback, ref features);

            if (feedback != CaptureFeedback.Good)
            {
                SafeSetStatus("Amostra ruim.");
                SafeLog("Amostra ruim.");
                MostrarBotaoRetry();
                return;
            }

            if (_isTesting && _currentTask != null)
            {
                var template = LoadTemplateForCurrentTask();
                if (template == null)
                {
                    SafeSetStatus("Template nao encontrado.", true);
                    SafeLog("Nao foi possivel localizar template para verificacao.");
                    if (_apiClient != null)
                    {
                        Task.Run(() => _apiClient.FailTaskAsync(_currentTask.TaskId, "Template nao encontrado no agente.", CancellationToken.None));
                    }
                    _currentTask = null;
                    MostrarBotaoRetry();
                    return;
                }

                var result = new Verification.Result();
                _verificador.Verify(features, template, ref result);

                if (result.Verified)
                {
                    SafeSetStatus("Digital validada com sucesso!");
                    SafeLog($"Teste reconheceu {_colaboradorNome}.");
                    FinalizarVerificacao(true);
                }
                else
                {
                    SafeSetStatus("Digital nao confere.", true);
                    SafeLog("Teste de verificacao falhou.");
                    FinalizarVerificacao(false);
                }

                return;
            }

            bool reconhecida = false;

            foreach (var file in Directory.GetFiles(_templatesDir, "*.dpft"))
            {
                try
                {
                    var template = new Template(new MemoryStream(File.ReadAllBytes(file)));
                    var result = new Verification.Result();
                    _verificador.Verify(features, template, ref result);

                    if (result.Verified)
                    {
                        reconhecida = true;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    SafeLog("Erro ao verificar template local: " + ex.Message);
                }
            }

            if (reconhecida)
            {
                SafeSetStatus("Digital verificada com sucesso!");
                SafeLog($"Digital reconhecida: {_colaboradorNome}");
                StopCapture();
                _capturador.EventHandler = null;
                _capturador = null;
                OcultarJanelaApos(3000);
            }
            else
            {
                SafeSetStatus("Digital nao reconhecida.", true);
                SafeLog("Digital nao encontrada. Exibindo botao de repeticao.");
                MostrarBotaoRetry();
                StopCapture();
                _capturador.EventHandler = null;
                _capturador = null;
            }
        }

        private void FinalizarVerificacao(bool sucesso)
        {
            StopCapture();
            if (_capturador != null)
            {
                _capturador.EventHandler = null;
                _capturador = null;
            }

            if (_currentTask != null && _apiClient != null)
            {
                var taskId = _currentTask.TaskId;
                if (sucesso)
                {
                    Task.Run(() => _apiClient.CompleteTaskAsync(taskId, string.Empty, CancellationToken.None));
                }
                else
                {
                    Task.Run(() => _apiClient.FailTaskAsync(taskId, "Digital nao corresponde ao template armazenado.", CancellationToken.None));
                }
            }

            _currentTask = null;
            _isTesting = false;
            OcultarJanelaApos(3000);
        }

        private void FinalizarCaptura(string base64Template)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(FinalizarCaptura), base64Template);
                return;
            }

            _capturaConcluida = true;
            _resultadoTemplate = base64Template;
            _isRegistering = false;
            _enrollment?.Clear();
            StopCapture();
            if (_capturador != null)
            {
                _capturador.EventHandler = null;
                _capturador = null;
            }

            _currentTask = null;
            OcultarJanelaApos(3000);
        }

        private void MostrarBotaoRetry()
        {
            if (InvokeRequired) { BeginInvoke(new Action(MostrarBotaoRetry)); return; }

            _retryButton.Visible = true;
            _retryButton.Enabled = true;
            SafeLog("Botao Tentar novamente exibido.");

            var timer = new System.Windows.Forms.Timer { Interval = 10000 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (_retryButton.Visible)
                {
                    _retryButton.Visible = false;
                    SafeLog("Botao ocultado automaticamente apos timeout.");
                    OcultarJanelaApos(500);
                }
            };
            timer.Start();
        }

        private void RetryCapture()
        {
            if (InvokeRequired) { BeginInvoke(new Action(RetryCapture)); return; }

            _retryButton.Visible = false;
            SafeLog("Usuario optou por tentar novamente.");
            StartCapture(_isRegistering);
        }

        private void MostrarEAtivarJanela()
        {
            if (InvokeRequired) { BeginInvoke(new Action(MostrarEAtivarJanela)); return; }

            if (!Visible) Show();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            TopMost = true;
            BringToFront();
            Focus();

            var timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (s, e) =>
            {
                TopMost = false;
                timer.Stop();
            };
            timer.Start();
        }

        private void OcultarJanelaApos(int milissegundos)
        {
            new Thread(() =>
            {
                Thread.Sleep(milissegundos);
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _retryButton.Visible = false;
                        TopMost = false;
                        WindowState = FormWindowState.Minimized;
                        Hide();
                        SafeLog("Janela ocultada apos timeout.");
                    }));
                }
            })
            { IsBackground = true }.Start();
        }

        public void PrepararCaptura(string nome, string id)
        {
            Invoke(new Action(() =>
            {
                _colaboradorId = id;
                _colaboradorNome = nome;
                _capturaConcluida = false;
                _resultadoTemplate = null;

                MostrarEAtivarJanela();
                SafeSetStatus($"Capturando digital de: {_colaboradorNome}");
                _isRegistering = true;
                _isTesting = false;
                StartCapture(true);
            }));
        }

        public bool CapturaConcluida => _capturaConcluida;

        public object ObterResultadoCaptura()
        {
            if (!_capturaConcluida)
            {
                return new { status = "erro", mensagem = "Tempo limite atingido sem captura." };
            }

            return new
            {
                status = "ok",
                mensagem = "Digital capturada com sucesso.",
                colaboradorId = _colaboradorId
            };
        }

        private void SafeSetStatus(string text, bool erro = false)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SafeSetStatus(text, erro))); return; }
            _labelStatus.Text = text;
            _labelStatus.ForeColor = erro ? Color.Red : Color.DarkBlue;
        }

        public void SafeLog(string message)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SafeLog(message))); return; }
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber) => SafeLog("Dedo detectado.");
        public void OnFingerGone(object Capture, string ReaderSerialNumber) => SafeLog("Dedo removido.");
        public void OnReaderConnect(object Capture, string ReaderSerialNumber) => SafeLog("Leitor conectado.");
        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber) => SafeLog("Leitor desconectado.");
        public void OnSampleQuality(object Capture, string ReaderSerialNumber, CaptureFeedback feedback) => SafeLog("Qualidade: " + feedback);
    }
}


