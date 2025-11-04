using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EPIFlow.Biometria.Agent
{
    public partial class ConfigurationForm : Form
    {
        public ConfigurationForm()
        {
            InitializeComponent();
            txtBaseUrl.Text = Properties.Settings.Default.AgentBaseUrl;
            txtApiKey.Text = Properties.Settings.Default.AgentApiKey;
            lblStatus.Text = string.Empty;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateFields())
            {
                return;
            }

            AgentSettings.Save(txtBaseUrl.Text, txtApiKey.Text);
            DialogResult = DialogResult.OK;
            Close();
        }

        private async void btnTest_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Testando conexão...";
            lblStatus.ForeColor = System.Drawing.Color.DarkBlue;
            btnTest.Enabled = false;

            try
            {
                if (!ValidateFields())
                {
                    return;
                }

                string baseUrl = NormalizeBaseUrl(txtBaseUrl.Text);
                Guid apiKey;
                Guid.TryParse(txtApiKey.Text, out apiKey);

                if (apiKey == Guid.Empty)
                {
                    lblStatus.Text = "Informe uma chave de agente válida.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    return;
                }

                var tester = new AgentConfigurationTester(baseUrl, apiKey);
                var response = await tester.ExecuteAsync().ConfigureAwait(true);

                if (response.Success)
                {
                    lblStatus.Text = string.Format("Conectado. Intervalo atribuído: {0}s.", response.PollingIntervalSeconds);
                    lblStatus.ForeColor = System.Drawing.Color.DarkGreen;
                }
                else
                {
                    
                    lblStatus.Text = response.Message ?? "Conexão recusada.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Falha ao testar: " + ex.Message;
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            finally
            {
                btnTest.Enabled = true;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(txtBaseUrl.Text))
            {
                lblStatus.Text = "Informe a URL do EPIFlow.";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                txtBaseUrl.Focus();
                return false;
            }

            Guid tmp;
            if (!Guid.TryParse(txtApiKey.Text, out tmp))
            {
                lblStatus.Text = "Informe uma chave de agente válida.";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                txtApiKey.Focus();
                return false;
            }

            lblStatus.Text = string.Empty;
            return true;
        }

        private static string NormalizeBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "https://localhost:5001/";
            }

            url = url.Trim();
            if (!url.EndsWith("/", StringComparison.Ordinal))
            {
                url += "/";
            }

            return url;
        }
    }
}
