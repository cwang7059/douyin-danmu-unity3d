@echo off
setlocal

set "ROOT=%~dp0"
set "PANEL_DIR=%ROOT%tools\control-panel"
set "PACKAGE_FILE=%PANEL_DIR%\package.json"
set "ELECTRON_EXE=%PANEL_DIR%\node_modules\electron\dist\electron.exe"

if not exist "%PACKAGE_FILE%" (
    echo [ERROR] Cannot find control panel package:
    echo "%PACKAGE_FILE%"
    pause
    exit /b 1
)

where node >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Node.js is not installed or not in PATH.
    echo Please install Node.js first, then run this script again.
    pause
    exit /b 1
)

where npm >nul 2>nul
if errorlevel 1 (
    echo [ERROR] npm is not installed or not in PATH.
    pause
    exit /b 1
)

pushd "%PANEL_DIR%"

if not exist "%ELECTRON_EXE%" (
    echo [INFO] Installing control panel dependencies...
    call npm install
    if errorlevel 1 (
        echo [ERROR] Failed to install dependencies.
        popd
        pause
        exit /b 1
    )
)

echo [INFO] Starting mobile control panel...
call npm start
set "EXIT_CODE=%ERRORLEVEL%"

popd

if not "%EXIT_CODE%"=="0" (
    echo [ERROR] Control panel exited with code %EXIT_CODE%.
    pause
)

endlocal
exit /b %EXIT_CODE%
