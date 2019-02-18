# 
# Dockerfile for SkynetServer.Web project
#
FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY SkynetServer.sln .
COPY NuGet.config .
COPY src/SkynetServer.Shared/SkynetServer.Shared.csproj src/SkynetServer.Shared/
COPY src/SkynetServer.Web/SkynetServer.Web.csproj src/SkynetServer.Web/

RUN dotnet restore src/SkynetServer.Web/SkynetServer.Web.csproj

# Copy everything else and build
COPY src/ src/
RUN dotnet publish -c Release -o /app/out src/SkynetServer.Web/SkynetServer.Web.csproj

# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT [ "dotnet", "SkynetServer.Web.dll" ]
