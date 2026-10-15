@echo off
rem Builds HideBar.exe with the C# compiler included in Windows (.NET Framework 4.x).
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 ^
  /win32icon:assets\HideBar.ico /resource:assets\HideBar.ico,HideBar.ico ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /out:HideBar.exe /recurse:src\*.cs
if errorlevel 1 exit /b 1
echo Built HideBar.exe
