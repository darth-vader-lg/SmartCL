@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "%~dp0\publish.ps1 %*"
exit /b %ErrorLevel%
