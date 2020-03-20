# Skynet server documentation #

> General project documentation such as the protocol specification can be found in our [home repository](https://github.com/skynet-im/skynet).

### Projects ###
The Skynet server solution contains four projects:
- `SkynetServer` - .NET Core console application with VSL socket implementation
- `SkynetServer.Cli` - .NET Core console application for management and debugging
- `SkynetServer.Shared` - .NET Core class library with database and model implementation
- `SkynetServer.Web` - ASP.NET Core MVC for mail address verification

### Tools ###
- `database.ps1` - Allows the installation of a local MySQL or MariaDB server for development purposes.
Please install the PowerShell Extension for Visual Studio and load the script `. .\tools\database.ps1`.  
Functions:
  - `Install-DbServer`
  - `Start-DbServer`
  - `Stop-DbServer`

### Certificate ###
Several steps need to be taken to generate a new certificate for Skynet:
1. `openssl ecparam -name prime256v1 -out prime256v1.pem`
2. `openssl genpkey -paramfile prime256v1.pem -out key.pem`
3. `openssl req -nodes -new -x509 -key key.pem -out cert.cer -days 365`
4. `openssl pkcs12 -export -out cert.pfx -inkey key.pem -in cert.cer`
