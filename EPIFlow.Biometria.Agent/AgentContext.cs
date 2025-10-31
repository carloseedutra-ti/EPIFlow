using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EPIFlow.Biometria.Agent
{
    public class AgentContext : ApplicationContext
    {
        private readonly HiddenCaptureForm _form;
        private readonly HttpAgent _http;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _menu;

        public AgentContext()
        {
            // Cria o formulário invisível responsável pela captura
            _form = new HiddenCaptureForm();
            _http = new HttpAgent(_form);
            _http.Start("http://localhost:5051/");

            // Cria o menu da bandeja
            _menu = new ContextMenuStrip();
            _menu.Items.Add("Mostrar janela", null, MostrarJanela_Click);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Sair", null, Sair_Click);

            // Cria o ícone da bandeja
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // você pode trocar por um ícone do EPIFlow
                ContextMenuStrip = _menu,
                Text = "EPIFlow Biometria Agent",
                Visible = true
            };

            _trayIcon.DoubleClick += MostrarJanela_Click;

            // Mostra dica inicial
            _trayIcon.ShowBalloonTip(
                3000,
                "EPIFlow Biometria Agent",
                "Agente biométrico iniciado e escutando em http://localhost:5051",
                ToolTipIcon.Info
            );
        }

        private void MostrarJanela_Click(object sender, EventArgs e)
        {
            try
            {
                if (_form == null || _form.IsDisposed)
                {
                    MessageBox.Show("A janela de captura não está disponível.", "EPIFlow Agent");
                    return;
                }

                _form.Show();
                _form.WindowState = FormWindowState.Normal;
                _form.Opacity = 1; // deixa visível para debug
                _form.TopMost = true;
                _form.BringToFront();
                _form.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao exibir a janela: " + ex.Message, "EPIFlow Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Sair_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Deseja realmente encerrar o agente biométrico?",
                                "EPIFlow Agent",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ExitThread();
            }
        }

        protected override void ExitThreadCore()
        {
            try { _trayIcon.Visible = false; } catch { }
            try { _http?.Stop(); } catch { }
            try { _form?.Dispose(); } catch { }
            base.ExitThreadCore();
        }
    }
}
