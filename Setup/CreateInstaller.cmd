@echo off
rem ----------------------------------------------------------------------------------
title Create installer for AllOnOnePage and upload to homepage
rem ----------------------------------------------------------------------------------

rem Location of the NSIS package (Nullsoft Scriptable Installer System)
set NSISDIR=C:\GIT\AllOnOnePage\Setup\NSIS

set OUTPUTDIR1="\\server1\temp"



@rem ----------------------------------------------------------------------------------------------
@rem Set parameters
@rem Starting point for all is the file Version.cs
@rem Edit the new version in this file, then build the application
@rem ----------------------------------------------------------------------------------------------
call setinstallerversion.cmd
set INSTALLER="AllOnOnePage_Setup_english.exe"
set DESTINATIONFILE="AllOnOnePage_Setup_english_%VERSION2%.exe"



echo ----------------------------------------------------------------------------------------------
echo Parameters:
echo ----------------------------------------------------------------------------------------------
echo Version is         : %VERSION2%
echo Installer temp     : %INSTALLER%
echo Installer  will be : %DESTINATIONFILE%
echo Outputdir          : %OUTPUTDIR1%
choice /C yn /N /M "Is the version correct? (y/n)"
if errorlevel 2 goto end



echo ----------------------------------------------------------------------------------------------
echo Create the installer
echo ----------------------------------------------------------------------------------------------
del %INSTALLER%
del %DESTINATIONFILE%
NSIS\makensis.exe AllOnOnePage.nsi
echo Created installer : %INSTALLER%
echo Renaming to       : %DESTINATIONFILE%
ren   "AllOnOnePage_Setup_english.exe"  %DESTINATIONFILE%
echo.
echo.



echo ----------------------------------------------------------------------------------
echo Copy the installer to temp dir for QA
echo ----------------------------------------------------------------------------------
xcopy %DESTINATIONFILE% "%OUTPUTDIR1%" /Y
echo copied to Temp "%OUTPUTDIR1%"
echo.
echo.

:end