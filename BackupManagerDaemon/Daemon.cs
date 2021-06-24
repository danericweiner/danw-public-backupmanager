using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BackupManagerDaemon.Models;
using BackupManagerLibrary;
using BackupManagerLibrary.Models;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackupManagerDaemon
{
    public class Daemon : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ApplicationSettings _applicationSettings;
        private readonly UserSettings _userSettings;

        public Daemon(ILogger<Daemon> logger, IOptions<ApplicationSettings> applicationConfig, UserSettings userSettings) {
            try {
                _logger = logger;
                _applicationSettings = applicationConfig.Value;
                _userSettings = userSettings;
            } catch (Exception ex) {
                _logger.LogError(ex, $"Error loading {nameof(Daemon)}", null);
            }
        }

        //BackgroundService
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                HashSet<string> scheduledActionKeys = new HashSet<string>();

                _logger.LogInformation($"Starting Backup Manager Daemon with parameters:\n" +
                    $"\tVersion: {_applicationSettings.Version}\n" +
                    $"\tApplication Data Folder: {Constants.Folders.ApplicationData}\n" +
                    $"\tUser Data Folder: {_userSettings.UserData}");

                foreach (ActionItem actionItem in _userSettings.Actions) {
                    RecurringJob.AddOrUpdate<ActionHandler>(actionItem.Key, x => x.ActionAsync(null), actionItem.CronSchedule, TimeZoneInfo.Local);

                    if (scheduledActionKeys.Contains(actionItem.Key.ToLower())) {
                        throw new Exception($"Duplicate Action Key detected, Key '{actionItem.Key}' is not unique");
                    }
                    scheduledActionKeys.Add(actionItem.Key.ToLower());

                    _logger.LogInformation($"Action Key '{actionItem.Key}' scheduled with parameters:\n" +
                        $"\tName: {actionItem.Name}\n" +
                        $"\tCron Schedule: {actionItem.CronSchedule}\n" +
                        $"\tRun Now: {actionItem.RunNow.ToString()}");
                }

                _logger.LogInformation($"Backup Manager Daemon jobs successfully scheduled");

                foreach (ActionItem actionItem in _userSettings.Actions) {
                    if (actionItem.RunNow) { RecurringJob.Trigger(actionItem.Key); }
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "Error starting Backup Manager Daemon", null);
            }
            return Task.CompletedTask;
        }
    }
}
