using BackupManagerLibrary;
using BackupManagerLibrary.Models;
using Microsoft.Extensions.Logging;
using System;

namespace BackupManagerGoogle
{
    public abstract class BackupOperation : ProcessRunner
    {
        protected UserSettings _userSettings;
        protected CredentialManager _credentialManager;

        public BackupOperation(UserSettings userSettings, ExecutionLog processLog, ILogger logger) : base(processLog, logger) {
            _userSettings = userSettings;
        }

        public override ProcessRunner Initialize() {
            try {
                _processLog.Start();
                _credentialManager = new CredentialManager(_logger).Authorize();
            } catch(Exception ex) {
                _processLog.LogFailedStatus("Error initializing Backup Operation", _logger, ex);
                _processLog.End();
                throw;
            }            
            return this;
        }

        public override ProcessRunner Complete() {
            _processLog.End();
            return this;
        }
    }
}
