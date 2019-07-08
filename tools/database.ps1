$mysqlName = "mysql-5.7.26-winx64"
$mariadbName = "mariadb-10.4.6-winx64"
$installPath = Join-Path $PSScriptRoot "bin"

function Install-DbServer {
	[CmdletBinding()]
	param(
		[Parameter()]
		[switch]$MySQL,
		[Parameter()]
		[switch]$MariaDB
	)

	begin {
		# We require mirrors to support HTTPS
		$mysqlMirror = "https://cdn.mysql.com//Downloads/MySQL-5.7/mysql-5.7.26-winx64.zip"
		$mariadbMirror = "https://mirrors.ukfast.co.uk/sites/mariadb//mariadb-10.4.6/winx64-packages/mariadb-10.4.6-winx64.zip"

		function InstallDbServer ($Mirror, $Name) {
			$guid = [System.Guid]::NewGuid().ToString()
			$tempPath = Join-Path $env:TEMP $guid
			$downloadPath = Join-Path $env:TEMP ($guid + ".zip")
			$versionPath = Join-Path $installPath $Name

			$webResponse = Invoke-WebRequest -Uri $Mirror -Method Head -UseBasicParsing -ErrorAction Stop
			$fileSize = [System.Math]::Round(($webResponse.Headers["Content-Length"] / 1024 / 1024), 1)
			$message = "Download and install?"
			$question = "The download size is $fileSize MiB. Do you want to proceed?"
			$choices = New-Object Collections.ObjectModel.Collection[Management.Automation.Host.ChoiceDescription]
			$choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList "&Yes"))
			$choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList "&No"))
		
			$decision = $Host.UI.PromptForChoice($message, $question, $choices, 1)
			if ($decision -eq 1) {
				return
			}
			
			Write-Host "Downloading archive..."
			Start-BitsTransfer -Source $Mirror -Destination $downloadPath -ErrorAction Stop
			Write-Host "Unpacking archive..."
			Expand-Archive -Path $downloadPath -DestinationPath $tempPath
			if (!(Test-Path -PathType Container -Path $installPath)) { New-Item -ItemType Directory -Force -Path $installPath | Out-Null }
			if (Test-Path -Path $versionPath) {	Remove-Item -Path $versionPath -Recurse -Force }
			Move-Item -Path (Join-Path $tempPath $Name) -Destination $versionPath -Force

			Write-Host "Cleaning up..."
			# Do not rely on temporary files not to be deleted in the meantime
			Remove-Item -Path $downloadPath -ErrorAction SilentlyContinue
			Remove-Item -Path $tempPath -ErrorAction SilentlyContinue
		}

		function PurgeDbServer ($Path) {
			if (Test-Path -Path $Path -PathType Container) {
				$message = "Remove old database server?"
				$question = "An outdated database server was found at $Path. Do you want to remove it?"
				$choices = New-Object Collections.ObjectModel.Collection[Management.Automation.Host.ChoiceDescription]
				$choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList "&Yes"))
				$choices.Add((New-Object Management.Automation.Host.ChoiceDescription -ArgumentList "&No"))
			
				$decision = $Host.UI.PromptForChoice($message, $question, $choices, 1)
				if ($decision -eq 0) {
					Remove-Item -Path $Path -Recurse
				}
			}
		}
	}
	process {
		$mysqlInstalled = Test-Path -Path (Join-Path $installPath $mysqlName)
		$mariadbInstalled = Test-Path -Path (Join-Path $installPath $mariadbName)
		Write-Host "Installed database servers:"
		if ($mysqlInstalled) {
			Write-Host "MySQL:    $mysqlName"
		} else {
			Write-Host "MySQL:    ---"
		}
		if ($mariadbInstalled) {
			Write-Host "MariaDB:  $mariadbName"
		} else {
			Write-Host "MariaDB:  ---"
		}
		Write-Host ""
		Write-Host "Already installed versions will be overridden"
		Write-Host ""

		if ($MySQL) {
			InstallDbServer -Mirror $mysqlMirror -Name $mysqlName
		}
		if ($MariaDB) {
			InstallDbServer -Mirror $mariadbMirror -Name $mariaName
		}

		PurgeDbServer -Path (Join-Path $env:LOCALAPPDATA MariaDB)
	}
}

