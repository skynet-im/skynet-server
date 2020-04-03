#
# Dockerfile for SkynetServer project
#
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY SkynetServer.sln .editorconfig ./
COPY src/SkynetServer/SkynetServer.csproj src/SkynetServer/
COPY src/SkynetServer.Shared/SkynetServer.Shared.csproj src/SkynetServer.Shared/

RUN dotnet restore src/SkynetServer/SkynetServer.csproj

# Copy everything else and build
COPY src/ src/
RUN dotnet publish -c Release -o /app/out src/SkynetServer/SkynetServer.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT [ "dotnet", "SkynetServer.dll" ]
