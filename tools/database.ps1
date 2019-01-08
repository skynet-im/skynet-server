$mariaDbPath = Join-Path $env:LOCALAPPDATA MariaDB
$versionPath = Join-Path $mariaDbPath mariadb-10.3.12-x64
$binPath = Join-Path $versionPath bin

function Install-DbServer {
	if (Test-Path -Path $versionPath) {
		Write-Host "MariaDB is already installed and will be overridden if you continue."
	}
	$message = "Download and install?"
	$question = "The download size is 68.4 MiB. Do you want to proceed?"
	$choices = New-Object Collections.ObjectModel.Collection[Management.Automation.Host.ChoiceDescription]
	$choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList "&Yes"))
	$choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList "&No"))

	$decision = $Host.UI.PromptForChoice($message, $question, $choices, 1)
	if ($decision -eq 1) {
		return
	}

	$guid = [System.Guid]::NewGuid().ToString()
	$tempPath = Join-Path $env:TEMP $guid
	$downloadPath = Join-Path $env:TEMP ($guid + ".zip")
	$mirror = "http://ftp.hosteurope.de/mirror/mariadb.org//mariadb-10.3.12/winx64-packages/mariadb-10.3.12-winx64.zip"

	Start-BitsTransfer -Source $mirror -Destination $downloadPath -ErrorAction Stop
	Expand-Archive -Path $downloadPath -DestinationPath $tempPath
	if (Test-Path -Path $versionPath) {	Remove-Item -Path $versionPath -Recurse -Force }
	Move-Item -Path (Join-Path $tempPath mariadb-10.3.12-winx64) -Destination $versionPath -Force

	Remove-Item -Path $downloadPath
	Remove-Item -Path $tempPath
}

function Start-DbServer {
	$process = Get-Process -Name "mysqld" -ErrorAction SilentlyContinue
	if ($process) {
		Write-Host "A MariaDB server is already running on this system."
	} else {
		Start-Process -FilePath (Join-Path $binPath mysqld.exe) -ArgumentList "--standalone"
	}
}

function Stop-DbServer {
	Start-Process -FilePath (Join-Path $binPath mysqladmin.exe) -ArgumentList "-u root","shutdown"
}

function Start-DbCli {
	$mysql = Join-Path $binPath mysql.exe
	Start-Process -FilePath "cmd.exe" -ArgumentList "/c ""$mysql"" -u root Skynet" -WindowStyle Normal
}