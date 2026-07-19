@ECHO -----------------------------------------------------------------------------------
@ECHO.
@ECHO ABRAHAM DEPLOY
@ECHO.
@ECHO Oliver Abraham 2026, mail@oliver-abraham.de
@ECHO This program is hosted at http://www.github.com/oliverabraham/Deploy
@ECHO.
@ECHO -----------------------------------------------------------------------------------
@ECHO off

@set DESTINATION=\\server1\Dashboard5
@set COMMUNICATION_DIR=\\server1\Dashboard5

echo . >%COMMUNICATION_DIR%\Force_application_close.dat
echo . >%COMMUNICATION_DIR%\Force_reboot.dat
del     %COMMUNICATION_DIR%\rebooting