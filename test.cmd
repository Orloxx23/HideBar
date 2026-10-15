@echo off
rem Builds and runs the tests with the C# compiler included in Windows (.NET Framework 4.x).
rem They cover src\Core, which doesn't touch Windows, against the fakes in tests\.
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

"%CSC%" /nologo /target:exe /codepage:65001 /out:tests\HideBar.Tests.exe src\Core\*.cs tests\*.cs
if errorlevel 1 exit /b 1
tests\HideBar.Tests.exe
