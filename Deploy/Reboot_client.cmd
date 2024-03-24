@set DESTINATION=\\server1\Dashboard5
@set COMMUNICATION_DIR=\\server1\Dashboard5

echo . >%COMMUNICATION_DIR%\Force_application_close.dat
echo . >%COMMUNICATION_DIR%\Force_reboot.dat
del     %COMMUNICATION_DIR%\rebooting