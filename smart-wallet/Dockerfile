FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

ADD ob_sandbox_root_ca.cer /usr/local/share/ca-certificates/ob_sandbox_root_ca.cer

RUN chmod 644 /usr/local/share/ca-certificates/ob_sandbox_root_ca.cer && update-ca-certificates

ADD ob_sandbox_issuing_ca.cer /usr/local/share/ca-certificates/ob_sandbox_issuing_ca.cer

RUN chmod 644 /usr/local/share/ca-certificates/ob_sandbox_issuing_ca.cer && update-ca-certificates

# Copy csproj and restore as distinct layers
COPY smart-wallet/*.csproj ./

RUN dotnet restore .

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "smart-wallet.dll"]