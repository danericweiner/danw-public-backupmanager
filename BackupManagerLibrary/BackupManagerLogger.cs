using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RazorLight;

namespace BackupManagerLibrary
{
    /// <summary>
    /// These enums are in order of precedence from low to high.
    /// E.g. A 'Warning' combined with a 'Success' will produce a 'Warning'
    /// </summary>
    public enum ExecutionStatus
    {
        None = 0,
        Skipped = 1,
        Success = 2,
        Warning = 3,
        Failed = 4
    }

    public static class ExecutionStatusExtensions
    {
        public static bool IsError(this ExecutionStatus status) {
            switch (status) {
                case ExecutionStatus.None: return false;
                case ExecutionStatus.Skipped: return false;
                case ExecutionStatus.Success: return false;
                case ExecutionStatus.Warning: return true;
                case ExecutionStatus.Failed: return true;
                default: return false;
            }
        }

        public static string ToHtml(this ExecutionStatus status) {
            switch (status) {
                case ExecutionStatus.None: return "";
                case ExecutionStatus.Skipped: return "Skipped";
                case ExecutionStatus.Success: return "Success";
                case ExecutionStatus.Warning: return "Warning";
                case ExecutionStatus.Failed: return "Failed";
                default: return status.ToString();
            }
        }

        public static string CssClass(this ExecutionStatus status) {
            switch (status) {
                case ExecutionStatus.None: return "Skipped";
                case ExecutionStatus.Skipped: return "Skipped";
                case ExecutionStatus.Success: return "Information";
                case ExecutionStatus.Warning: return "Warning";
                case ExecutionStatus.Failed: return "Error";
                default: return status.ToString();
            }
        }

        public static ExecutionStatus Combine(this ExecutionStatus currentStatus, ExecutionStatus newStatus) {
            return (ExecutionStatus)Math.Max((int)currentStatus, (int)newStatus);
        }
    }

    public enum ExecutionMessageStatus
    {
        Information,
        Warning,
        Error
    }

    public static class ExecutionMessageStatusExtensions
    {
        public static string CssClass(this ExecutionMessageStatus status) {
            switch (status) {
                case ExecutionMessageStatus.Information: return "Information";
                case ExecutionMessageStatus.Warning: return "Warning";
                case ExecutionMessageStatus.Error: return "Error";
                default: return status.ToString();
            }
        }
    }

    public class ExecutionMessage
    {
        public string Message { get; private set; }
        public ExecutionMessageStatus Status { get; private set; }
        public ExecutionMessage(ExecutionMessageStatus status, string message) {
            Message = message;
            Status = status;
        }
    }

    public class BackupManagerLogger
    {
        public string ApplicationShortName { get; set; }
        public string ApplicationLongName { get; set; }
        public string ApplicationVersion { get; set; }
        public string MachineName { get; set; }
        public string ApplicationDataFolder { get; set; }
        public string UserDataFolder { get; set; }
        public ExecutionLog ActionLog { get; set; }
        public BackupManagerLogger() {
        }

        public string GetEmailTitle() {
            return $"{MachineName} - {ActionLog.Status.ToHtml()}";
        }

        public async Task<string> GetEmailBodyAsync() {
            RazorLightEngine razorLightEngine = new RazorLightEngineBuilder().UseEmbeddedResourcesProject(typeof(BackupManagerLogger))
                                                                             .SetOperatingAssembly(Assembly.GetExecutingAssembly())
                                                                             .Build();
            return await razorLightEngine.CompileRenderAsync("Templates.HtmlLog.cshtml", this);
        }
    }

