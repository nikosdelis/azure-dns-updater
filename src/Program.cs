using AzureDnsUpdater;

var builder = Host.CreateApplicationBuilder(args);

// Allows the same binary to run as a plain console app, inside Docker, or as a Windows Service.
builder.Services.AddWindowsService(options => options.ServiceName = "Azure DNS Updater");

UpdaterOptions options;
try
{
    options = UpdaterOptions.FromConfiguration(builder.Configuration);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

builder.Services.AddSingleton(options);
builder.Services.AddHostedService<DnsUpdateWorker>();

await builder.Build().RunAsync();
return 0;
