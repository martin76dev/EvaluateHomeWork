namespace EvaluationHW.Application.Configuration
{
    /// <summary>
    /// POCO to bind Google OAuth configuration from appsettings / environment / user-secrets.
    /// </summary>
    public class GoogleOAuthSettings
    {
        /// <summary>
        /// OAuth client id (e.g. xxxxx.apps.googleusercontent.com). Can be stored in appsettings for non-secret usage.
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// OAuth client secret. SHOULD NOT be committed to source control; use user-secrets, env vars or Key Vault.
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Redirect URI configured in Google Cloud Console.
        /// </summary>
        public string? RedirectUri { get; set; }

        /// <summary>
        /// Array of scopes to request from Google APIs.
        /// </summary>
        public string[]? Scopes { get; set; }
        /// <summary>
        /// Nombre de la aplicación para los servicios de Google.
        /// </summary>
        public string ApplicationName { get; set; } = "EvaluateHomework";
    }
}
