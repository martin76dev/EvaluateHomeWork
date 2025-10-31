using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using EvaluationHW.Application.Configuration;
using EvaluationHW.Application.Services;
using EvaluationHW.Application.Models;
using System.Text.RegularExpressions;
using System.Globalization;

// Cargar configuración de Google OAuth
var settings = ConfigurationLoader.LoadGoogleOAuthSettings();

// Print values (do not print secrets in real apps)
Console.WriteLine("=== EvaluationHW.Application (Console) ===");
Console.WriteLine("Google OAuth configuration (note: ClientSecret may be stored externally)");
Console.WriteLine($"ClientId: {settings.ClientId}");
Console.WriteLine($"ClientSecret: {(string.IsNullOrEmpty(settings.ClientSecret) ? "(not set)" : "(set)")}");
Console.WriteLine($"RedirectUri: {settings.RedirectUri}");
if (settings.Scopes != null && settings.Scopes.Length > 0)
{
	Console.WriteLine("Scopes: " + string.Join(", ", settings.Scopes));
}
else
{
	Console.WriteLine("Scopes: (none)");
}

// Ejemplo de uso de GoogleServices
try
{
    var googleServices = new GoogleServices();
    googleServices.Init(settings);
    Console.WriteLine("GoogleServices inicializado correctamente.");

    // Solicitar nombre de carpeta y llamar a EvaluateFolderByNameAsync
    Console.WriteLine("Introduce el nombre de la carpeta de Google Drive a evaluar:");
    var folderName = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(folderName))
    {
        var docs = await googleServices.GetGoogleDocsByFolderNameAsync(folderName);
        Console.WriteLine($"Se encontraron {docs.Length} documentos en la carpeta '{folderName}':");
        // Cargar configuración de ChatGPT
    var chatGptSettings = ConfigurationLoader.LoadChatGPTSettings();
        var chatGptService = new ChatGPTService(chatGptSettings.ApiKey!);

    // Lista para acumular resultados JSON
    var resultadosJson = new Dictionary<string, object>();

        foreach (var doc in docs)
        {
            Console.WriteLine($"- {doc.DocumentName} (ID: {doc.DocumentId})");
            if (string.IsNullOrWhiteSpace(doc.DocumentText))
            {
                Console.WriteLine("No se encontró texto a evaluar.");
            }
            else
            {
                try
                {
                    var resultado = await chatGptService.EvaluarTextoConRubricaAsync(doc.DocumentText, "Rubrica");
                    var content = resultado.Choices?.FirstOrDefault()?.Message?.Content;
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        Console.WriteLine($"No se obtuvo evaluación IA para '{doc.DocumentName}'");
                        continue;
                    }
                    var match = Regex.Match(content, "```json\\s*(.*?)```", RegexOptions.Singleline);
                    string evalJson = match.Success ? match.Groups[1].Value : content;
                    var eval = JsonSerializer.Deserialize<EvaluacionRoot>(evalJson);
                    if (eval?.Evaluacion != null)
                    {
                        // Crear objeto para este documento
                        var criterios = new List<Dictionary<string, object>>();
                        foreach (var item in eval.Evaluacion)
                        {
                            criterios.Add(new Dictionary<string, object>
                            {
                                { "criterio", item.Criterio },
                                { "nivel", item.Nivel },
                                { "comentario", item.Comentario }
                            });
                        }
                        resultadosJson[doc.DocumentName] = criterios;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error evaluando '{doc.DocumentName}': {ex.Message}");
                }
            }
        }
        // Guardar el JSON al final
        var jsonFileName = folderName + ".json";
        var jsonFinal = new Dictionary<string, object> { { folderName, resultadosJson } };
        var opcionesJson = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(jsonFileName, JsonSerializer.Serialize(jsonFinal, opcionesJson));
        Console.WriteLine($"Archivo JSON guardado: {jsonFileName}");
    }
    else
    {
        Console.WriteLine("No se proporcionó un nombre de carpeta.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error al inicializar GoogleServices: {ex.Message}");
}

// Función para escapar campos CSV
// ...
// Exit
return;

