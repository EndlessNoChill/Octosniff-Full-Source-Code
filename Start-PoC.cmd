@echo off
setlocal
cd /d "%~dp0"

set "POC_LAUNCHER=%~dp0minor fix pCP.exe"
set "POC_TARGET=%~dp0app\sigh_Divine sniff.exe"

if not exist "%POC_LAUNCHER%" goto incomplete_package
if not exist "%POC_TARGET%" goto incomplete_package

if /i "%~1"=="--preflight" (
  echo Package preflight passed.
  echo Launcher: %POC_LAUNCHER%
  echo Target:   %POC_TARGET%
  endlocal & exit /b 0
)

"%POC_LAUNCHER%"
set "POC_EXIT=%ERRORLEVEL%"
if errorlevel 1 (
  echo.
  echo The security PoC failed.
  echo Send compatibility-report.txt and poc-run.log to the developers.
  pause
)
endlocal & exit /b %POC_EXIT%

:incomplete_package
echo.
echo This package is incomplete at:
echo   %~dp0
echo.
echo Do not run Start-PoC.cmd from inside the ZIP preview.
echo Right-click the ZIP, choose Extract All, open the extracted
echo "minor fix pCP" folder, and run Start-PoC.cmd there.
echo.
echo Required files:
echo   minor fix pCP.exe
echo   app\sigh_Divine sniff.exe
pause
endlocal & exit /b 2
