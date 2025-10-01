@echo off
REM SMTP to Graph Relay Service - Installation Helper
REM Run this script as Administrator

echo ========================================
echo SMTP to Graph Relay Service Installer
echo ========================================
echo.

REM Check for administrator privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click and select "Run as Administrator"
    pause
    exit /b 1
)

set SERVICE_NAME=SMTP to Graph Relay
set EXE_PATH=%~dp0SMTP Service.exe

echo Current directory: %~dp0
echo Executable: %EXE_PATH%
echo.

:menu
echo What would you like to do?
echo.
echo 1. Install Service
echo 2. Start Service
echo 3. Stop Service
echo 4. Restart Service
echo 5. Uninstall Service
echo 6. Check Service Status
echo 7. Open Configuration (Tray App)
echo 8. View Logs
echo 9. Add Firewall Rule
echo 0. Exit
echo.

set /p choice="Enter your choice (0-9): "

if "%choice%"=="1" goto install
if "%choice%"=="2" goto start
if "%choice%"=="3" goto stop
if "%choice%"=="4" goto restart
if "%choice%"=="5" goto uninstall
if "%choice%"=="6" goto status
if "%choice%"=="7" goto config
if "%choice%"=="8" goto logs
if "%choice%"=="9" goto firewall
if "%choice%"=="0" goto end
goto menu

:install
echo.
echo Installing service...
sc create "%SERVICE_NAME%" binPath= "%EXE_PATH%" start= auto DisplayName= "%SERVICE_NAME%"
if %errorLevel% equ 0 (
    echo Service installed successfully!
    sc description "%SERVICE_NAME%" "Relays SMTP emails to Microsoft 365 via MS Graph API"
) else (
    echo Failed to install service. Error code: %errorLevel%
)
echo.
pause
goto menu

:start
echo.
echo Starting service...
sc start "%SERVICE_NAME%"
if %errorLevel% equ 0 (
    echo Service started successfully!
) else (
    echo Failed to start service. Error code: %errorLevel%
)
echo.
pause
goto menu

:stop
echo.
echo Stopping service...
sc stop "%SERVICE_NAME%"
if %errorLevel% equ 0 (
    echo Service stopped successfully!
) else (
    echo Failed to stop service. Error code: %errorLevel%
)
echo.
pause
goto menu

:restart
echo.
echo Restarting service...
sc stop "%SERVICE_NAME%"
timeout /t 3 /nobreak >nul
sc start "%SERVICE_NAME%"
if %errorLevel% equ 0 (
    echo Service restarted successfully!
) else (
    echo Failed to restart service. Error code: %errorLevel%
)
echo.
pause
goto menu

:uninstall
echo.
echo WARNING: This will remove the service completely!
set /p confirm="Are you sure? (Y/N): "
if /i "%confirm%"=="Y" (
    echo Stopping service...
    sc stop "%SERVICE_NAME%"
    timeout /t 2 /nobreak >nul
    echo Uninstalling service...
    sc delete "%SERVICE_NAME%"
    if %errorLevel% equ 0 (
        echo Service uninstalled successfully!
    ) else (
        echo Failed to uninstall service. Error code: %errorLevel%
    )
) else (
    echo Uninstall cancelled.
)
echo.
pause
goto menu

:status
echo.
echo Checking service status...
sc query "%SERVICE_NAME%"
echo.
pause
goto menu

:config
echo.
echo Opening configuration interface...
start "" "%EXE_PATH%" --tray
echo Configuration app started in system tray.
echo.
pause
goto menu

:logs
echo.
echo Opening logs directory...
if exist "%~dp0logs" (
    explorer "%~dp0logs"
) else (
    echo Logs directory not found. Service may not have run yet.
)
echo.
pause
goto menu

:firewall
echo.
echo Adding firewall rule for SMTP port 25...
netsh advfirewall firewall add rule name="SMTP Relay Service" dir=in action=allow protocol=TCP localport=25
if %errorLevel% equ 0 (
    echo Firewall rule added successfully!
) else (
    echo Failed to add firewall rule. Error code: %errorLevel%
)
echo.
pause
goto menu

:end
echo.
echo Exiting...
exit /b 0
