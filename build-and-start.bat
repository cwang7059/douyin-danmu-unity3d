@echo off
setlocal

set "ROOT=%~dp0"
set "SCRIPT=%ROOT%build-and-start.ps1"

if not exist "%SCRIPT%" (
    echo [ERROR] Cannot find build script:
    echo "%SCRIPT%"
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%

