@echo off
setlocal

set "ROOT=%~dp0"
set "GAME_EXE=%ROOT%Builds\Windows\ApocalypseKingUnity3D.exe"

if not exist "%GAME_EXE%" (
    echo [ERROR] Cannot find game executable:
    echo "%GAME_EXE%"
    echo.
    echo Please build it from Unity first:
    echo Apocalypse King / Build Windows Player
    pause
    exit /b 1
)

pushd "%ROOT%"
start "" "%GAME_EXE%" %*
popd

endlocal

