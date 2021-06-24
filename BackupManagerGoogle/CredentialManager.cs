using BackupManagerLibrary;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;

namespace BackupManagerGoogle
{
    public class CredentialManager
    {
        private ILogger _logger;
        private UserCredential _userCredential;

        public CredentialManager(ILogger logger) {
            _logger = logger;
        }

        public string AccessToken { get; private set; }            
        public DriveService DriveService { get; private set; }

        public CredentialManager Authorize() {
            try {
                _logger.LogInformation("Authorizing Google account token");
                if (!Directory.Exists(Constants.Folders.ApplicationCredentials)) { Directory.CreateDirectory(Constants.Folders.ApplicationCredentials); }

                using (var stream = Constants.GoogleAccess.ApiClientSecretJson.ToStream()) {
                    _userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, Constants.GoogleAccess.Scopes, "user", CancellationToken.None, new FileDataStore(Constants.Files.ApplicationUserGoogleAccountToken, true)).Result;
                    string temp = _userCredential.GetAccessTokenForRequestAsync().Result; // force refresh
                }

                AccessToken = _userCredential.Token.AccessToken;

                DriveService = new DriveService(new BaseClientService.Initializer() {
                    HttpClientInitializer = _userCredential,
                    ApplicationName = Constants.GoogleAccess.ApplicationName
                });

                return this;
            } catch (Exception ex) {
                throw new Exception($"Credential authorization failed\n{ex.Message}", ex);
            }
        }
    }
}
