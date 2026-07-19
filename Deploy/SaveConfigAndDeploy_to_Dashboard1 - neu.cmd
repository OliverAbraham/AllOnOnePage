@ECHO -----------------------------------------------------------------------------------
@ECHO.
@ECHO ABRAHAM DEPLOY
@ECHO.
@ECHO Oliver Abraham 2026, mail@oliver-abraham.de
@ECHO This program is hosted at http://www.github.com/oliverabraham/Deploy
@ECHO.
@ECHO -----------------------------------------------------------------------------------
@ECHO off

set AppName=Dashboard1
set CONFIGDIR=C:\Credentials\AllOnOnePage\Config_%AppName%
set SETTINGS_SOURCE=%USERPROFILE%\Documents\All on one page\*.hjson
xcopy "%SETTINGS_SOURCE%" "%CONFIGDIR%" /Y

call BackupAllSettings.cmd

deploy --config appsettings_%AppName%.json --push --wait

@choice /T 30 /C SN /D S /N /M "Dieses Fenster schliesst sich nach 30 Sekunden. S zum schliessen" 