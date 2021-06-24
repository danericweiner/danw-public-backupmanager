using BackupManagerLibrary;
using BackupManagerLibrary.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BackupManagerGoogle
{
    public class CalendarBackupOperation : BackupOperation
    {
        private GoogleCalendarArgs _processArgs;

        public CalendarBackupOperation(GoogleCalendarArgs processArgs, UserSettings userSettings, ExecutionLog processLog, ILogger logger) : base(userSettings, processLog, logger) {
            _processArgs = processArgs;
        }

        public override async Task<ProcessRunner> RunAsync() {
            try {
                _logger.LogInformation("Backing up Google calendar...");

                if (_processArgs.Calendars == null || _processArgs.Calendars.Length == 0) {
                    throw new Exception("No calendars found");
                }

                string calendarDirectory = Constants.Folders.UserCalendar(_userSettings.UserData);

                if (Directory.Exists(calendarDirectory)) { Directory.Delete(calendarDirectory, true); }
                Directory.CreateDirectory(calendarDirectory);

                foreach (GoogleCalendarArgs.CalendarInfo calendar in _processArgs.Calendars) {
                    if (calendar.SecretAddress.Trim().Length == 0) { continue; }
                    string calendarFile = Constants.Files.UserGoogleCalendarBackup(calendar.FileSuffix, _userSettings.UserData);

                    await WebUtilities.DownloadFileAsync(calendar.SecretAddress, calendarFile);
                    _processLog.LogInformationStatus($"Calendar file written '{calendarFile}'", _logger);
                }
                _processLog.LogSuccessStatus("Calendar backup complete", _logger);
            } catch (Exception ex) {
                _processLog.LogFailedStatus("Error backing up Google Calendar", _logger, ex);
            }
            return this;
        }
    }
}
