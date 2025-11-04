using System;
using System.Threading;
using System.Threading.Tasks;

namespace EPIFlow.Biometria.Agent
{
    internal sealed class AgentPollingService : IDisposable
    {
        private readonly HiddenCaptureForm _form;
        private readonly AgentApiClient _apiClient;
        private readonly Timer _timer;
        private TimeSpan _currentInterval;
        private bool _isProcessing;

        public AgentPollingService(HiddenCaptureForm form, AgentApiClient apiClient)
        {
            _form = form;
            _apiClient = apiClient;
            _currentInterval = TimeSpan.FromSeconds(5);
            _timer = new Timer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void Start()
        {
            _timer.Change(TimeSpan.Zero, _currentInterval);
        }

        public void SetInterval(int seconds)
        {
            if (seconds < 3)
            {
                seconds = 3;
            }

            _currentInterval = TimeSpan.FromSeconds(seconds);
            _timer.Change(TimeSpan.Zero, _currentInterval);
        }

        private async void OnTimer(object state)
        {
            if (_isProcessing || _form.IsBusy)
            {
                return;
            }

            _isProcessing = true;
            try
            {
                var task = await _apiClient.PollAsync(CancellationToken.None).ConfigureAwait(false);
                if (task != null)
                {
                    _form.HandleRemoteTask(task);
                }
            }
            catch (Exception ex)
            {
                _form.SafeLog("Erro ao consultar solicitações do EPIFlow: " + ex.Message);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
