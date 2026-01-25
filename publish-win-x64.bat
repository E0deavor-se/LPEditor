@echo off
setlocal
cd /d "%~dp0"
dotnet publish "LPEditorApp\LPEditorApp.csproj" -p:PublishProfile=win-x64-selfcontained
if errorlevel 1 (
  echo.
  echo Publish failed.
  pause
  exit /b 1
)
echo.
echo Publish completed.
echo Output: LPEditorApp\bin\Release\net8.0\win-x64\publish
pause
