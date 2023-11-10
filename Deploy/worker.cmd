@ECHO -----------------------------------------------------------------------------------
@ECHO
@ECHO CIRIDATA DESKTOP APPLICATION RUNNER
@ECHO
@ECHO Oliver Abraham 2023, mail@oliver-abraham.de
@ECHO This program is hosted at http://www.github.com/oliverabraham/desktopapplicationrunner
@ECHO
@ECHO -----------------------------------------------------------------------------------
@ECHO off

set BIN_DESTINATION=%ROOT%\bin
set SETTINGS_DESTINATION=%ROOT%\settings
set COMMUNICATION=%ROOT%\ipc

@ECHO Root folder                : %ROOT%
@ECHO Bin source folder          : %BIN_SOURCE%
@ECHO Settings source folder     : %SETTINGS_SOURCE%
@ECHO Settings destination folder: %SETTINGS_DESTINATION%
@ECHO Bin destination folder     : %BIN_DESTINATION%
@ECHO IPC folder                 : %COMMUNICATION%
@ECHO -----------------------------------------------------------------------------------
rem pause


echo .
echo Cleanup
del %COMMUNICATION%\Force_application_close.dat
del %COMMUNICATION%\Application_is_updated.dat


echo .
echo First copy all files...
del                                     "%BIN_DESTINATION%\*" 	    /S /Q
xcopy "%BIN_SOURCE%"  	    		    "%BIN_DESTINATION%" 	    /s /Y
del                                     "%SETTINGS_DESTINATION%\*" 	/S /Q
xcopy "%SETTINGS_SOURCE%"               "%SETTINGS_DESTINATION%" 	/s /Y


echo .
echo Send a message to the runner on the client, to end the running application
echo .>%COMMUNICATION%\Force_application_close.dat


echo .
echo Wait until the runner has (1)ended the app (2)copied the new files and (3)restarted the app
:wait
@CHOICE /T 2 /M "Waiting until the update is finished...  press 2 to skip waiting" /C:123 /CS /D 1
IF ERRORLEVEL 2 GOTO dontwait
if not exist %COMMUNICATION%\Application_is_updated.dat goto wait
:dontwait


echo .
echo Cleanup
if exist  %COMMUNICATION%\Force_application_close.dat   del %COMMUNICATION%\Force_application_close.dat
if exist  %COMMUNICATION%\Application_is_updated.dat	del %COMMUNICATION%\Application_is_updated.dat
if exist  %COMMUNICATION%\Application_is_closed.dat	    del %COMMUNICATION%\Application_is_closed.dat
rem pause