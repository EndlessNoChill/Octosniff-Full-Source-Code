$ErrorActionPreference = 'Stop'

$pocRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$pocExe = [System.IO.Path]::GetFullPath((Join-Path $pocRoot 'app\sigh_Divine sniff.exe'))

$stopped = 0
Get-CimInstance Win32_Process -Filter "Name='sigh_Divine sniff.exe'" | ForEach-Object {
    $candidate = [System.IO.Path]::GetFullPath($_.ExecutablePath)
    if ($candidate -ieq $pocExe) {
        Stop-Process -Id $_.ProcessId
        $stopped++
    }
}

Write-Host "Stopped $stopped isolated sigh_Divine sniff PoC process(es)."
