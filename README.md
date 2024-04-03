# Azure DNS Updater

This whole project is inspired by this AWESOME [tutorial by **Jussi Roine**](https://jussiroine.com/2019/06/building-a-simple-and-secure-dns-updater-for-azure-dns-using-raspberry-pi-and-azure-functions/). I'm not that fond of Powershell and I know that I am not alone there. Therefore I pretty much copied the whole project and wrote it in plain C#.

## Description

Long story short, this project helps you get rid of the headache that **dynamic IP address** is. Give that you use [**Azure DNS**](https://azure.microsoft.com/en-gb/products/dns) for your DNS needs, this project will update any record-set for you with your current **external** IP address. The external IP address is fetched by a simple *GET* request to [**http://checkip.amazonaws.com/**](http://checkip.amazonaws.com/) (lol).

## Requirements

Obviously you need access to an Azure Subscription as well as a [*Service Principal*](https://learn.microsoft.com/en-us/entra/identity-platform/app-objects-and-service-principals?tabs=browser). The Service Principal needs to have the **DNS Zone Contributor** role, to be allowed to upsert recordsets.

A more detailed explanation of the parameters needed:
- TenantId: The ID of your Azure Tenant.
- SubscipritonId: The ID of your Azure Subscription.
- RgName: The name of the resource group where your DNS Zone is created.
- ZoneName: The name of your DNS Zone (e.g. something.com).
- RecordSetName: The A record (e.g www, or any subdomain). This doesn't need to exist before running.
- ClientID: From your Service Principal
- ClientSecret: From your Service Principal
- IntervalInMinutes (*optional*): Integer represetning how often (in minutes) the record will be updated (if changed). Default value is 5.


## Installation (local copy)

Build (run the following command while in the same directory as Dockerfile)
```
docker build -t dns-updater .
```
Run
``` 
docker run -d -e tenantId=XXXXX -e subscriptionId=XXXXX -e rgName=XXXXX -e zoneName=XXXXX -e recordsetName=XXXXX -e clientId=XXXXX -e clientSecret=XXXXX -e intervalInMinutes=5 dns-updater
```

## Installation (remote image)

*coming soon...*