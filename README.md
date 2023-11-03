# Abraham.AllOnOnePage

## OVERVIEW

AllOnOnePage is a WPF Dashboard application that can display values from various sources:
MQTT brokers, PRTG server, Openweathermap.com, Email postboxes, Google calenders, Excel files, and FinTX bank accounts.

- It is meant to run as a fullscreen dashboard application on a dedicated PC or tablet.
- You can also run it as a windowed application on your desktop, having your important data on one page.
- Have the current weather, time and your bank account balance side by side. :-)
- You have an excel file with important data? Display individual cells on your dashboard!
- You can monitor your backups, using my tool "BackupServerDaemon"
- You can display appointments from your google calendar.


## LICENSE

Licensed under Apache licence.
https://www.apache.org/licenses/LICENSE-2.0


## Compatibility

The nuget package was build with DotNET 6.



## INSTALLATION



## INTEGRATION WITH HOME AUTOMATION SYSTEMS

If you want to use an MQTT broker, I recommend using Mosquitto. It's quite easy to install and configure.
You can find it here: https://mosquitto.org/
I'm using it inside Homeassistant. It can be installed as an add-on to homeassistant.
Homeassistant can be found here: https://www.home-assistant.io/




## AUTHOR

Oliver Abraham, mail@oliver-abraham.de, https://www.oliver-abraham.de

Please feel free to comment and suggest improvements!



## SOURCE CODE

The source code is hosted at:

https://github.com/OliverAbraham/Abraham.AllOnOnePage



## SCREENSHOTS

This shows the Producer demo sending a value to topic "garden/temperature":
![](Screenshots/screenshot1.jpg)

This shows the Broker in Homeassistant receiving the value:
![](Screenshots/screenshot2.jpg)

This shows the Subscriber demo receiving the values (after 2 executions of Publisher demo):
![](Screenshots/screenshot3.jpg)


# MAKE A DONATION !

If you find this application useful, buy me a coffee!
I would appreciate a small donation on https://www.buymeacoffee.com/oliverabraham
