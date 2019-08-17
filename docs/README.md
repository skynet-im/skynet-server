# Skynet server documentation #

> General project documentation such as the protocol specification can be found in our [home repository](https://github.com/skynet-im/skynet).

The Skynet server solution contains four projects:
- `SkynetServer` - .NET Core console application with VSL socket implementation
- `SkynetServer.Cli` - .NET Core console application for management and debugging
- `SkynetServer.Shared` - .NET Core class library with database and model implementation
- `SkynetServer.Web` - ASP.NET Core MVC for mail address verification

Tools:
- `database.ps1` - Allows the installation of a local MySQL or MariaDB server for development purposes.
Please install the PowerShell Extension for Visual Studio and load the script `. .\tools\database.ps1`.  
Functions:
  - `Install-DbServer`
  - `Start-DbServer`
  - `Stop-DbServer`
