@echo off
title Deploy to Web server
cls

@echo -------------------------------------------------------
@echo Parameter setzen

set FTP_URL=www.oliver-abraham.de
set FTP_USERNAME=ftp263677-deploy2
set FTP_PASSWORD=ZUc@67oYN2yJwL@W
set ARCHIVER="C:\Program Files\7-zip\7z"
set PASSWORD=
set QUELLE=bin\publish\*
set VERSIONSINFORMATION_ZIELDATEI=version.html

pushd "C:\TFS\AllOnOnePage\AllOnOnePage\"

set VERSION=0000-00-00
if exist    bin\Debug\netcoreapp3.1\setversion.cmd   call   bin\Debug\netcoreapp3.1\setversion.cmd
ECHO Programmversion ist %VERSION%
ECHO Programmversion ist %VERSION2%
set ZIELDATEI_WEBSITE=AllOnOnePage_%VERSION2%.zip
set ZIELDATEI=bin\%ZIELDATEI_WEBSITE%




@echo -------------------------------------------------------
@echo Versions-Infodatei aktualisieren

copy %VERSIONSINFORMATION_ZIELDATEI% bin\version.sav.html
echo ^<a href="https://www.abraham-beratung.de/aoop/%ZIELDATEI_WEBSITE%"^>Update auf Version %VERSION2%^</a^> ^<br /^>   >%VERSIONSINFORMATION_ZIELDATEI%
type bin\version.sav.html >>%VERSIONSINFORMATION_ZIELDATEI%



@echo -------------------------------------------------------
@echo ZIP-Datei erzeugen

%ARCHIVER%  a -r %ZIELDATEI%   %QUELLE% 
@echo.
@echo.




@echo -------------------------------------------------------
@echo Upload-Script erzeugen
@echo -------------------------------------------------------

if exist %TEMP%\DeployHelperScript.txt   del %TEMP%\DeployHelperScript.txt   	>NUL
echo open %FTP_URL%>>%TEMP%\DeployHelperScript.txt
echo %FTP_USERNAME%>>%TEMP%\DeployHelperScript.txt
echo %FTP_PASSWORD%>>%TEMP%\DeployHelperScript.txt
echo put %VERSIONSINFORMATION_ZIELDATEI%>>%TEMP%\DeployHelperScript.txt
echo binary>>%TEMP%\DeployHelperScript.txt
echo put %ZIELDATEI%>>%TEMP%\DeployHelperScript.txt
echo quit>>%TEMP%\DeployHelperScript.txt
@echo.
@echo.


@echo -------------------------------------------------------
@echo Upload von %ZIELDATEI%
@echo -------------------------------------------------------
ftp -s:%TEMP%\DeployHelperScript.txt
rem del %TEMP%\DeployHelperScript.txt >NUL
set FTP_URL= >NUL
set FTP_USERNAME= >NUL
set FTP_PASSWORD= >NUL
set ARCHIVER= >NUL
set PASSWORD= >NUL
popd
