# 
# Dockerfile for SkynetServer.Web project
#
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY Skynet.Server.sln nuget.config .editorconfig ./
COPY src/Skynet.Server.Shared/Skynet.Server.Shared.csproj src/Skynet.Server.Shared/
COPY src/Skynet.Server.Web/Skynet.Server.Web.csproj src/Skynet.Server.Web/

RUN dotnet restore src/Skynet.Server.Web/Skynet.Server.Web.csproj

# Copy everything else and build
COPY src/ src/
RUN dotnet publish -c Release -o /app/out src/Skynet.Server.Web/Skynet.Server.Web.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT [ "dotnet", "Skynet.Server.Web.dll" ]
