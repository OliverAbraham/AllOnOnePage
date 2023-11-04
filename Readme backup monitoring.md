# Abraham.AllOnOnePage

## Monitoring backups with AllOnOnePage and BackupServerDaemon

AllOnOnePage is a WPF Dashboard application that can display values from various sources.
This article describes how to monitor your backups.
You can use my tool BackupServerDaemon. It is also hosted on github.
It can be found here: https://github.com/OliverAbraham/BackupServerDaemon

## CONCEPT

The concept behind this is that you have a server or NAS that stores your backups.
For resiliance against ransomware this is a separate machine that is not accessible from your clients.
The machine does not expose any file shares, but instead will pull data from your file server actively. 
So you need a daemon/client on that machine to check your backups.


## SETUP

I have written BackupServerDaemon, a command line tool that can be used to check your backups.
You can run it as a docker container or as a standalone application.
This tool will monitor directories that contain you backup data.
For each monitored folder, it will search for the newest file in root and send the age of this file to an MQTT broker.
The age is sent in a free format, that you can decide.
I am sending these values: OK,1w,2w,3w,4w,old. (2w means 2 weeks old)
With AllOnOnePage you can then display this value on a dashboard. 
You can set a warning color for certain values. I am using green for "OK" and a red one for the rest.



## AUTHOR

Oliver Abraham, mail@oliver-abraham.de, https://www.oliver-abraham.de

Please feel free to comment and suggest improvements!



## SOURCE CODE

The source code is hosted at:

https://github.com/OliverAbraham/Abraham.AllOnOnePage

https://github.com/OliverAbraham/Abraham.BackupServerDaemon




# MAKE A DONATION !

If you find this application useful, buy me a coffee!
I would appreciate a small donation on https://www.buymeacoffee.com/oliverabraham
