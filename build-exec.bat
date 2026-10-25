@echo off
setlocal

call "%~dp0build.bat" -Run %*
exit /b %errorlevel%
