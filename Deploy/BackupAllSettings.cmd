set ALLSETTINGS=C:\Credentials\AllOnOnePage
set BACKUPTO=\\server1\Archiv\_Backups\AllOnOnePage


for /f "tokens=1,2" %%u in ('date /t') do set d=%%u 
for /f "tokens=1" %%u in ('time /t') do set t=%%u 
if "%t:~1,1%"==":" set t=0%t% 
set timestr=%t:~0,2%%t:~3,2% 
set datestr=%d:~6,4%%d:~3,2%%d:~0,2% 
set TIMESTAMP=%datestr:~0,8%%timestr:~0,4%
echo Timestamp=%TIMESTAMP%

"C:\Program Files\7-Zip\7z.exe" u -r   "%BACKUPTO%\AllOnOnePage-Settings_%TIMESTAMP%.7z"   %ALLSETTINGS%
