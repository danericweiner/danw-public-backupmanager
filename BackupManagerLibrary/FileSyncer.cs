using BackupManagerLibrary.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BackupManagerLibrary
{
    public class FileSyncer
    {
        private ILogger _logger;
        private ExecutionLog _executionLog;
        private string _root;
        private Dictionary<string, FileSyncerInfo> _fileInfoByPath;

        private class FileSyncerInfo
        {
            public DateTime LastWriteTime { get; set; }
            public bool Matched { get; set; }
        }

        public FileSyncer(string root, ExecutionLog executionLog, ILogger logger) {
            _root = root;
            _executionLog = executionLog;
            _logger = logger;
            _fileInfoByPath = new Dictionary<string, FileSyncerInfo>();
        }

        public FileSyncer Initialize() {
            FillDirectoryInfo(_root);
            return this;
        }

        public bool MatchFile(string path, DateTime? lastWriteTime = null) {
            bool fileUnchanged = false;
            if (_fileInfoByPath.ContainsKey(path)) {
                FileSyncerInfo fileInfo = _fileInfoByPath[path];
                if (lastWriteTime == null
                || (fileInfo.LastWriteTime - (DateTime)lastWriteTime).Duration() <= TimeSpan.FromSeconds(1)) {
                    fileUnchanged = true;
                }
                fileInfo.Matched = true;
            }
            return fileUnchanged;
        }

        public void Complete(bool purgeDeletedFiles = true, bool deleteEmptyDirectories = true) {
            if (purgeDeletedFiles) { PurgeDeleteFiles(); }
            if (deleteEmptyDirectories) { DeleteEmptyDirectories(_root); }
        }

        private void FillDirectoryInfo(string root) {
            DirectoryInfo di = new DirectoryInfo(root);
            foreach (FileInfo file in di.GetFiles()) {
                if (!_fileInfoByPath.ContainsKey(file.FullName)) {
                    _fileInfoByPath.Add(file.FullName, new FileSyncerInfo() { LastWriteTime = file.LastWriteTime, Matched = false });
                }
            }
            foreach (DirectoryInfo dir in di.GetDirectories()) {
                FillDirectoryInfo(dir.FullName);
            }
        }

        private void PurgeDeleteFiles() {
            foreach (string path in _fileInfoByPath.Keys) {
                if (_fileInfoByPath[path].Matched) { continue; }
                try {
                    File.Delete(path);
                    _executionLog.LogWarningStatus($"File deleted '{path}'", _logger);
                } catch (Exception ex) {
                    _executionLog.LogFailedStatus($"Unable to purge deleted file '{path}'", _logger, ex); // keep going
                }
            }
        }

        private void DeleteEmptyDirectories(string root) {
            foreach (string path in Directory.GetDirectories(root)) {
                DeleteEmptyDirectories(path);                
                if (Directory.GetDirectories(path).Length == 0 && Directory.GetFiles(path).Length == 0) {
                    try {
                        Directory.Delete(path);
                        _executionLog.LogInformationStatus($"Empty directory deleted '{path}'", _logger);
                    } catch (Exception ex) {
                        _executionLog.LogFailedStatus($"Unable to delete empty directory '{path}'", _logger, ex); // keep going
                    }
                }
            }
        }
    }
}