    public class ExecutionLog
    {
        private ExecutionStatus _status = ExecutionStatus.None;
        public string Id { get; private set; }
        public string Name { get; set; }
        public string CronSchedule { get; set; }
        public ExecutionStatus Status {
            get {
                if (ProcessLogs == null) { return _status; }
                ExecutionStatus childStatus = ExecutionStatus.None;
                foreach (ExecutionLog processLog in ProcessLogs) {
                    childStatus = childStatus.Combine(processLog.Status);
                }
                return _status.Combine(childStatus);
            }
            private set {
                _status = value;
            }
        }
        public List<ExecutionMessage> Messages { get; private set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration {
            get {
                if (StartTime == null || EndTime == null) { return TimeSpan.Zero; }
                return EndTime - StartTime;
            }
        }
        public List<ExecutionLog> ProcessLogs { get; set; }
        public ExecutionLog(string id, string name) {
            Id = id;
            Name = name;
            Status = ExecutionStatus.None;
            Messages = new List<ExecutionMessage>();
            ProcessLogs = new List<ExecutionLog>();
        }

        public ExecutionLog GetProcessLog(string id) {
            foreach (ExecutionLog executionLog in this.ProcessLogs) {
                if (executionLog.Id.ToLower() == id.ToLower()) {
                    return executionLog;
                }
            }
            return null;
        }

        public void Start() {
            Status = ExecutionStatus.None;
            Messages = new List<ExecutionMessage>();
            ProcessLogs = new List<ExecutionLog>();
            StartTime = DateTime.Now;
        }

        public void End(ExecutionStatus status = ExecutionStatus.None, string message = null, ILogger logger = null) {
            LogStatus(status, message, logger);
            EndTime = DateTime.Now;
        }

        public void LogStatus(ExecutionStatus status, string message = null, ILogger logger = null, Exception ex = null) {
            switch (status) {
                case ExecutionStatus.None: LogInformationStatus(message, logger); break;
                case ExecutionStatus.Success: LogSuccessStatus(message, logger); break;
                case ExecutionStatus.Skipped: LogSkippedStatus(message, logger); break;
                case ExecutionStatus.Warning: LogWarningStatus(message, logger, ex); break;
                case ExecutionStatus.Failed: LogFailedStatus(message, logger, ex); break;
                default: LogFailedStatus(message, logger, ex); break;
            }
        }

        public void LogInformationStatus(string message, ILogger logger) {
            Status = Status.Combine(ExecutionStatus.None); // for completenesss            
            if (message != null) {
                logger?.LogInformation(message);
                Messages.Add(new ExecutionMessage(ExecutionMessageStatus.Information, message));
            }
        }

        public void LogSkippedStatus(string message = null, ILogger logger = null) {
            Status = Status.Combine(ExecutionStatus.Skipped);
            if (message != null) {
                logger?.LogInformation(message);
                Messages.Add(new ExecutionMessage(ExecutionMessageStatus.Information, message));
            }
        }

        public void LogSuccessStatus(string message = null, ILogger logger = null) {
            Status = Status.Combine(ExecutionStatus.Success);
            if (message != null) {
                logger?.LogInformation(message);
                Messages.Add(new ExecutionMessage(ExecutionMessageStatus.Information, message));
            }
        }

        public void LogWarningStatus(string message, ILogger logger = null, Exception ex = null) {
            Status = Status.Combine(ExecutionStatus.Warning);
            if (message != null) {
                logger?.LogWarning(ex, message, null);
                Messages.Add(new ExecutionMessage(ExecutionMessageStatus.Warning, message));
                if (ex != null) {
                    Messages.Add(new ExecutionMessage(ExecutionMessageStatus.Warning, ex.Message));
                }
            }

        }

        public void LogFailedStatus(string message, ILogger logger = null, Exception ex = null) {
            Status = Status.Combine(ExecutionStatus.Failed);
            if (message != null) {
                logger?.LogError(ex, message, null);
                Messages.Add(new ExecutionMessage(ExecutionMessageStatus.Error, message));
                if (ex != null) {
                    Messages.Add(new ExecutionMessage(ExecutionMessageStatus.Error, ex.Message));
                    Messages.Add(new ExecutionMessage(ExecutionMessageStatus.Error, ex.StackTrace));
                }
            }
        }
    }
}
