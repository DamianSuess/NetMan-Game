@echo off
setlocal

set "NETMAN_POWERSHELL=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
where pwsh.exe >nul 2>&1
if not errorlevel 1 set "NETMAN_POWERSHELL=pwsh.exe"

"%NETMAN_POWERSHELL%" -NoLogo -NoProfile -File "%~dp0build.ps1" %*
exit /b %errorlevel%
