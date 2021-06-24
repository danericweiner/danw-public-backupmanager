using BackupManagerGoogle.Models.Photos;
using BackupManagerLibrary;
using BackupManagerLibrary.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using static BackupManagerLibrary.Utilities;

namespace BackupManagerGoogle
{
    public class PhotosBackupOperation : BackupOperation
    {
        private readonly string _backupRoot;
        private FileSyncer _fileSyncer;
        private int _failedDownloadCount;

        public PhotosBackupOperation(UserSettings userSettings, ExecutionLog processLog, ILogger logger) : base(userSettings, processLog, logger) {
            _backupRoot = Constants.Folders.UserPhotos(_userSettings.UserData);
            _fileSyncer = new FileSyncer(_backupRoot, processLog, _logger);
        }

        public override async Task<ProcessRunner> RunAsync() {
            try {
                _logger.LogInformation("Backing up Google Photos...");

                if (!Directory.Exists(_backupRoot)) { Directory.CreateDirectory(_backupRoot); }

                _failedDownloadCount = 0;
                _fileSyncer.Initialize();

                await DownloadChangedGooglePhotosAsync();

                _fileSyncer.Complete(true, true);

                _processLog.LogSuccessStatus("Photos backup complete", _logger);
            } catch (Exception ex) {
                _processLog.LogFailedStatus("Error backing up Google Photos", _logger, ex);
            }
            return this;
        }

        private async Task DownloadChangedGooglePhotosAsync() {
            string pageToken = null;            
            do {
                string url = $"https://photoslibrary.googleapis.com/v1/mediaItems?pageSize=100";
                if (pageToken != null) { url += $"&pageToken={pageToken}"; }

                MediaItemsList mediaItemsList = await GetJsonObjectWithAuthorizationRetry<MediaItemsList>(url);

                foreach (MediaItem mediaItem in mediaItemsList.MediaItems) {                    
                    await BackupMediaItemAsync(mediaItem);                    
                }
                pageToken = mediaItemsList.NextPageToken;
            } while (pageToken != null);
        }

        private async Task<TResponse> GetJsonObjectWithAuthorizationRetry<TResponse>(string url) {
            try {
                HeaderItem[] headers = new HeaderItem[] { GetAuthorizationHeader(_credentialManager.AccessToken) };
                return await WebUtilities.GetJsonObjectAsync<TResponse>(url, headers);
            } catch (Exception) {
                HeaderItem[] headers = new HeaderItem[] { GetAuthorizationHeader(_credentialManager.Authorize().AccessToken) };
                return await WebUtilities.GetJsonObjectAsync<TResponse>(url, headers);
            }
        }

        private HeaderItem GetAuthorizationHeader(string accessToken) {
            return new HeaderItem() { Key = "Authorization", Value = $"Bearer {accessToken}" };
        }

        private async Task BackupMediaItemAsync(MediaItem mediaItem) {
            DateTime creationTime = GetCreationTime(mediaItem);
            string backupFile = GetBackupFilePath(mediaItem, creationTime);

            if (_fileSyncer.MatchFile(backupFile)) { return; }

            WriteMediaItemTrace(mediaItem);

            try {
                if (IsVideo(mediaItem.MimeType)) {
                    await BackupVideoAsync(mediaItem, backupFile, creationTime);
                } else {
                    await BackupPhotoAsync(mediaItem, backupFile, creationTime);
                }
                _processLog.LogInformationStatus($"File written '{backupFile}'", _logger);
            } catch (Exception ex) {
                _processLog.LogFailedStatus($"Photo backup failure, try downloading manually\nId: {mediaItem.Id}\nUrl: {mediaItem.ProductUrl}\nBackup Path: {backupFile}", _logger, ex);
                _failedDownloadCount++;
                if (_failedDownloadCount > Constants.GoogleAccess.Photos.MaximumFailuresBeforeFatal) {
                    throw new Exception($"Photos failures maximum '{Constants.GoogleAccess.Photos.MaximumFailuresBeforeFatal}' exceeded", ex);
                }
            }
        }

        private void WriteMediaItemTrace(MediaItem mediaItem) {
            if (!_logger.IsEnabled(LogLevel.Trace)) { return; }  // Serilog equivalent is "Verbose"
            string json = JsonConvert.SerializeObject(mediaItem, Formatting.Indented);
            _logger.LogTrace(json);
        }

        private async Task BackupPhotoAsync(MediaItem mediaItem, string path, DateTime creationTime) {
            string url = $"{mediaItem.BaseUrl}=d";
            await WebUtilities.DownloadFileAsync(url, path, creationTime);
        }

        private async Task BackupVideoAsync(MediaItem mediaItem, string path, DateTime creationTime) {
            var videoStatus = mediaItem.MediaMetadata?.Video?.Status ?? VideoStatus.Ready;
            if (videoStatus != VideoStatus.Ready) {
                _processLog.LogWarningStatus($"Video with status '{videoStatus.ToString()}' not backed up\nUrl: {mediaItem.ProductUrl}", _logger);
                return;
            }
            string url = $"{mediaItem.BaseUrl}=dv";
            await WebUtilities.DownloadFileAsync(url, path, creationTime);
        }       

        private string GetBackupFilePath(MediaItem mediaItem, DateTime creationTime) {
            string fileExtension = Path.GetExtension(mediaItem.Filename.ToLower());
            string fileName = Sha1HashString(mediaItem.Id, Constants.GoogleAccess.Photos.DownloadedFileNameLength); // use persistent unique id - but shorten it
            string fileNameWithExtension = $"{fileName}{fileExtension}";
            string yearFolder = creationTime.ToString("yyyy");
            string monthFolder = creationTime.ToString("MM");
            return Path.Combine(_backupRoot, yearFolder, monthFolder, fileNameWithExtension);
        }

        private DateTime GetCreationTime(MediaItem mediaitem) {
            if (DateTime.TryParse(mediaitem.MediaMetadata?.CreationTime, out DateTime creationTime)) {
                return creationTime;
            }
            return DateTime.MinValue;
        }

        private bool IsVideo(string mimeType) {
            return mimeType != null && mimeType.ToLower().StartsWith("video");
        }
    }
}
