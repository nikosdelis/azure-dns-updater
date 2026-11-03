using System.Net;
using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.Dns.Models;

namespace AzureDnsUpdater;

public sealed class DnsUpdateWorker(UpdaterOptions options, ILogger<DnsUpdateWorker> logger) : BackgroundService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting. Zone '{Zone}', record '{Record}', checking every {Interval}",
            options.ZoneName, options.RecordSetName, options.Interval);

        var credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        var armClient = new ArmClient(credential, options.SubscriptionId);
        var zoneId = DnsZoneResource.CreateResourceIdentifier(options.SubscriptionId, options.ResourceGroupName, options.ZoneName);
        var aRecords = armClient.GetDnsZoneResource(zoneId).GetDnsARecords();

        using var timer = new PeriodicTimer(options.Interval);

        do
        {
            try
            {
                await UpdateOnceAsync(aRecords, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Update failed. Retrying in {Interval}", options.Interval);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        logger.LogInformation("Stopping");
    }

    private async Task UpdateOnceAsync(DnsARecordCollection aRecords, CancellationToken cancellationToken)
    {
        var publicIp = await GetPublicIpAsync(cancellationToken);

        DnsARecordResource? existing = null;
        try
        {
            existing = await aRecords.GetAsync(options.RecordSetName, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            // Record does not exist yet; it will be created below.
        }

        if (existing is not null)
        {
            var current = existing.Data.DnsARecords.FirstOrDefault()?.IPv4Address;

            if (current is not null && current.Equals(publicIp))
            {
                logger.LogInformation("IP address {Ip} has not changed", publicIp);
                return;
            }

            logger.LogInformation("IP address has changed from '{Old}' to '{New}'. Updating...", current, publicIp);
        }
        else
        {
            logger.LogInformation("A-record '{Record}' not found. Creating it with IP address '{Ip}'...", options.RecordSetName, publicIp);
        }

        var data = new DnsARecordData { TtlInSeconds = options.TtlInSeconds };
        data.DnsARecords.Add(new DnsARecordInfo { IPv4Address = publicIp });

        await aRecords.CreateOrUpdateAsync(WaitUntil.Completed, options.RecordSetName, data, cancellationToken: cancellationToken);

        logger.LogInformation("A-record '{Record}.{Zone}' now points to {Ip}", options.RecordSetName, options.ZoneName, publicIp);
    }

    private async Task<IPAddress> GetPublicIpAsync(CancellationToken cancellationToken)
    {
        var body = (await HttpClient.GetStringAsync(options.PublicIpEndpoint, cancellationToken)).Trim();

        if (!IPAddress.TryParse(body, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new InvalidOperationException($"'{options.PublicIpEndpoint}' did not return a valid IPv4 address: '{body}'");
        }

        return ip;
    }
}
