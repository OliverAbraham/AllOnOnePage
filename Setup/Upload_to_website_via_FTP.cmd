@rem ----------------------------------------------------------------------------------------------
@title Deploy to Web server
@rem ----------------------------------------------------------------------------------------------
@echo off


@rem ----------------------------------------------------------------------------------------------
@rem Set parameters
@rem Starting point for all is the file Version.cs
@rem Edit the new version in this file, then build the application
@rem ----------------------------------------------------------------------------------------------

rem this will set variables FTP_URL, FTP_USERNAME and FTP_PASSWORD:
call C:\Credentials\AllOnOnePageCredentials.cmd

rem this will set variable ARCHIVER to the 7zip location
call C:\Credentials\SetArchiver.cmd

rem this will set the variables BINDIR and PUBLISHDIR
call setbindir.cmd

rem this file was created by my tool "CreateVersionForDeploy.exe"
call setinstallerversion.cmd

set ARCHIVEWEBSITE=AllOnOnePage_%VERSION2%.zip
set ARCHIVEFILENAME=bin\%ARCHIVEWEBSITE%

set VERSION_INFORMATION_FILE="%BINDIR%\version.html"
set VERSION_INFORMATION_FILE_SAV="%BINDIR%\version.html.sav"

set INSTALLERFILENAME="%BINDIR%\AllOnOnePage_Setup_english_%VERSION2%.exe"




echo ----------------------------------------------------------------------------------------------
echo Parameters:
echo ----------------------------------------------------------------------------------------------
echo Version2                                : %VERSION2%
echo Version info file                       : %VERSION_INFORMATION_FILE%
echo Version info file                       : %VERSION_INFORMATION_FILE_SAV%
echo BIN dir (version info file)             : %BINDIR%
echo PUBLISH dir (source directory for us)   : %PUBLISHDIR%
echo Installer (existing)                    : %INSTALLERFILENAME%
echo Archiver is                             : %ARCHIVER%
echo This archive will be created            : %ARCHIVEWEBSITE%
echo This is the link for the website        : %ARCHIVEFILENAME%

choice /C yn /N /M "Are all parameters correct? (y/n)"
if errorlevel 2 goto end



rem ----------------------------------------------------------------------------------------------
echo Updating version info file
rem ----------------------------------------------------------------------------------------------
copy   %VERSION_INFORMATION_FILE%   %VERSION_INFORMATION_FILE_SAV%
echo ^<a href="https://www.abraham-beratung.de/aoop/%ARCHIVEWEBSITE%"^>Update auf Version %VERSION2%^</a^> ^<br /^>   >%VERSION_INFORMATION_FILE%
type %VERSION_INFORMATION_FILE_SAV% >>%VERSION_INFORMATION_FILE%
echo.
echo.



rem ----------------------------------------------------------------------------------
rem Verify the version info file
rem ----------------------------------------------------------------------------------
echo Please verify this linklist:
type %VERSION_INFORMATION_FILE%
choice /C yn /N /M "Is the list correct? (y/n)"
if errorlevel 2 goto end
echo.
echo.



rem ----------------------------------------------------------------------------------------------
echo Creating the zip file
rem ----------------------------------------------------------------------------------------------
%ARCHIVER%  a -r %ARCHIVEFILENAME%   %QUELLE% 
echo.
echo.




rem ----------------------------------------------------------------------------------------------
echo FTP upload of %ARCHIVEFILENAME%
echo FTP upload of %INSTALLERFILENAME%
rem ----------------------------------------------------------------------------------------------
if exist %TEMP%\DeployHelperScript.txt   del %TEMP%\DeployHelperScript.txt   	>NUL
echo open %FTP_URL%>>%TEMP%\DeployHelperScript.txt
echo %FTP_USERNAME%>>%TEMP%\DeployHelperScript.txt
echo %FTP_PASSWORD%>>%TEMP%\DeployHelperScript.txt
echo put %VERSION_INFORMATION_FILE%>>%TEMP%\DeployHelperScript.txt
echo binary>>%TEMP%\DeployHelperScript.txt
echo put %ARCHIVEFILENAME%>>%TEMP%\DeployHelperScript.txt
echo put %INSTALLERFILENAME%>>%TEMP%\DeployHelperScript.txt
echo quit>>%TEMP%\DeployHelperScript.txt

ftp -s:%TEMP%\DeployHelperScript.txt




rem ----------------------------------------------------------------------------------------------
rem Cleanup
rem ----------------------------------------------------------------------------------------------
del %TEMP%\DeployHelperScript.txt >NUL
set FTP_URL= >NUL
set FTP_USERNAME= >NUL
set FTP_PASSWORD= >NUL
set ARCHIVER= >NUL
:end