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
            _notifyIcon.Text = "EPIFlow Agente Biométrico";

            var menu = new ContextMenuStrip();
           // menu.Items.Add("Mostrar janela", null, (s, e) => _form.MostrarJanela());
            menu.Items.Add("Iniciar captura", null, (s, e) => _form.StartCapture());
            menu.Items.Add("Cadastrar digital", null, (s, e) => _form.StartCapture(forRegistration: true));
            menu.Items.Add("Sair", null, (s, e) => Application.Exit());

            _notifyIcon.ContextMenuStrip = menu;
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}
