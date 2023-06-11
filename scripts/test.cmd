@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "%~dp0\test.ps1"
exit /b %ErrorLevel%
