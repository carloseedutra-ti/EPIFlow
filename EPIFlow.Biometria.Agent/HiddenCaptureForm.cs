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
        private bool _isCapturing = false;
        private bool _isRegistering = false;
        private Label lblStatus;
        private TextBox txtLog;
        private Button btnRetry;
        private string _colaboradorId;
        private string _colaboradorNome;
        private bool _capturaConcluida = false;
        private string _resultadoTemplate = null;
        private string _endpointEpiFlow = "http://localhost:5000/api/biometria/receber";

        public HiddenCaptureForm()
        {
            Text = "EPIFlow Agente Biométrico";
            Width = 500;
            Height = 420;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Visible = false;

            _statusPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "status.json");
            _templatesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EPIFlow", "Templates");
            Directory.CreateDirectory(_templatesDir);

            lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Text = "Aguardando..."
            };

            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };

            btnRetry = new Button
            {
                Text = "🔁 Tentar novamente",
                Dock = DockStyle.Bottom,
                Height = 40,
                Visible = false,
                BackColor = Color.LightSteelBlue,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnRetry.Click += (s, e) => TentarNovamente();

            Controls.Add(txtLog);
            Controls.Add(btnRetry);
            Controls.Add(lblStatus);

            Load += HiddenCaptureForm_Load;
            FormClosing += HiddenCaptureForm_Closing;
        }

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
            }));
        }

        private void HiddenCaptureForm_Closing(object sender, FormClosingEventArgs e)
        {
            _tray?.Dispose();
            _capturador?.StopCapture();
            _capturador.EventHandler = null;
            _capturador = null;
            _http?.Stop();
        }

        // --- BOTÃO DE REPETIÇÃO ---
        private void MostrarBotaoRetry()
        {
            if (InvokeRequired) { BeginInvoke(new Action(MostrarBotaoRetry)); return; }

            btnRetry.Visible = true;
            btnRetry.Enabled = true;
            SafeLog("Botão 'Tentar novamente' exibido.");

            var t = new System.Windows.Forms.Timer { Interval = 10000 }; // 10 segundos
            t.Tick += (s, e) =>
            {
                t.Stop();
                if (btnRetry.Visible)
                {
                    btnRetry.Visible = false;
                    SafeLog("Botão ocultado automaticamente após timeout.");
                    OcultarJanelaApos(500);
                }
            };
            t.Start();
        }

        private void TentarNovamente()
        {
            if (InvokeRequired) { BeginInvoke(new Action(TentarNovamente)); return; }

            btnRetry.Visible = false;
            SafeLog("Usuário clicou em 'Tentar novamente'.");
            StartCapture(_isRegistering);
        }

        // --- LEITOR ---
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

        public void StartCapture(bool forRegistration = false)
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
                    SafeLog("Nenhum leitor encontrado para reinicializar.");
                    SafeSetStatus("Nenhum leitor encontrado.");
                    return;
                }

                var selected = readers.First().Value;
                _capturador = new Capture(selected.SerialNumber, Priority.Normal);
                _capturador.EventHandler = this;

                MostrarEAtivarJanela();

                _isRegistering = forRegistration;
                _capturador.StartCapture();
                _isCapturing = true;

                SafeSetStatus(forRegistration ? "Capturando digital para cadastro..." : "Aguardando dedo no leitor...");
                SafeLog(forRegistration ? "Modo cadastro iniciado (reinstanciado)." : "Captura iniciada (reinstanciado).");
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

        // --- EVENTOS DPFP ---
        public void OnComplete(object Capture, string ReaderSerialNumber, Sample Sample)
        {
            try
            {
                if (_isRegistering)
                    RegistrarDigital(Sample);
                else
                    VerificarDigital(Sample);
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
            // (sem alterações — igual ao seu)
            // mantém o envio e o OcultarJanelaApos(3000)
            // se ocorrer exceção, mostra botão de retry
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
                SafeLog($"Amostra {_enrollment.FeaturesNeeded} restante(s)...");

                if (_enrollment.TemplateStatus == Enrollment.Status.Ready)
                {
                    byte[] templateBytes;
                    using (var ms = new MemoryStream())
                    {
                        _enrollment.Template.Serialize(ms);
                        templateBytes = ms.ToArray();
                    }

                    var base64Template = Convert.ToBase64String(templateBytes);
                    var json = JsonConvert.SerializeObject(new
                    {
                        colaboradorId = _colaboradorId,
                        nome = _colaboradorNome,
                        templateBase64 = base64Template
                    });

                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        var resp = client.PostAsync(_endpointEpiFlow, content).Result;

                        if (resp.IsSuccessStatusCode)
                        {
                            SafeSetStatus("✅ Digital enviada com sucesso!");
                            SafeLog($"Template de {_colaboradorNome} enviado ao servidor EPIFlow.");
                        }
                        else
                        {
                            SafeSetStatus("⚠️ Erro ao enviar template.");
                            SafeLog("Falha ao enviar template: " + resp.StatusCode);
                            MostrarBotaoRetry();
                        }
                    }

                    _capturaConcluida = true;
                    _resultadoTemplate = base64Template;
                    _isRegistering = false;
                    _enrollment.Clear();
                    StopCapture();
                    _capturador.EventHandler = null;
                    _capturador = null;
                    OcultarJanelaApos(3000);
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

            bool reconhecida = false;

            foreach (var file in Directory.GetFiles(_templatesDir, "*.dpft"))
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

            if (reconhecida)
            {
                SafeSetStatus("✅ Digital verificada com sucesso!");
                SafeLog($"Digital reconhecida: {_colaboradorNome}");
                StopCapture();
                _capturador.EventHandler = null;
                _capturador = null;
                OcultarJanelaApos(3000);
            }
            else
            {
                SafeSetStatus("❌ Digital não reconhecida.", true);
                SafeLog("Digital não encontrada. Exibindo botão de repetição.");
                MostrarBotaoRetry();
                StopCapture();
                _capturador.EventHandler = null;
                _capturador = null;
            }
        }

        // --- UI helpers ---
        private void SafeSetStatus(string text, bool erro = false)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SafeSetStatus(text, erro))); return; }

            lblStatus.Text = text;
            lblStatus.ForeColor = erro ? Color.Red : Color.DarkBlue;
        }

        public void SafeLog(string msg)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SafeLog(msg))); return; }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }

        public void MostrarEAtivarJanela()
        {
            if (InvokeRequired) { BeginInvoke(new Action(MostrarEAtivarJanela)); return; }

            if (!Visible) Show();
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;

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
                        btnRetry.Visible = false;
                        TopMost = false;
                        WindowState = FormWindowState.Minimized;
                        Hide();
                        SafeLog("Janela ocultada após timeout.");
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
                lblStatus.Text = $"Capturando digital de: {_colaboradorNome}";
                _isRegistering = true;
                StartCapture(true);
            }));
        }

        public bool CapturaConcluida => _capturaConcluida;

        public object ObterResultadoCaptura()
        {
            if (!_capturaConcluida)
                return new { status = "erro", mensagem = "Tempo limite atingido sem captura." };

            return new
            {
                status = "ok",
                mensagem = "Digital capturada com sucesso.",
                colaboradorId = _colaboradorId
            };
        }

        // Eventos do SDK
        public void OnFingerTouch(object Capture, string ReaderSerialNumber) => SafeLog("Dedo detectado.");
        public void OnFingerGone(object Capture, string ReaderSerialNumber) => SafeLog("Dedo removido.");
        public void OnReaderConnect(object Capture, string ReaderSerialNumber) => SafeLog("Leitor conectado.");
        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber) => SafeLog("Leitor desconectado!");
        public void OnSampleQuality(object Capture, string ReaderSerialNumber, CaptureFeedback feedback) => SafeLog("Qualidade: " + feedback);
    }
}
