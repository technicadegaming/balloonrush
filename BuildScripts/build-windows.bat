@echo off
setlocal

rem Set UNITY_EXE in the environment to override this default editor path.
if not defined UNITY_EXE set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.0.82f1\Editor\Unity.exe"
set "PROJECT_PATH=%~dp0.."
set "LOG_PATH=%PROJECT_PATH%\Builds\balloonrush-build.log"

if not exist "%PROJECT_PATH%\Builds" mkdir "%PROJECT_PATH%\Builds"

if not exist "%UNITY_EXE%" (
  echo Unity editor not found at:
  echo %UNITY_EXE%
  echo Edit UNITY_EXE in this file.
  exit /b 1
)

"%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_PATH%" ^
  -executeMethod BalloonRush.Editor.BalloonRushProjectBuilder.BuildWindowsCabinetCommandLine ^
  -logFile "%LOG_PATH%"

set "EXIT_CODE=%ERRORLEVEL%"
if not "%EXIT_CODE%"=="0" (
  echo Build failed. See:
  echo %LOG_PATH%
  exit /b %EXIT_CODE%
)

echo Build complete: %PROJECT_PATH%\Builds\Windows\BalloonRush.exe
endlocal
