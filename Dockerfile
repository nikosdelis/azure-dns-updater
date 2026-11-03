FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between source changes
COPY src/azure-dns-updater.csproj ./
RUN dotnet restore azure-dns-updater.csproj

COPY src/ ./
RUN dotnet publish azure-dns-updater.csproj -c Release -o /app --no-restore

# Runtime image: plain .NET runtime, no ASP.NET needed
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
USER app
ENTRYPOINT ["dotnet", "azure-dns-updater.dll"]
