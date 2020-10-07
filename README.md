# Skynet server #

[![Build Status](https://dev.azure.com/vectordata/skynet/_apis/build/status/skynet-im.skynet-server?branchName=master)](https://dev.azure.com/vectordata/skynet/_build/latest?definitionId=4&branchName=master)

> General project documentation such as the protocol specification can be found in our [home repository](https://github.com/skynet-im/skynet).

### ⚠ Archive Notice ⚠ ###
This project is not actively developed anymore. It is still kept as reference for other projects.
Developing a messaging application from scratch with an own protocol is simply too much work for two students in their freetime.+
Information about our plans for the future of Skynet will be published in the [home repository](https://github.com/skynet-im/skynet).

### Projects ###
The Skynet server solution contains three projects:
- `SkynetServer` - .NET Core console application with a TLS socket or a management CLI
- `SkynetServer.Shared` - .NET Core class library with database and model implementation
- `SkynetServer.Web` - ASP.NET Core MVC for mail address verification

Enums, packets, etc. are implemented in the [Skynet libraries](https://github.com/skynet-im/skynet-dotnet) and referenced as NuGet packages.

Tools for setting up a database server on Windows have been moved to the [PSModules](https://github.com/daniel-lerch/psmodules) repository.

### Certificate ###
Several steps need to be taken to generate a new certificate for Skynet:
1. `openssl ecparam -name prime256v1 -out prime256v1.pem`
2. `openssl genpkey -paramfile prime256v1.pem -out key.pem`
3. `openssl req -nodes -x509 -key key.pem -out cert.cer -subj "/C=DE/ST=Hessen/CN=Skynet Messenger" -addext "subjectAltName = DNS:skynet.lerchen.net" -days 365`
4. `openssl pkcs12 -export -out cert.pfx -inkey key.pem -in cert.cer`
