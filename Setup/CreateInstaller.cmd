@rem ----------------------------------------------------------------------------------------------
@title Create installer for AllOnOnePage and upload to homepage
@rem ----------------------------------------------------------------------------------------------
@echo off

rem Location of the NSIS package (Nullsoft Scriptable Installer System)
set NSISDIR=C:\GIT\AllOnOnePage\Setup\NSIS

set OUTPUTDIR1="\\server1\temp"



rem ----------------------------------------------------------------------------------------------
rem Set parameters
rem Starting point for all is the file Version.cs
rem Edit the new version in this file, then build the application
rem ----------------------------------------------------------------------------------------------

rem this will set the variables BINDIR and PUBLISHDIR
call setbindir.cmd

rem this file was created by my tool "CreateVersionForDeploy.exe"
call setinstallerversion.cmd

call C:\Credentials\SetArchiver.cmd

set INSTALLER="AllOnOnePage_Setup_english.exe"
set INSTALLERFILENAME="%BINDIR%\AllOnOnePage_Setup_english_%VERSION2%.exe"
set ARCHIVEFILENAME="%BINDIR%\AllOnOnePage_%VERSION2%.zip"


echo ----------------------------------------------------------------------------------------------
echo Parameters:
echo ----------------------------------------------------------------------------------------------
echo Archiver (7zip)           : %ARCHIVER%
echo Setup creator             : %NSISDIR%
echo.                          
echo.                          
echo Version is                : %VERSION2%
echo Source directory          : %BINDIR%\publish
echo 1. Generated Zip  will be : %ARCHIVEFILENAME%
echo 2. Installer      will be : %INSTALLERFILENAME%
echo 3. Outputdir      will be : %OUTPUTDIR1%
choice /C yn /N /M "Is the version correct? (y/n)"
if errorlevel 2 goto end



echo ----------------------------------------------------------------------------------------------
echo Create a copy of the publish dir and create a zip file out of it for Github release
echo ----------------------------------------------------------------------------------------------
del    "%BINDIR%\AllOnOnePage"                          /S /Q >NUL
xcopy  "%BINDIR%\publish\*"  "%BINDIR%\AllOnOnePage"    /S >NUL
%ARCHIVER%  a -r   "%ARCHIVEFILENAME%"   %QUELLE% 


echo ----------------------------------------------------------------------------------------------
echo Create the installer
echo ----------------------------------------------------------------------------------------------
del %INSTALLER%
del %INSTALLERFILENAME%
NSIS\makensis.exe AllOnOnePage.nsi
echo Created installer : %INSTALLER%
echo moving to         : %INSTALLERFILENAME%
copy "AllOnOnePage_Setup_english.exe"  %INSTALLERFILENAME%
del  "AllOnOnePage_Setup_english.exe"
echo.
echo.



echo ----------------------------------------------------------------------------------
echo Copy the installer to temp dir for QA
echo ----------------------------------------------------------------------------------
xcopy %INSTALLERFILENAME% "%OUTPUTDIR1%" /Y
echo copied to Temp "%OUTPUTDIR1%"
echo.
echo.
set ARCHIVEFILENAME=%BINDIR%\AllOnOnePage_%VERSION2%.zip


echo ----------------------------------------------------------------------------------------------
echo Summary:
echo ----------------------------------------------------------------------------------------------
echo 1. Generated ZIP      : %ARCHIVEFILENAME%
echo 2. Generated Installer: %INSTALLERFILENAME%
echo 3. Installer copied to: %OUTPUTDIR1%

pause
:end