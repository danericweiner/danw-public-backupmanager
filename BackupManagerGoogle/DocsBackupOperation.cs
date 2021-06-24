using BackupManagerLibrary;
using BackupManagerLibrary.Models;
using Google.Apis.Download;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BackupManagerGoogle
{
    public class DocsBackupOperation : BackupOperation
    {
        private GoogleDocsArgs _processArgs;
        private readonly string _backupRoot;
        private Dictionary<string, string> _cachedPathsById;
        private FileSyncer _fileSyncer;

        public DocsBackupOperation(GoogleDocsArgs processArgs, UserSettings userSettings, ExecutionLog processLog, ILogger logger) : base(userSettings, processLog, logger) {
            _processArgs = processArgs;
            _backupRoot = Constants.Folders.UserDocs(_userSettings.UserData);
            _cachedPathsById = new Dictionary<string, string>();
            _fileSyncer = new FileSyncer(_backupRoot, processLog, _logger);
        }

        public override Task<ProcessRunner> RunAsync() {
            try {
                _logger.LogInformation("Backing up Google Docs...");

                if (!Directory.Exists(_backupRoot)) { Directory.CreateDirectory(_backupRoot); }
                _fileSyncer.Initialize();

                DownloadChangedGoogleDocs();

                _fileSyncer.Complete(true, true);

                _processLog.LogSuccessStatus("Docs backup complete", _logger);
            } catch (Exception ex) {
                _processLog.LogFailedStatus("Error backing up Google Docs", _logger, ex);
            }
            return Task.FromResult<ProcessRunner>(this);
        }

        private void DownloadChangedGoogleDocs() {
            string requestFilter = GetRequestFilter();
            string sPageToken = null;
            do {
                var listRequest = _credentialManager.DriveService.Files.List();
                listRequest.Q = requestFilter;
                listRequest.Spaces = "drive";
                listRequest.Fields = "nextPageToken, files(id, name, mimeType, trashed, parents, modifiedTime)";
                listRequest.PageToken = sPageToken;
                var fileList = listRequest.Execute();
                foreach (var file in fileList.Files) {
                    string path = GetAbsolutePath(file);
                    BackupFile(path, file);
                }
                sPageToken = fileList.NextPageToken;
            } while (sPageToken != null);
        }

        private string GetRequestFilter() {
            var mimeTypeFilter = new StringBuilder();
            foreach (string mimeType in Constants.GoogleAccess.Docs.MimeTypeConversions.Keys) {
                if (mimeTypeFilter.Length > 0) { mimeTypeFilter.Append(" or "); }
                mimeTypeFilter.Append($"mimeType='{mimeType}'");
            }

            var filter = new StringBuilder();
            filter.Append("trashed = false");
            if (_processArgs.OnlyOwnedByMe) { filter.Append(" and 'me' in owners"); }
            filter.Append(" and (");
            filter.Append(mimeTypeFilter.ToString());
            filter.Append(")");
            return filter.ToString();
        }

        private string GetAbsolutePath(Google.Apis.Drive.v3.Data.File file) {
            IList<string> parents = file.Parents;
            if (parents != null && parents.Count > 0) {
                return Path.Combine(GetAbsolutePathHelper(parents[0]), file.Name);
            } else {
                _processLog.LogWarningStatus($"Orphaned file found '{file.Name}'", _logger);
                return file.Name;
            }
        }

        private string GetAbsolutePathHelper(string id) {
            if (!_cachedPathsById.ContainsKey(id)) {
                var absolutePath = "";
                var fileResourceRequest = _credentialManager.DriveService.Files.Get(id);
                fileResourceRequest.Fields = "id, name, parents";
                var file = fileResourceRequest.Execute();
                IList<string> parents = file.Parents;
                if (parents != null && parents.Count > 0) {
                    absolutePath = Path.Combine(GetAbsolutePathHelper(parents[0]), file.Name);
                }
                _cachedPathsById.Add(id, absolutePath);
            }
            return _cachedPathsById[id];
        }

        private void BackupFile(string path, Google.Apis.Drive.v3.Data.File file) {
            if (!Constants.GoogleAccess.Docs.MimeTypeConversions.ContainsKey(file.MimeType)) {
                _processLog.LogWarningStatus($"File not processed '{path}'", _logger);
                return;
            }

            var conversionInfo = Constants.GoogleAccess.Docs.MimeTypeConversions[file.MimeType];
            string backupFile = Path.Combine(_backupRoot, path + conversionInfo.Extension);
            string directDownloadLink = conversionInfo.DirectDownload?.Replace(Constants.GoogleAccess.Docs.FileIdMarker, file.Id);
            DateTime lastWriteTime = file.ModifiedTime ?? DateTime.Now;

            if (_fileSyncer.MatchFile(backupFile, lastWriteTime)) { return; }

            string backupFolder = Path.GetDirectoryName(backupFile);
            var exportRequest = _credentialManager.DriveService.Files.Export(file.Id, conversionInfo.MimeType);
            var memoryStream = new MemoryStream();

            if (!Directory.Exists(backupFolder)) { Directory.CreateDirectory(backupFolder); }
            exportRequest.MediaDownloader.ProgressChanged += (IDownloadProgress downloadProgress) => {
                switch (downloadProgress.Status) {
                    case DownloadStatus.Completed: { WriteDownloadedFile(memoryStream, backupFile, lastWriteTime); break; }
                    case DownloadStatus.Failed: { DownloadFailed(backupFile, directDownloadLink, lastWriteTime); break; } // keep going anyway
                }
            };
            exportRequest.Download(memoryStream);
        }

        private void DownloadFailed(string path, string directDownloadLink, DateTime lastWriteTime) {
            if (directDownloadLink != null) {
                _processLog.LogWarningStatus($"File downoad failed '{path}'\n"
                            + $"\tTo download manually click the following link, organize the file, and then update the file properties in Total Commander.\n"
                            + $"\tLink: {directDownloadLink}\n\tModified Date: {lastWriteTime.ToString("MM/dd/yyyy HH:mm:ss")}", _logger);
            } else {
                _processLog.LogWarningStatus($"Backup file download failed, no download link exists '{path}'", _logger);
            }
        }

        private void WriteDownloadedFile(MemoryStream memoryStream, string path, DateTime lastWriteTime) {
            try {
                IOUtilities.WriteStreamToFile(memoryStream, path, lastWriteTime);
                _processLog.LogInformationStatus($"File written '{path}'", _logger);
            } catch (Exception ex) {
                _processLog.LogFailedStatus($"Error writing file '{path}'", _logger, ex); // keep going anyway                
            }
        }
    }
}
