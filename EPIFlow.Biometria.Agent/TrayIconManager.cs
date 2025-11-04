using System;
using System.Drawing;
using System.Windows.Forms;

namespace EPIFlow.Biometria.Agent
{
    public class TrayIconManager : IDisposable
    {
        private readonly HiddenCaptureForm _form;
        private NotifyIcon _notifyIcon;

        public TrayIconManager(HiddenCaptureForm form)
        {
            _form = form;
            _notifyIcon = new NotifyIcon();
        }

        public void Initialize()
        {
            _notifyIcon.Icon = SystemIcons.Information;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "EPIFlow Agente Biom\u00E9trico";

            var menu = new ContextMenuStrip();
            menu.Items.Add("Cadastrar digital", null, (s, e) => _form.StartCapture(true));
            menu.Items.Add("Testar digital", null, (s, e) => _form.StartCapture(false));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Configura\u00E7\u00F5es...", null, (s, e) => OpenConfigurationDialog());
            menu.Items.Add("Sair", null, (s, e) => Application.Exit());

            _notifyIcon.ContextMenuStrip = menu;
        }

        private void OpenConfigurationDialog()
        {
            using (var dialog = new ConfigurationForm())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _form.ApplyConfigurationChange();
                }
            }
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
