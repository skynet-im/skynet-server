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
	$execPath = AssertExistsAndNotRunning mysqld.exe
	if ($execPath) {
		Start-Process -FilePath $execPath -ArgumentList "--standalone"
	}
}

function Stop-DbServer {
	$execPath = AssertExistsAndRunning mysqladmin.exe
	if ($execPath) {
		Start-Process -FilePath $execPath -ArgumentList "-u root","shutdown"
	}
}

function Start-DbCli {
	$execPath = AssertExistsAndRunning mysql.exe
	if ($execPath) {
		Start-Process -FilePath "cmd.exe" -ArgumentList "/c ""$execPath"" -u root Skynet" -WindowStyle Normal
	}
}

function AssertExistsAndRunning($execName) {
	$execPath = Join-Path $binPath $execName
	if (Test-Path -Path $execPath -PathType Leaf) {
		$process = Get-Process -Name "mysqld" -ErrorAction SilentlyContinue
		if ($process) {
			return $execPath
		} else {
			Write-Host "No MariaDB server is currently running on this system."
		}
	} else {
		Write-Host "Could not find executable at ""$execPath""."
	}
}

function AssertExistsAndNotRunning($execName) {
		$execPath = Join-Path $binPath $execName
	if (Test-Path -Path $execPath -PathType Leaf) {
		$process = Get-Process -Name "mysqld" -ErrorAction SilentlyContinue
		if ($process) {
			Write-Host "A MariaDB server is already running on this system."
		} else {
			return $execPath
		}
	} else {
		Write-Host "Could not find executable at ""$execPath""."
	}
}