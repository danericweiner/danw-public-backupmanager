using BackupManagerLibrary.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BackupManagerLibrary
{
    public abstract class ProcessRunner
    {
        protected ILogger _logger;
        protected ExecutionLog _processLog;

        public ProcessRunner(ExecutionLog processLog, ILogger logger) {
            _logger = logger;
            _processLog = processLog;
        }

        public abstract ProcessRunner Initialize();
        public abstract Task<ProcessRunner> RunAsync();
        public abstract ProcessRunner Complete();
    }

    public class StatusRunner : ProcessRunner
    {
        private ExecutionStatus _status;
        private string _messages;

        public StatusRunner(ExecutionStatus status, string messages, ExecutionLog processLog, ILogger logger) : base(processLog, logger) {
            _status = status;
            _messages = messages;
        }

        public StatusRunner(ExecutionStatus status, ExecutionLog processLog, ILogger logger) : base(processLog, logger) {
            _status = status;
            _messages = null;
        }

        public override ProcessRunner Initialize() {
            _processLog.Start();
            return this;
        }
        public override Task<ProcessRunner> RunAsync() {
            _processLog.LogStatus(_status, _messages, _logger);
            return Task.FromResult<ProcessRunner>(this);
        }

        public override ProcessRunner Complete() {
            _processLog.End();
            return this;
        }
    }
}
