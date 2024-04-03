FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

# Copy everything
COPY src/ ./

# Restore as distinct layers
RUN dotnet restore azure-dns-updater.csproj

# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /src
COPY --from=build-env /src/out .
ENTRYPOINT ["dotnet", "azure-dns-updater.dll"]

ENV Logging__Console__FormatterName=Simple