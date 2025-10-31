using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using EvaluationHW.Application.Models;

namespace EvaluationHW.Application.Services
{
	public class ChatGPTService
	{
		private readonly string _apiKey;
		private readonly string _dataDirectory;
		private readonly bool _mock;

		public ChatGPTService(string apiKey, string? dataDirectory = null, bool mock = false)
		{
			_apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
			_dataDirectory = dataDirectory ?? Path.Combine(AppContext.BaseDirectory, "Data");
			_mock = mock;
		}

		public async Task<ChatGptResponse> EvaluarTextoConRubricaAsync(string texto, string nombreRubrica)
		{
			if (_mock)
			{
				// MOCK: Carga el JSON de respuesta desde un archivo en Data/mock_gpt_response.json
				var mockPath = Path.Combine(_dataDirectory, "mock_gpt_response.json");
				if (!File.Exists(mockPath))
					throw new FileNotFoundException($"No se encontró el archivo de mock '{mockPath}'");
				var mockJson = await File.ReadAllTextAsync(mockPath);
				var mockResponse = JsonSerializer.Deserialize<ChatGptResponse>(mockJson);
				if (mockResponse == null)
					throw new Exception("No se pudo deserializar el mock de ChatGPT");
				return mockResponse;
			}

			// Leer la rúbrica desde el archivo JSON
			var rubricaPath = Path.Combine(_dataDirectory, nombreRubrica + ".json");
			if (!File.Exists(rubricaPath))
				throw new FileNotFoundException($"No se encontró la rúbrica '{nombreRubrica}' en '{_dataDirectory}'.", rubricaPath);
			var rubricaJson = await File.ReadAllTextAsync(rubricaPath);

			// Construir el payload para la API de OpenAI
			var payload = new
			{
				model = "gpt-4o-mini",
				temperature = 0.2,
				messages = new List<object>
				{
					new { role = "system", content = "Eres un evaluador de escritura experto." },
					new { role = "user", content = $"Texto del estudiante:\n\n{texto}" },
					new { role = "user", content = $"Rúbrica de evaluación:\n\n{rubricaJson}\n\nDevuelve la evaluación en formato JSON con las claves: 'criterio', 'nivel' (1-4) y 'comentario'." }
				}
			};

			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

			var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadAsStringAsync();
				throw new Exception($"Error al llamar a OpenAI: {response.StatusCode} - {error}");
			}
			var responseJson = await response.Content.ReadAsStringAsync();
			var chatGptResponse = JsonSerializer.Deserialize<ChatGptResponse>(responseJson);
			if (chatGptResponse == null)
				throw new Exception("No se pudo deserializar la respuesta de ChatGPT");
			return chatGptResponse;
		}
	}
}
