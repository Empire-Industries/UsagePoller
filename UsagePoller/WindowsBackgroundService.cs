using Serilog;
using System.Data;
using Microsoft.Extensions.Hosting.Internal;
using UsagePoller.ActivityChecker;
using UsagePoller.ServiceHelpers;

namespace UsagePoller
{
    public class WindowsBackgroundService : BackgroundService
    {
        private readonly Checker _activityChecker;

        private readonly ILogger<WindowsBackgroundService> _logger;

        public WindowsBackgroundService(Checker activityChecker, ILogger<WindowsBackgroundService> logger) => (this._activityChecker, this._logger) = (activityChecker, logger);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _activityChecker.LoadSettings(Environment.GetEnvironmentVariable("SETTINGS_FILE") ?? Path.Combine(AppContext.BaseDirectory, "UsagePollerSettings.json"));
                _logger.LogInformation("Started GP SP Usage Poller in {Environment} environment on {Hostname} with the following settings file: {SettingsFile}:\n{SettingsJson}", Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), System.Net.Dns.GetHostName(), Environment.GetEnvironmentVariable("SETTINGS_FILE") ?? "UsagePollerSettings.json",  _activityChecker.GetPublicSettings());

                while (!stoppingToken.IsCancellationRequested)
                {
                    DataTable activityDetails = _activityChecker.GetActivityDetails();
                    DataRow[] longSessions = _activityChecker.GetLongSessions(activityDetails);

                    _activityChecker.UpdateActivityLog(activityDetails);
                    if (longSessions.Length > 0)
                    {
                        _logger.LogInformation("Found {SessionCount} sessions that have been idle longer than {SessionThreshold} minute threshold and not previously logged.\n\n{SessionData}", longSessions.Length, _activityChecker.GetMaxIdleSessionLength(), Helpers.ParseSessionDetails(longSessions));
                        _activityChecker.EmailAllLongSessions(longSessions, _activityChecker.GetLongReportEmailAddress());
                        _activityChecker.EmailLongUsers(longSessions);
                        if (Environment.GetEnvironmentVariable("DELETE_LONG_SESSIONS") != "false")
                        {
                            _activityChecker.DeleteLongIdleSessions(longSessions, _logger);
                        }

                    }
                    await Task.Delay(TimeSpan.FromSeconds(_activityChecker.GetSleepTime()), stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("GP SP Usage Poller exited on {Hostname}", System.Net.Dns.GetHostName());
                await Log.CloseAndFlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on {Hostname} \n{Message}", System.Net.Dns.GetHostName(), ex.Message);
                await Log.CloseAndFlushAsync();
                throw new ApplicationException();
            }
        }
    }
}