namespace AzureDnsUpdater;

public sealed record UpdaterOptions(
    string TenantId,
    string SubscriptionId,
    string ResourceGroupName,
    string ZoneName,
    string RecordSetName,
    string ClientId,
    string ClientSecret,
    TimeSpan Interval,
    long TtlInSeconds,
    Uri PublicIpEndpoint)
{
    public static UpdaterOptions FromConfiguration(IConfiguration configuration)
    {
        var missing = new List<string>();

        string Required(string key)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                missing.Add(key);
                return string.Empty;
            }
            return value.Trim();
        }

        var options = new UpdaterOptions(
            TenantId: Required("tenantId"),
            SubscriptionId: Required("subscriptionId"),
            ResourceGroupName: Required("rgName"),
            ZoneName: Required("zoneName"),
            RecordSetName: Required("recordsetName"),
            ClientId: Required("clientId"),
            ClientSecret: Required("clientSecret"),
            Interval: TimeSpan.FromMinutes(configuration.GetValue("intervalInMinutes", 5)),
            TtlInSeconds: configuration.GetValue("ttlInSeconds", 3600L),
            PublicIpEndpoint: new Uri(configuration["publicIpEndpoint"] ?? "https://checkip.amazonaws.com/"));

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required configuration: {string.Join(", ", missing)}. " +
                "Set them as environment variables, in appsettings.json, or as --key=value arguments.");
        }

        if (options.Interval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("intervalInMinutes must be greater than zero.");
        }

        return options;
    }
}
