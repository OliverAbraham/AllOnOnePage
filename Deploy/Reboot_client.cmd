@set DESTINATION=\\server1\Hausnet$\Dashboard3
@set COMMUNICATION_DIR=\\server1\Hausnet$\Dashboard3

echo . >%COMMUNICATION_DIR%\Force_application_close.dat
echo . >%COMMUNICATION_DIR%\Force_reboot.dat
del     %COMMUNICATION_DIR%\rebooting