using System;

namespace EPIFlow.Biometria.Agent
{
    internal static class AgentSettings
    {
        private const string DefaultBaseUrl = "https://localhost:5001/";

        public static Uri BaseUri
        {
            get
            {
                var baseUrl = Properties.Settings.Default.AgentBaseUrl;
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    baseUrl = DefaultBaseUrl;
                }

                if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
                {
                    baseUrl += "/";
                }

                return new Uri(baseUrl, UriKind.Absolute);
            }
        }

        public static Guid ApiKey
        {
            get
            {
                Guid value;
                return Guid.TryParse(Properties.Settings.Default.AgentApiKey, out value) ? value : Guid.Empty;
            }
        }

        public static bool IsConfigured
        {
            get
            {
                return ApiKey != Guid.Empty && !string.IsNullOrWhiteSpace(Properties.Settings.Default.AgentBaseUrl);
            }
        }

        public static void Save(string baseUrl, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = DefaultBaseUrl;
            }

            Properties.Settings.Default.AgentBaseUrl = baseUrl.Trim();
            Properties.Settings.Default.AgentApiKey = apiKey == null ? string.Empty : apiKey.Trim();
            Properties.Settings.Default.Save();
        }
    }
}
