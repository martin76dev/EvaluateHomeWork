namespace EvaluationHW.Application.Configuration
{
    /// <summary>
    /// POCO para enlazar la configuración de OpenAI/ChatGPT desde appsettings, secretos o variables de entorno.
    /// </summary>
    public class ChatGPTSettings
    {
        /// <summary>
        /// API Key de OpenAI/ChatGPT. Debe almacenarse en secretos o variables de entorno.
        /// </summary>
        public string? ApiKey { get; set; }
    }
}