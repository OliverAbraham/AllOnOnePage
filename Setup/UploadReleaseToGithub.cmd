@rem ----------------------------------------------------------------------------------------------
@title Upload Setup and zip to Github
@rem ----------------------------------------------------------------------------------------------
@echo off



rem ----------------------------------------------------------------------------------------------
rem Set parameters
rem Starting point for all is the file Version.cs
rem Edit the new version in this file, then build the application
rem ----------------------------------------------------------------------------------------------

rem this will set the variables BINDIR and PUBLISHDIR
call setbindir.cmd

rem this file was created by my tool "CreateVersionForDeploy.exe"
call setinstallerversion.cmd

set INSTALLERFILENAME="%BINDIR%\AllOnOnePage_Setup_english_%VERSION2%.exe"
set ARCHIVEFILENAME="%BINDIR%\AllOnOnePage_%VERSION2%.zip"
set REPOSITORY=oliverabraham/AllOnOnePage


echo ----------------------------------------------------------------------------------------------
echo Parameters:
echo ----------------------------------------------------------------------------------------------
echo Version is                : %VERSION2%
echo Source directory          : %BINDIR%\publish
echo 1. Generated Zip  is      : %ARCHIVEFILENAME%
echo 2. Installer      is      : %INSTALLERFILENAME%
echo Upload to repo            : %REPOSITORY%
choice /C yn /N /M "Is the version correct? (y/n)"
if errorlevel 2 goto end



echo ----------------------------------------------------------------------------------------------
echo Login to Github
echo ----------------------------------------------------------------------------------------------

echo on
gh auth login --with-token < C:\Credentials\GithubAccessToken.txt
gh auth status
echo off

choice /C yn /N /M "Is the login correct? (y/n)"



echo ----------------------------------------------------------------------------------------------
echo List existing releases
echo ----------------------------------------------------------------------------------------------

echo on
gh release list --repo %REPOSITORY%
echo off

choice /C yn /N /M "Is the list correct? (y/n)"



echo ----------------------------------------------------------------------------------------------
echo Create new package
echo ----------------------------------------------------------------------------------------------

echo Creating the release
gh release create %VERSION2% --repo %REPOSITORY%  --title %VERSION2% --latest --notes %VERSION2%

echo Uploading the zip
gh release upload %VERSION2%    %ARCHIVEFILENAME%   --clobber --repo %REPOSITORY%

echo Uploading the exe
gh release upload %VERSION2%    %INSTALLERFILENAME% --clobber --repo %REPOSITORY%

gh release list --repo %REPOSITORY%

echo Release was created .
pause
