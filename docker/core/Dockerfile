#
# Dockerfile for SkynetServer project
#
FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY SkynetServer.sln .
COPY NuGet.config .
COPY src/SkynetServer/SkynetServer.csproj src/SkynetServer/
COPY src/SkynetServer.Cli/SkynetServer.Cli.csproj src/SkynetServer.Cli/
COPY src/SkynetServer.Shared/SkynetServer.Shared.csproj src/SkynetServer.Shared/

RUN dotnet restore src/SkynetServer/SkynetServer.csproj \
&& dotnet restore src/SkynetServer.Cli/SkynetServer.Cli.csproj

# Copy everything else and build
COPY src/ src/
RUN dotnet publish -c Release -o /app/out src/SkynetServer/SkynetServer.csproj \
&& dotnet publish -c Release -o /app/out src/SkynetServer.Cli/SkynetServer.Cli.csproj

# Build runtime image
FROM microsoft/dotnet:2.1-runtime AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT [ "dotnet", "SkynetServer.dll" ]