function Start-DbServer {
	[CmdletBinding()]
	param(
		[Parameter()]
		[switch]$MySQL,
		[Parameter()]
		[switch]$MariaDB
	)

	if (($MySQL -and $MariaDB) -or (-not $MySQL -and -not $MariaDB)) {
		Write-Host "Decide for one database server to start!"
		return
	}
	if (Get-Process -Name "mysqld" -ErrorAction SilentlyContinue) {
		Write-Host "A MySQL/MariaDB server is already running on this system."
		return
	}

	if ($MySQL) {
		$serverPath = Join-Path (Join-Path (Join-Path $installPath $mysqlName) "bin") "mysqld.exe"
		if (Test-Path -Path $serverPath -PathType Leaf) {
			$firstRun = !(Test-Path -Path (Join-Path (Join-Path $installPath $mysqlName) "data") -PathType Container)
			if ($firstRun) {
				Start-Process -FilePath $serverPath -ArgumentList `
					"--console","--skip-log-syslog","--explicit-defaults-for-timestamp", `
					"--initialize-insecure" -Wait
			}
			Start-Process -FilePath $serverPath -ArgumentList `
				"--console","--skip-log-syslog","--explicit-defaults-for-timestamp","--transaction-isolation=READ-COMMITTED"
			if ($firstRun) {
				$execPath = GetPathWhenRunning -Name "mysqladmin.exe"
				if ($execPath) {
					Start-Process -FilePath $execPath -ArgumentList "-uroot","password root"
				}
			}
		} else {
			Write-Host "MySQL was not found at $serverPath"
		}
	}

	if ($MariaDB) {
		$binPath = Join-Path (Join-Path $installPath $mariadbName) "bin"
		$serverPath = Join-Path $binPath "mysqld.exe"
		$initPath = Join-Path $binPath "mysql_install_db.exe"
		if ((Test-Path -Path $serverPath -PathType Leaf) -and (Test-Path -Path $initPath -PathType Leaf)) {
			if (!(Test-Path -Path (Join-Path (Join-Path (Join-Path $installPath $mariadbName) "data") "my.ini") -PathType Leaf)) {
				Start-Process -FilePath $initPath -ArgumentList `
					"--password=root" -Wait
			}
			Start-Process -FilePath $serverPath -ArgumentList `
				"--console","--transaction-isolation=READ-COMMITTED"
		} else {
			Write-Host "MariaDB was not found $serverPath"
		}
	}
}

function Stop-DbServer {
	$execPath = GetPathWhenRunning -Name "mysqladmin.exe"
	if ($execPath) {
		Start-Process -FilePath $execPath -ArgumentList "-uroot","-proot","shutdown"
	}
}

function Start-DbCli {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory = $false, Position = 0)]
		[string]$Database
	)
	$execPath = GetPathWhenRunning -Name "mysql.exe"
	if ($execPath) {
		if (-not [string]::IsNullOrWhiteSpace($Database)) {
			$Database = " $Database"
		}
		Start-Process -FilePath "cmd.exe" -ArgumentList "/c ""$execPath"" -uroot -proot$Database" -WindowStyle Normal
	}
}

function GetPathWhenRunning ($Name) {
	$process = Get-Process -Name "mysqld" -ErrorAction SilentlyContinue
	if ($process) {
		$binPath = Split-Path -Path $process.Path -Parent
		$execPath = Join-Path $binPath $Name
		if (Test-Path -Path $execPath -PathType Leaf) {
			return $execPath
		} else {
			"Could not find executable at $execPath"
		}
	} else {
		Write-Host "No MySQL/MariaDB server is currently running on this system."
	}
}
