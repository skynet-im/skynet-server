# 
# Dockerfile for SkynetServer.Web project
#
FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ../src/*/*.csproj .
RUN dotnet restore

# Copy everything else and build
COPY ../src .
RUN dotnet publish -c Release -o /app/out

# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT [ "dotnet", "SkynetServer.Web.dll" ]
