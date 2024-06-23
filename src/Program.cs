using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using Timer = System.Timers.Timer;


public class Program
{
    public static async Task Main(string[] args)
    {
        string TenantId = Environment.GetEnvironmentVariable("tenantId")!;
        string SubscriptionId = Environment.GetEnvironmentVariable("subscriptionId")!;
        string ResourceGroupName = Environment.GetEnvironmentVariable("rgName")!;
        string ZoneName = Environment.GetEnvironmentVariable("zoneName")!;
        string RecordSetName = Environment.GetEnvironmentVariable("recordsetName")!;
        string ClientId = Environment.GetEnvironmentVariable("clientId")!;
        string ClientSecret = Environment.GetEnvironmentVariable("clientSecret")!;
        int IntervalInMinuntes = Environment.GetEnvironmentVariable("intervalInMinutes") != null ? int.Parse(Environment.GetEnvironmentVariable("intervalInMinutes")!) : 5;

        while (true)
        {
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(TenantId, ClientId, ClientSecret);
            var dnsClient = new DnsManagementClient(serviceCreds) { SubscriptionId = SubscriptionId };

            using var httpClient = new HttpClient();
            string publicIp = await httpClient.GetStringAsync("http://checkip.amazonaws.com/");
            publicIp = publicIp.Trim();

            var recordSetParams = new RecordSet
            {
                TTL = 3600,
                ARecords = new[] { new ARecord(publicIp) }
            };

            RecordSet? record = null;

            try { record = await dnsClient.RecordSets.GetAsync(ResourceGroupName, ZoneName, RecordSetName, RecordType.A); } catch (Exception) { Console.WriteLine($"{DateTime.UtcNow.ToString()} UTC - Failed to fetch A-record"); };

            if (record != null)
            {
                string? currentIpInAzure = record.ARecords[0]?.Ipv4Address;
                if (currentIpInAzure == publicIp)
                {
                    Console.WriteLine($"{DateTime.UtcNow.ToString()} UTC - IP address has not changed. Checking again in {IntervalInMinuntes} minutes");
                }
                else
                {
                    Console.WriteLine($"{DateTime.UtcNow.ToString()} UTC - IP address has changed. Updating '{currentIpInAzure ?? string.Empty}' to '{publicIp}'...");
                    await dnsClient.RecordSets.CreateOrUpdateAsync(ResourceGroupName, ZoneName, RecordSetName, RecordType.A, recordSetParams);
                }
            }
            else
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString()} UTC - A-Record not found. Creating new A-record for IP address '{publicIp}'...");
                await dnsClient.RecordSets.CreateOrUpdateAsync(ResourceGroupName, ZoneName, RecordSetName, RecordType.A, recordSetParams);
            }

            Thread.Sleep(TimeSpan.FromMinutes(IntervalInMinuntes));
        }
    }
}
