using System;
using System.Drawing;
using System.Windows.Forms;

namespace EPIFlow.Biometria.Agent
{
    public class AgentContext : ApplicationContext
    {
        private HiddenCaptureForm _form;
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _menu;

        public AgentContext()
        {
            if (!EnsureConfiguration())
            {
                ExitThread();
                return;
            }

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _form = new HiddenCaptureForm();

            _menu = new ContextMenuStrip();
            _menu.Items.Add("Configurações", null, Configuracoes_Click);
            _menu.Items.Add("Mostrar janela", null, MostrarJanela_Click);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Sair", null, Sair_Click);

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = _menu,
                Text = "EPIFlow Biometria Agent",
                Visible = true
            };

            _trayIcon.DoubleClick += MostrarJanela_Click;
            _trayIcon.ShowBalloonTip(3000, "EPIFlow Biometria Agent", "Agente biométrico em execução.", ToolTipIcon.Info);
        }

        private bool EnsureConfiguration()
        {
            while (!AgentSettings.IsConfigured)
            {
                using (var dialog = new ConfigurationForm())
                {
                    var result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void Configuracoes_Click(object sender, EventArgs e)
        {
            using (var dialog = new ConfigurationForm())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (_form != null && !_form.IsDisposed)
                    {
                        _form.ApplyConfigurationChange();
                    }
                }
            }
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
                _form.Opacity = 1;
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
            if (MessageBox.Show("Deseja realmente encerrar o agente biométrico?", "EPIFlow Agent", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ExitThread();
            }
        }

        protected override void ExitThreadCore()
        {
            try { if (_trayIcon != null) _trayIcon.Visible = false; } catch { }
            try { if (_form != null && !_form.IsDisposed) _form.Dispose(); } catch { }
            base.ExitThreadCore();
        }
    }
}
