using BackupManagerLibrary;
using BackupManagerLibrary.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BackupManagerGoogle
{
    public class ContactsBackupOperation : BackupOperation
    {
        public ContactsBackupOperation(UserSettings userSettings, ExecutionLog processLog, ILogger logger) : base(userSettings, processLog, logger) {
        }

        public override async Task<ProcessRunner> RunAsync() {
            try {
                _logger.LogInformation("Backing up Google contacts...");
                string contactsDirectory = Constants.Folders.UserContacts(_userSettings.UserData);
                string contactsFile = Constants.Files.UserGoogleContactsBackup(_userSettings.UserData);

                if (Directory.Exists(contactsDirectory)) { Directory.Delete(contactsDirectory, true); }
                Directory.CreateDirectory(contactsDirectory);

                string url = "https://www.google.com/m8/feeds/contacts/default/full?max-results=1000000&oauth_token=@@ACCESS_TOKEN@@";
                url = url.Replace("@@ACCESS_TOKEN@@", _credentialManager.AccessToken);

                await WebUtilities.DownloadFileAsync(url, contactsFile);

                _processLog.LogInformationStatus($"Contacts file written '{contactsFile}'", _logger);
                _processLog.LogSuccessStatus("Contacts backup complete", _logger);
            } catch (Exception ex) {
                _processLog.LogFailedStatus("Error backing up Google Contacts", _logger, ex);
            }
            return this;
        }
    }
}
