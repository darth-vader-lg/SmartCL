@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "%~dp0\build.ps1"
exit /b %ErrorLevel%
