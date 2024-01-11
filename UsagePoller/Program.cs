#region Using statements
using CliWrap;
using Serilog;
using UsagePoller;
using UsagePoller.ActivityChecker;
#endregion

#region Catch install flag
const string serviceName = "GP SP Usage Polling";

if (args is { Length: 1 })
{
    try
    {
        string executablePath =
            Path.Combine(AppContext.BaseDirectory, "UsagePoller.exe");

        if (args[0] is "/Install")
        {
            await Cli.Wrap("sc.exe")
                .WithArguments(new[] { "create", serviceName, $"binPath={executablePath}", "start=auto" })
                .ExecuteAsync();
        }
        else if (args[0] is "/Uninstall")
        {
            await Cli.Wrap("sc.exe")
                .WithArguments(new[] { "stop", serviceName })
                .ExecuteAsync();

            await Cli.Wrap("sc.exe")
                .WithArguments(new[] { "delete", serviceName })
                .ExecuteAsync();
        }

        Environment.Exit(0);
        return;
    }
    catch (Exception ex) { Console.Write(ex); Environment.Exit(1); }
}
#endregion

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = serviceName;
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<WindowsBackgroundService>();
        services.AddSingleton<Checker>();
    })
    .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.FromLogContext())
    .Build();

await host.RunAsync();
