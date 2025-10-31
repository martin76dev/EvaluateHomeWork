using System.Text.Json.Serialization;

namespace EvaluationHW.Application.Models
{
    public class ChatGptChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public ChatGptMessage Message { get; set; }
    }

    public class ChatGptMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class ChatGptResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatGptChoice> Choices { get; set; }
    }

    // Modelo para la evaluación extraída del content
    public class EvaluacionRubrica
    {
        [JsonPropertyName("criterio")]
        public string Criterio { get; set; }

        [JsonPropertyName("nivel")]
        public int Nivel { get; set; }

        [JsonPropertyName("comentario")]
        public string Comentario { get; set; }
    }

    public class EvaluacionRoot
    {
        [JsonPropertyName("evaluacion")]
        public List<EvaluacionRubrica> Evaluacion { get; set; }
    }
}
