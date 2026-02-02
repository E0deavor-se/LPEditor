@echo off
setlocal
cd /d "%~dp0"
dotnet publish "LPEditorApp.Desktop\LPEditorApp.Desktop.csproj" -c Release -r win-x64 -p:SelfContained=true
if errorlevel 1 (
  echo.
  echo Publish failed.
  pause
  exit /b 1
)
echo.
echo Publish completed.
echo Output: LPEditorApp.Desktop\bin\Release\net8.0-windows\win-x64\publish
pause
