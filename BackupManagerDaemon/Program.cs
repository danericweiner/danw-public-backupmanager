using System;
using System.IO;
using System.Threading.Tasks;
using BackupManagerDaemon.Models;
using BackupManagerLibrary;
using BackupManagerLibrary.Models;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using static BackupManagerLibrary.Utilities;

namespace BackupManagerDaemon
{
    class Program
    {
        public static async Task Main(string[] args) {
            KillExistingProcesses();
            SetEnvironmentVariables();
            IOUtilities.WriteObjectJson(UserSettings.GetDefault(), Constants.Files.UserSettings, false);
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((context, config) => {
                })
                .ConfigureServices((hostContext, services) => {
                    services.AddOptions();
                    services.Configure<ApplicationSettings>(hostContext.Configuration.GetSection(nameof(ApplicationSettings)));
                    // replace environment variables strings in the settings object
                    services.Configure<ApplicationSettings>(applicationSettings => UpdateObjectProperties<ApplicationSettings, string>(applicationSettings, SubstituteEnvironmentVariables, true));

                    services.AddSingleton(sp => {
                        if (!File.Exists(Constants.Files.UserSettings)) { return null; }
                        string userSettingsString = File.ReadAllText(Constants.Files.UserSettings);
                        UserSettings userSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<UserSettings>(userSettingsString);
                        UpdateObjectProperties<UserSettings, string>(userSettings, SubstituteEnvironmentVariables, true);
                        return userSettings;
                    });

                    services.AddSingleton(sp => {
                        UserSettings userSettings = sp.GetRequiredService<UserSettings>();
                        EmailSettings emailSettings = userSettings.EmailSettings;
                        if (emailSettings == null) { return null; }
                        return new EmailSender(
                            emailSettings.ToAddress,
                            emailSettings.FromAddress,
                            emailSettings.FromName,
                            emailSettings.SmtpServer,
                            emailSettings.SmtpPort,
                            emailSettings.SmtpUserName,
                            emailSettings.SmtpPassword);
                    });

                    services.AddTransient<ActionHandler>();
                    services.AddHostedService<Daemon>();
                    services.AddHangfireServer();
                    services.AddHangfire(config => { config.UseMemoryStorage(
                        new MemoryStorageOptions() { FetchNextJobTimeout = TimeSpan.FromDays(7)}); // dont ever time out
                    });
                })
                .ConfigureLogging((hostingContext, logging) => {
                    var config = new LoggerConfiguration().ReadFrom.Configuration(hostingContext.Configuration).CreateLogger();
                    logging.ClearProviders();
                    logging.AddSerilog(config, dispose: true);
                    //logging.AddConsole();
                    //logging.AddDebug(); 
                });


        private static void SetEnvironmentVariables() {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            foreach (var (Variable, Value) in Constants.EnvironmentVariables) {
                Environment.SetEnvironmentVariable(Variable, Value);
            }
        }
    }
}