@ECHO -----------------------------------------------------------------------------------
@ECHO
@ECHO CIRIDATA DESKTOP APPLICATION RUNNER
@ECHO
@ECHO Oliver Abraham 2023, mail@oliver-abraham.de
@ECHO This program is hosted at http://www.github.com/oliverabraham/desktopapplicationrunner
@ECHO
@ECHO -----------------------------------------------------------------------------------
@ECHO off

set CONFIGDIR=C:\Credentials\AllOnOnePage\Config_Dashboard3
set BIN_SOURCE=..\AllOnOnePage\bin\publish\*
set SETTINGS_SOURCE=%USERPROFILE%\Documents\All on one page\*.hjson
set ROOT=\\server1\Dashboard3

copy "%CONFIGDIR%\appsettings.hjson" "%USERPROFILE%\Documents\All on one page"

