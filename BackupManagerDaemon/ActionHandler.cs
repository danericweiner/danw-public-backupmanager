using BackupManagerDaemon.Models;
using BackupManagerGoogle;
using BackupManagerLibrary;
using BackupManagerLibrary.Models;
using Hangfire;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BackupManagerDaemon
{
    public class ActionHandler
    {
        private readonly ILogger _logger;
        private readonly UserSettings _userSettings;
        private readonly ApplicationSettings _applicationSettings;
        private readonly EmailSender _emailSender;

        private ActionItem _actionItem;
        private ExecutionLog _actionLog;
        private BackupManagerLogger _backupManagerLogger;

        public ActionHandler(ILogger<ActionHandler> logger, UserSettings userSettings, IOptions<ApplicationSettings> applicationConfig, EmailSender emailSender) {
            try {
                _logger = logger;
                _userSettings = userSettings;
                _applicationSettings = applicationConfig.Value;
                _emailSender = emailSender;
            } catch (Exception ex) {
                _logger.LogError(ex, $"Error loading {nameof(ActionHandler)}", null);
            }
        }

        public async Task ActionAsync(PerformContext context) {
            string recurringJobId = "";

            try {
                recurringJobId = GetRecurringJobId(context);

                _logger.LogInformation($"Action Key '{recurringJobId}' beginning...");

                InitializeAction(recurringJobId);
                _actionLog.Start();

                foreach (ProcessItem processItem in _actionItem.Processes) {
                    ExecutionLog processLog = new ExecutionLog(processItem.Key, processItem.Name);
                    _actionLog.ProcessLogs.Add(processLog);
                    _logger.LogInformation($"Action Key '{recurringJobId}' Process Key '{processItem.Key}' queued");
                }

                foreach (ProcessItem processItem in _actionItem.Processes) {
                    _logger.LogInformation($"Action Key '{recurringJobId}' Process Key '{processItem.Key}' beginning...");

                    (await ProcessRunnerFactory(processItem).Initialize().RunAsync()).Complete();

                    _logger.LogInformation($"Action Key '{recurringJobId}' Process Key '{processItem.Key}' complete");
                }

                _actionLog.End(ExecutionStatus.Success);
                _emailSender?.SendEmail(_backupManagerLogger.GetEmailTitle(), await _backupManagerLogger.GetEmailBodyAsync());
                _logger.LogInformation($"Action Key '{recurringJobId}' complete");
            } catch (Exception ex) {
                _logger.LogError(ex, $"Error completing Action Key '{recurringJobId}'", null);
                if (_backupManagerLogger != null) {
                    _actionLog.End(ExecutionStatus.Failed, ex.Message);
                    _emailSender?.SendEmail(_backupManagerLogger.GetEmailTitle(), await _backupManagerLogger.GetEmailBodyAsync());
                }
            }
        }

        private string GetRecurringJobId(PerformContext context) {
            IStorageConnection connection = JobStorage.Current.GetConnection();
            string backgroundJobId = context.BackgroundJob.Id;
            string recurringJobId = connection.GetRecurringJobs().Where(j => j.LastJobId == backgroundJobId).FirstOrDefault()?.Id;
            if (recurringJobId == null) { throw new Exception("Unable to find recurring job id"); }
            return recurringJobId;
        }

        private void InitializeAction(string recurringJobId) {
            _actionItem = _userSettings.GetActionItemByKey(recurringJobId);

            _actionLog = new ExecutionLog(_actionItem.Key, _actionItem.Name) {
                CronSchedule = _actionItem.CronSchedule
            };

            _backupManagerLogger = new BackupManagerLogger() {
                ApplicationShortName = Constants.ApplicationShortName,
                ApplicationLongName = Constants.ApplicationLongName,
                ApplicationVersion = _applicationSettings.Version,
                MachineName = Environment.MachineName,
                ApplicationDataFolder = Constants.Folders.ApplicationData,
                UserDataFolder = _userSettings.UserData,
                ActionLog = _actionLog
            };
        }

        private ProcessRunner ProcessRunnerFactory(ProcessItem processItem) {
            ExecutionLog processLog = _actionLog.GetProcessLog(processItem.Key);
            if (processItem.Skip) { return new StatusRunner(ExecutionStatus.Skipped, "Process skipped", processLog, _logger); }
            switch (processItem.Key.ToLower()) {
                case string key when key == Constants.ProcessKeys.BackupGoogleDocs.ToLower(): return new DocsBackupOperation(processItem.GetArguments<GoogleDocsArgs>(), _userSettings, processLog, _logger);
                case string key when key == Constants.ProcessKeys.BackupGoogleContacts.ToLower(): return new ContactsBackupOperation(_userSettings, processLog, _logger);
                case string key when key == Constants.ProcessKeys.BackupGoogleCalendar.ToLower(): return new CalendarBackupOperation(processItem.GetArguments<GoogleCalendarArgs>(), _userSettings, processLog, _logger);
                case string key when key == Constants.ProcessKeys.BackupGooglePhotos.ToLower(): return new PhotosBackupOperation(_userSettings, processLog, _logger);
                case string key when key == Constants.ProcessKeys.LogToEmail.ToLower(): return new StatusRunner(ExecutionStatus.Success, processLog, _logger);
                default: return new StatusRunner(ExecutionStatus.Failed, $"The Process Key '{processItem.Key}' does not match a known key", processLog, _logger);
            }
        }
    }
}
