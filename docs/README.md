# Skynet server documentation #

The Skynet server solution contains three projects:
- `SkynetServer` - .NET Core console application with VSL socket implementation
- `SkynetServer.Shared` - .NET Core class library with database and model implementation
- `SkynetServer.Web` - ASP.NET Core Razor Pages for mail address verification

Tools:
- `database.ps1` - Allows the installation of a local MariaDB server for development purposes.
Please install the PowerShell Extension for Visual Studio and load the script `. .\tools\database.ps1`.  
Functions:
  - `Install-DbServer`
  - `Start-DbServer`
  - `Stop-DbServer`

## Verification process ##
`SkynetServer` sends an e-mail with a verification URL which looks like https://api.skynet-messenger.com/confirm/bkhtgqwnwxjhrrxt.  
`SkynetServer.Web` responds to `GET` requests to this URL with a website including a form. If JavaScript is enabled, it will automatically submit this form.
The form sends a `POST` request to the URL to verify the mail address. The webserver directly responds to the `POST` request with a page stating _Successfully activated_.