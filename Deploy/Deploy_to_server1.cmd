@set SOURCE1=..\AllOnOnePage
@set SOURCE2=..\AllOnOnePage\bin\Debug\net8.0-windows
@set SOURCE3=..\DeployDirectory
@set SOURCE4=..\AllOnOnePage\bin\publish
@set DESTINATION=\\server1\Hausnet$\Dashboard5
@set COMMUNICATION_DIR=\\server1\Hausnet$\Dashboard5


REM use the local configuration for deployment to the dashboard
xcopy "%USERPROFILE%\Documents\All on one page\appsettings.hjson"   %DESTINATION% /D /Y


@call Deploy.cmd
