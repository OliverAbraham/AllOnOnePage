@ECHO -----------------------------------------------------------------------------------
@ECHO.
@ECHO CIRIDATA DESKTOP APPLICATION RUNNER
@ECHO.
@ECHO Oliver Abraham 2023, mail@oliver-abraham.de
@ECHO This program is hosted at http://www.github.com/oliverabraham/desktopapplicationrunner
@ECHO.
@ECHO -----------------------------------------------------------------------------------
@ECHO off


set ROOT=\\server1\Dashboard5
set COMMUNICATION=%ROOT%\ipc
set BIN_SOURCE=%ROOT%\bin
set SETTINGS_SOURCE=%ROOT%\settings

set DESTINATION=C:\Dashboard
set BIN_DESTINATION=%DESTINATION%\bin
set BIN_DESTINATION_DELETE_BEFORE=%DESTINATION%\bin\*
set SETTINGS_DESTINATION=%USERPROFILE%\Documents\All On One Page
set SETTINGS_DESTINATION_DELETE_BEFORE=%USERPROFILE%\Documents\All On One Page\*.hjson
set START_NAME=AllOnOnePage.exe
set PROCESS_NAME=AllOnOnePage.exe


:create_directories_if_this_is_the_first_start
if not exist %DESTINATION%\* 		md %DESTINATION%
if not exist %BIN_DESTINATION%\* 	md %BIN_DESTINATION%
if not exist %SETTINGS_DESTINATION%\* 	md %SETTINGS_DESTINATION%

:loop
pushd .
rem Update the runner, if necessary (edge case)
xcopy %ROOT%\*.cmd    %DESTINATION%  /Y /D
call runner.cmd
popd
goto loop
