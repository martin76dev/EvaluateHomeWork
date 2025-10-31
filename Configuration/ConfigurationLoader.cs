using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using EvaluationHW.Application.Configuration;

namespace EvaluationHW.Application.Configuration
{
    public static class ConfigurationLoader
    {
        /// <summary>
        /// Lee y parsea el archivo user-secrets.json si existe, devolviendo el JsonNode o null.
        /// </summary>
        private static JsonNode? LoadUserSecretsJson()
        {
            try
            {
                string cwd = Directory.GetCurrentDirectory();
                var csproj = Directory.GetFiles(cwd, "*.csproj").FirstOrDefault();
                if (csproj != null)
                {
                    var csprojText = File.ReadAllText(csproj);
                    var startTag = "<UserSecretsId>";
                    var endTag = "</UserSecretsId>";
                    var si = csprojText.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
                    if (si >= 0)
                    {
                        var ei = csprojText.IndexOf(endTag, si, StringComparison.OrdinalIgnoreCase);
                        if (ei > si)
                        {
                            var id = csprojText.Substring(si + startTag.Length, ei - (si + startTag.Length)).Trim();
                            var secretsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", id, "secrets.json");
                            if (File.Exists(secretsPath))
                            {
                                var sjson = File.ReadAllText(secretsPath);
                                return JsonNode.Parse(sjson);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to read user-secrets: {ex.Message}");
            }
            return null;
        }

        public static ChatGPTSettings LoadChatGPTSettings()
        {
            string cwd = Directory.GetCurrentDirectory();
            ChatGPTSettings settings = new ChatGPTSettings();

            // 1. Variables de entorno
            var envApiKey = Environment.GetEnvironmentVariable("OpenAI__ApiKey") ?? Environment.GetEnvironmentVariable("OPENAI__APIKEY");
            if (!string.IsNullOrEmpty(envApiKey)) settings.ApiKey = envApiKey;

            // 2. User-secrets
            try
            {
                var sdoc = LoadUserSecretsJson();
                if (sdoc != null)
                {
                    // Support both flat and nested formats
                    // 1. Flat: { "OpenAI:ApiKey": "..." }
                    // 2. Nested: { "OpenAI": { "ApiKey": "..." } }
                    settings.ApiKey ??= sdoc?["OpenAI:ApiKey"]?.GetValue<string>();
                    if (settings.ApiKey == null)
                    {
                        var oai = sdoc?["OpenAI"];
                        if (oai != null)
                        {
                            settings.ApiKey = oai["ApiKey"]?.GetValue<string>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to read user-secrets for OpenAI: {ex.Message}");
            }

            return settings;
        }

        public static GoogleOAuthSettings LoadGoogleOAuthSettings()
        {
            string cwd = Directory.GetCurrentDirectory();
            string appsettingsPath = Path.Combine(cwd, "appsettings.json");
            GoogleOAuthSettings settings = new GoogleOAuthSettings();

            // 1. appsettings.json (solo datos públicos)
            if (File.Exists(appsettingsPath))
            {
                try
                {
                    var json = File.ReadAllText(appsettingsPath);
                    var doc = JsonNode.Parse(json);
                    var google = doc?["GoogleOAuth"];
                    if (google != null)
                    {
                        settings.ClientId = google["ClientId"]?.GetValue<string>();
                        settings.RedirectUri = google["RedirectUri"]?.GetValue<string>();
                        if (google["Scopes"] is JsonArray arr)
                        {
                            settings.Scopes = arr.Select(x => x?.ToString()).Where(s => !string.IsNullOrEmpty(s)).Cast<string>().ToArray();
                        }
                        settings.ApplicationName = google["ApplicationName"]?.GetValue<string>() ?? "EvaluateHomework";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: failed to read appsettings.json: {ex.Message}");
                }
            }

            // 2. Variables de entorno (sobrescriben lo anterior)
            var envClientId = Environment.GetEnvironmentVariable("GoogleOAuth__ClientId") ?? Environment.GetEnvironmentVariable("GOOGLEOAUTH__CLIENTID");
            if (!string.IsNullOrEmpty(envClientId)) settings.ClientId = envClientId;
            var envClientSecret = Environment.GetEnvironmentVariable("GoogleOAuth__ClientSecret") ?? Environment.GetEnvironmentVariable("GOOGLEOAUTH__CLIENTSECRET");
            if (!string.IsNullOrEmpty(envClientSecret)) settings.ClientSecret = envClientSecret;
            var envRedirect = Environment.GetEnvironmentVariable("GoogleOAuth__RedirectUri");
            if (!string.IsNullOrEmpty(envRedirect)) settings.RedirectUri = envRedirect;

            // 3. User-secrets (solo para desarrollo, solo si no está ya definido)
            try
            {
                var sdoc = LoadUserSecretsJson();
                if (sdoc != null)
                {
                    // Support both flat and nested formats
                    // 1. Flat: { "GoogleOAuth:ClientId": "..." }
                    // 2. Nested: { "GoogleOAuth": { "ClientId": "..." } }
                    settings.ClientId ??= sdoc?["GoogleOAuth:ClientId"]?.GetValue<string>();
                    settings.ClientSecret ??= sdoc?["GoogleOAuth:ClientSecret"]?.GetValue<string>();
                    settings.RedirectUri ??= sdoc?["GoogleOAuth:RedirectUri"]?.GetValue<string>();
                    // Scopes as comma-separated string (flat)
                    if ((settings.Scopes == null || settings.Scopes.Length == 0))
                    {
                        var scopesFlat = sdoc?["GoogleOAuth:Scopes"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(scopesFlat))
                        {
                            settings.Scopes = scopesFlat.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        }
                    }
                    // Nested object fallback
                    var g = sdoc?["GoogleOAuth"];
                    if (g != null)
                    {
                        settings.ClientId ??= g["ClientId"]?.GetValue<string>();
                        settings.RedirectUri ??= g["RedirectUri"]?.GetValue<string>();
                        if ((settings.Scopes == null || settings.Scopes.Length == 0) && g["Scopes"] is JsonArray sarr)
                        {
                            var list = new List<string>();
                            foreach (var x in sarr)
                            {
                                var s = x?.ToString();
                                if (!string.IsNullOrEmpty(s))
                                    list.Add(s);
                            }
                            settings.Scopes = list.ToArray();
                        }
                        // ClientSecret solo desde user-secrets si no está ya en variable de entorno
                        if (string.IsNullOrEmpty(settings.ClientSecret))
                            settings.ClientSecret = g["ClientSecret"]?.GetValue<string>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to read user-secrets: {ex.Message}");
            }

            return settings;
        }
    }
}
