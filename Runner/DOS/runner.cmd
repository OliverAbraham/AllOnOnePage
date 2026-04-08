@ECHO -----------------------------------------------------------------------------------
@ECHO
@ECHO CIRIDATA DESKTOP APPLICATION RUNNER
@ECHO
@ECHO Oliver Abraham 2023, mail@oliver-abraham.de
@ECHO This program is hosted at http://www.github.com/oliverabraham/desktopapplicationrunner
@ECHO
@ECHO -----------------------------------------------------------------------------------
@ECHO off

@ECHO Update source folder      : %ROOT% 
@ECHO IPC folder                : %COMMUNICATION%
@ECHO Application folder        : %DESTINATION%
@ECHO Bin source                : %BIN_SOURCE%
@ECHO Bin destination           : %BIN_DESTINATION%
@ECHO Settings source	        : %SETTINGS_SOURCE%
@ECHO Settings destination      : %SETTINGS_DESTINATION%
@ECHO Application(EXE) name     : %START_NAME%
@ECHO Application(process) name : %PROCESS_NAME%
@ECHO -----------------------------------------------------------------------------------
@ECHO off



:update
call :kill_the_application
call :copy_new_files
call :start_application


:infinite_loop
    FOR /L %%G IN (1,1,6) DO (call :wait_10_seconds "%%G")
    if not exist %COMMUNICATION%\* 							goto check_connection_and_reboot
    if exist %COMMUNICATION%\Force_reboot.dat 				goto reboot_computer
    if exist %COMMUNICATION%\Force_application_close.dat 	goto update_application
    if exist %BIN_DESTINATION%\restart-request.dat 			goto restart_application
    tasklist | find /N "%PROCESS_NAME%"
    if ERRORLEVEL 1 goto update_application
    goto infinite_loop


:check_connection_and_reboot
	echo no network connection, reboot pending in 30 seconds...
	call :wait_10_seconds
	if exist %COMMUNICATION%\* goto infinite_loop
	echo no network connection, reboot pending in 20 seconds...
	call :wait_10_seconds
	if exist %COMMUNICATION%\* goto infinite_loop
	echo no network connection, reboot pending in 10 seconds...
	call :wait_10_seconds
	if exist %COMMUNICATION%\* goto infinite_loop
	echo Rebooting now to restore
	goto reboot_computer


:reboot_computer
    echo Reboot is being done...
    shutdown -r
    echo . >%COMMUNICATION%\rebooting
    del     %COMMUNICATION%\Force_reboot.dat
    CHOICE /C:j /N /CS /T 120 /D j /M "waiting 120 seconds for reboot..."
    goto reboot_computer


:restart_application
    echo Application requested a hard restart.
    del %BIN_DESTINATION%\restart-request.dat
    taskkill /IM %PROCESS_NAME%
    CHOICE /C:j /N /CS /T 10 /D j /M "waiting 10 seconds ..."
    goto update


:wait_10_seconds
    if exist %COMMUNICATION%\Force_application_close.dat goto :eof
    if exist %BIN_DESTINATION%\restart-request.dat 		 goto :eof
    CHOICE /C:j /N /CS /T 10 /D j /M "waiting 10 seconds, press j to proceed now..."
    goto :eof


:copy_new_files
    echo .
    echo .
    if exist    %COMMUNICATION%\Force_reboot.dat        del %COMMUNICATION%\Force_reboot.dat
    if exist    %COMMUNICATION%\rebooting               del %COMMUNICATION%\rebooting

	if not exist %BIN_SOURCE%\* goto :copy_new_files_error
    echo copying new files...
    echo on
	del                                   "%BIN_DESTINATION_DELETE_BEFORE%" 		/S /Q
    xcopy %BIN_SOURCE%\*                  "%BIN_DESTINATION%" 						/s /Y /D
    echo copying new settings...
	del                                   "%SETTINGS_DESTINATION_DELETE_BEFORE%" 	/S /Q
    xcopy %SETTINGS_SOURCE%\*             "%SETTINGS_DESTINATION%" 					/s /Y /D
    echo off
    
    echo   .>%COMMUNICATION%\Application_is_updated.dat
    if exist %COMMUNICATION%\Force_application_close.dat del %COMMUNICATION%\Force_application_close.dat
    if exist %BIN_DESTINATION%\restart-request.dat       del %BIN_DESTINATION%\restart-request.dat
    goto :eof
:copy_new_files_error
    echo Copy error! No files in source bin directory or network is unreachable!
    goto :eof


:start_application
    echo on
    pushd %BIN_DESTINATION%
    start %START_NAME%
    popd
    echo off
    goto :eof


:kill_the_application
    CHOICE /C:j /N /CS /T 2 /D j /M "Killing the application process..."
    taskkill /IM %PROCESS_NAME%
    CHOICE /C:j /N /CS /T 10 /D j /M "waiting 10 seconds..."
    echo .>%COMMUNICATION%\Application_is_closed.dat
    goto :eof


:update_application
