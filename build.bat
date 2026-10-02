@echo off
rem Baut die App. Ergebnis im Ordner installer: Setup-EXE, Installieren.bat, Portable.bat, portable\
setlocal
cd /d "%~dp0"

if not exist "data\app.ico" powershell -NoProfile -ExecutionPolicy Bypass -File "data\make-icon.ps1" || goto :fail

echo [1/2] Kompiliere ...
dotnet build TegraRcmSmashGUI.csproj -c Release -o "dist\portable" --nologo -v q || goto :fail

echo [2/2] Baue Installer ...
set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
"%ISCC%" /Q "installer\setup.iss" || goto :fail
if not exist "installer\portable" mkdir "installer\portable"
copy /y "dist\portable\RcmGui.exe" "installer\portable\" >nul || goto :fail
rem Treiber-Installer neben die App legen (App ruft ihn bei Bedarf auf)
copy /y "installer\wdi-simple.exe" "dist\portable\" >nul
copy /y "installer\wdi-simple.exe" "installer\portable\" >nul

echo.
echo Fertig:
echo   installer\RcmGui_Setup_1.0.0.exe
echo   installer\Installieren.bat  - installiert die App
echo   installer\Portable.bat      - startet die portable Version
exit /b 0

:fail
echo Build fehlgeschlagen.
exit /b 1
