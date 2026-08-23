@echo off
setlocal
set "GAME=%~dp0BalloonRush.exe"
if not exist "%GAME%" (
  echo BalloonRush.exe was not found next to this launcher.
  pause
  exit /b 1
)
start "Balloon Rush" "%GAME%" -screen-width 1080 -screen-height 1920 -screen-fullscreen 1
endlocal
