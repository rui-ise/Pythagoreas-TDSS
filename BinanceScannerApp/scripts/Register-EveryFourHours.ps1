<#
Registers a Windows scheduled task that launches the published scanner every four hours.

Before running this script, publish the project from Visual Studio in Release mode and
replace $applicationPath below with the full path to BinanceScannerApp.exe.
#>

$applicationPath = "C:\Path\To\BinanceScannerApp.exe"
$taskName = "Binance Market Scanner - Four Hour Scan"

if (-not (Test-Path -LiteralPath $applicationPath)) {
    throw "Set `$applicationPath to the published BinanceScannerApp.exe file first."
}

$action = New-ScheduledTaskAction -Execute $applicationPath
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1)
$trigger.RepetitionInterval = (New-TimeSpan -Hours 4)
$trigger.RepetitionDuration = (New-TimeSpan -Days 3650)

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Description "Opens the read-only Binance Market Scanner every four hours." -Force
Write-Host "Created '$taskName'. The app will launch every four hours."
