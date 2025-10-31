using EvaluationHW.Application.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace EvaluationHW.Application.Services
{
    /// <summary>
    /// Abstracción para inicializar y gestionar autenticación y servicios de Google.
    /// </summary>
    public class GoogleServices
    {
        private GoogleOAuthSettings? _settings;
        private UserCredential? _userCredential;

        /// <summary>
        /// Devuelve todos los Google Docs de una carpeta, dado su nombre.
        /// </summary>
        /// <param name="folderName">Nombre de la carpeta en Google Drive.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Array de GoogleDoc con nombre, id y texto de cada documento.</returns>
        public async Task<GoogleDoc[]> GetGoogleDocsByFolderNameAsync(string folderName, CancellationToken cancellationToken = default)
        {
            // Obtener el id de la carpeta
            var folderId = await GetFolderIdByNameAsync(folderName, cancellationToken);

            // Inicializar OAuth y servicios si no están ya inicializados
            if (_userCredential == null || _settings == null)
                throw new InvalidOperationException("Google OAuth settings not initialized. Llama a Init primero.");

            var driveService = new DriveService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = _userCredential,
                ApplicationName = _settings.ApplicationName ?? "EvaluateHomework"
            });

            // Obtener todos los Google Docs en la carpeta
            var docs = await ListGoogleDocsInFolderAsync(driveService, folderId, cancellationToken);
            return docs;
        }

        /// <summary>
        /// Inicializa la configuración de Google con las credenciales proporcionadas.
        /// </summary>
        /// <param name="settings">Objeto de configuración con ClientId, ClientSecret, etc.</param>
        public void Init(GoogleOAuthSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Busca una carpeta por nombre, obtiene todos los Google Docs dentro y recorre cada uno para su validación por IA.
        /// </summary>
        /// <param name="folderName">Nombre de la carpeta en Google Drive.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
    public async Task<string> GetFolderIdByNameAsync(string folderName, CancellationToken cancellationToken = default)
        {
            if (_settings == null)
                throw new InvalidOperationException("Google OAuth settings not initialized. Llama a Init primero.");

            // Inicializar OAuth y servicios
            _userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = _settings.ClientId,
                    ClientSecret = _settings.ClientSecret
                },
                _settings.Scopes,
                "user",
                cancellationToken
            );

            var driveService = new DriveService(new Google.Apis.Services.BaseClientService.Initializer
            {
                HttpClientInitializer = _userCredential,
                ApplicationName = _settings.ApplicationName ?? "EvaluateHomework"
            });

            // Buscar la carpeta por coincidencia parcial (case-insensitive en C#)
            var folderRequest = driveService.Files.List();
            var safeName = folderName.Replace("'", "\\'");
            folderRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and trashed = false and name = '{safeName}'";
            folderRequest.Fields = "files(id, name)";

            var folderResult = await folderRequest.ExecuteAsync(cancellationToken);
            if (folderResult.Files == null || folderResult.Files.Count == 0)
                throw new Exception($"No se encontró la carpeta con nombre exacto '{folderName}' en Drive.");

            // Solo debería haber coincidencias exactas por la query
            if (folderResult.Files.Count == 0)
                throw new Exception($"No se encontró la carpeta con nombre exacto '{folderName}' en Drive.");
            if (folderResult.Files.Count > 1)
            {
                var names = string.Join(", ", folderResult.Files.Select(f => $"{f.Name} (ID: {f.Id})"));
                throw new Exception($"Se encontraron varias carpetas con el nombre exacto '{folderName}': {names}");
            }
            return folderResult.Files[0].Id;
        }
       
        /// <summary>
        /// Devuelve la lista de documentos Google Docs en una carpeta de Drive.
        /// </summary>
        /// <param name="driveService">Instancia autenticada de DriveService.</param>
        /// <param name="folderId">ID de la carpeta de Drive.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Array de GoogleDoc con nombre e ID de cada documento.</returns>
        public async Task<GoogleDoc[]> ListGoogleDocsInFolderAsync(DriveService driveService, string folderId, CancellationToken cancellationToken = default)
        {
            var docs = new List<GoogleDoc>();
            var request = driveService.Files.List();
            request.Q = $"'{folderId}' in parents and mimeType = 'application/vnd.google-apps.document' and trashed = false";
            request.Fields = "files(id, name)";
            FileList result = await request.ExecuteAsync(cancellationToken);
            if (result.Files != null)
            {
                    // Inicializar DocsService para obtener el texto de cada documento
                    var docsService = new DocsService(new Google.Apis.Services.BaseClientService.Initializer
                    {
                        HttpClientInitializer = driveService.HttpClientInitializer,
                        ApplicationName = driveService.ApplicationName
                    });
                    foreach (var file in result.Files)
                    {
                        var doc = new GoogleDoc
                        {
                            DocumentName = file.Name ?? string.Empty,
                            DocumentId = file.Id ?? string.Empty
                        };
                        // Obtener el texto plano del documento
                        await GetGoogleDocTextAsync(docsService, doc, cancellationToken);
                        docs.Add(doc);
                }
            }
            return docs.ToArray();
        }

        /// <summary>
        /// Obtiene el texto plano de un Google Doc y lo almacena en la propiedad DocumentText del objeto GoogleDoc.
        /// </summary>
        /// <param name="docsService">Instancia autenticada de DocsService.</param>
        /// <param name="doc">Objeto GoogleDoc con DocumentId.</param>
        /// <param name="cancellationToken">Token de cancelación opcional.</param>
        /// <returns>Task completado cuando el texto ha sido cargado en doc.DocumentText.</returns>
        public async Task GetGoogleDocTextAsync(DocsService docsService, GoogleDoc doc, CancellationToken cancellationToken = default)
        {
            if (doc == null || string.IsNullOrEmpty(doc.DocumentId))
                throw new ArgumentException("GoogleDoc inválido");

            Document document = await docsService.Documents.Get(doc.DocumentId).ExecuteAsync(cancellationToken);
            var text = new System.Text.StringBuilder();
            if (document.Body?.Content != null)
            {
                foreach (var element in document.Body.Content)
                {
                    if (element.Paragraph?.Elements != null)
                    {
                        foreach (var pe in element.Paragraph.Elements)
                        {
                            text.Append(pe.TextRun?.Content);
                        }
                    }
                }
            }
            doc.DocumentText = text.ToString();
        }
    }

    public class GoogleDoc
    {
        public string DocumentName { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string DocumentText { get; set; } = string.Empty;
    }
}
