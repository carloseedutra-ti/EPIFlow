using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace EPIFlow.Biometria.Agent
{
    public class HttpAgent
    {
        private readonly HiddenCaptureForm _form;
        private HttpListener _listener;
        private Thread _thread;
        private volatile bool _running = false;

        public HttpAgent(HiddenCaptureForm form)
        {
            _form = form;
            _listener = new HttpListener();
        }

        public void Start(string prefix)
        {
            if (_running) return;

            _listener.Prefixes.Clear();
            _listener.Prefixes.Add(prefix); // ex.: http://localhost:5051/
            _listener.Start();
            _running = true;

            _thread = new Thread(Loop) { IsBackground = true };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
        }

        private void Loop()
        {
            while (_running)
            {
                HttpListenerContext ctx = null;
                try { ctx = _listener.GetContext(); }
                catch { continue; }

                try { Handle(ctx); }
                catch (Exception ex)
                {
                    _form.SafeLog("Erro interno no agente HTTP: " + ex.Message);
                    TryWrite(ctx, 500, "{\"status\":\"erro\",\"mensagem\":\"Falha interna no agente.\"}");
                }
            }
        }

        private void Handle(HttpListenerContext ctx)
        {
            var path = ctx.Request.Url.AbsolutePath.ToLowerInvariant();

            // --- GET /api/biometria/iniciar
            if (path == "/api/biometria/iniciar")
            {
                _form.BeginInvoke(new Action(() => _form.StartCapture()));
                TryWrite(ctx, 200, "{\"status\":\"ok\",\"mensagem\":\"Leitura iniciada, posicione o dedo.\"}");
                return;
            }

            // --- GET /api/biometria/status
            if (path == "/api/biometria/status")
            {
                var statusFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "status.json");
                var json = File.Exists(statusFile)
                    ? File.ReadAllText(statusFile)
                    : "{\"status\":\"idle\",\"mensagem\":\"Aguardando leitura.\"}";
                TryWrite(ctx, 200, json);
                return;
            }

            // --- POST /api/biometria/capturar
            if (path == "/api/biometria/capturar" && string.Equals(ctx.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string body;
                    using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    var data = JsonConvert.DeserializeObject<CapturaRequest>(body);
                    if (data == null || string.IsNullOrWhiteSpace(data.ColaboradorId) || string.IsNullOrWhiteSpace(data.Nome))
                    {
                        TryWrite(ctx, 400, "{\"status\":\"erro\",\"mensagem\":\"Corpo da requisição inválido.\"}");
                        return;
                    }

                    _form.SafeLog("📥 Pedido de captura recebido: " + data.Nome + " (" + data.ColaboradorId + ")");
                    _form.PrepararCaptura(data.Nome, data.ColaboradorId);

                    // Aguarda captura concluir (máx 25s)
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (!_form.CapturaConcluida && sw.ElapsedMilliseconds < 25000)
                        Thread.Sleep(200);

                    var resultado = _form.ObterResultadoCaptura();
                    var jsonResp = JsonConvert.SerializeObject(resultado);
                    TryWrite(ctx, 200, jsonResp);
                }
                catch (Exception ex)
                {
                    _form.SafeLog("Erro no endpoint /capturar: " + ex.Message);
                    TryWrite(ctx, 500, "{\"status\":\"erro\",\"mensagem\":\"Falha ao processar captura.\"}");
                }
                return;
            }

            // --- POST /api/biometria/template (opcional)
            if (path == "/api/biometria/template" && string.Equals(ctx.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                string base64;
                using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                {
                    base64 = reader.ReadToEnd();
                }

                try
                {
                    var bytes = Convert.FromBase64String(base64);
                    var tplPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template_colaborador.dpft");
                    File.WriteAllBytes(tplPath, bytes);
                    TryWrite(ctx, 200, "{\"status\":\"ok\",\"mensagem\":\"Template atualizado no agente.\"}");
                }
                catch
                {
                    TryWrite(ctx, 400, "{\"status\":\"erro\",\"mensagem\":\"Base64 inválido.\"}");
                }
                return;
            }

            // --- 404 padrão
            TryWrite(ctx, 404, "{\"status\":\"erro\",\"mensagem\":\"Endpoint não encontrado.\"}");
        }

        private static void TryWrite(HttpListenerContext ctx, int status, string body)
        {
            try
            {
                ctx.Response.StatusCode = status;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                var data = Encoding.UTF8.GetBytes(body);
                ctx.Response.OutputStream.Write(data, 0, data.Length);
            }
            catch { }
            finally
            {
                try { ctx.Response.OutputStream.Close(); } catch { }
            }
        }
    }
}
