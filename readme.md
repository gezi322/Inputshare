# Inputshare

Inputshare allows you to use one keyboard and mouse across multiple PCs. Inputshare currently runs on Linux & windows using dotnet core. This project is still unfinished but progress is being made.

Inputshare allows you to:
 - quickly switch between PCs by moving the cursor onto the screen
 - Switch between PCs with using hotkeys
 - Share clipboard between PCs (Text,images,files)
 - Drag and drop between PCs (Text,Images,Files)

## Current progress 
|  | Windows | Linux (X11) | MacOS |
--|--|-- |--|
| Keyboard & mouse sharing | Fully Working | Fully Working| Not implemented
| Clipboard sharing | Fully working | Working but can't paste files (only copy) | Not implemented
| Drag & drop support | Fully working | Not yet implemented | Not implemented

## Default hotkeys

| Send alt+ctrl+delete | ctrl+alt+P       |   |   |   |
|----------------------|------------------|---|---|---|
| Stop server          | ctrl+alt+shift+Q |   |   |   |

## Requirements

 - Dotnet core 3 runtime (including desktop apps for windows)
 - Windows or linux OS supported by dotnet core.

### Installing the service
The windows service must be installed before use, which is done via CMD. To install the service, run command prompt as admin and enter the command 

'sc create Inputshareservice binpath= "path/to/inputshareservice.exe" start= auto'

The service can then be started with 'sc start inputshareservice'

### Allowing alt+ctrl+delete
By default, windows does not allow services or programs to sent alt+ctrl+delete, however this can be changed with a registry tweak. 

Create the DWORD key SoftwareSASGeneration in HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System and set the value to 1. This will allow the service to use the [SendSAS](https://docs.microsoft.com/en-us/windows/win32/api/sas/nf-sas-sendsas) function.
### Connecting to  a server
To connect to service to a server, start Inputshare and select the windows service option. This should allow you to connect to a server and enter a client name. When the service is started/restarted, it will automatically keep trying to reconnect to the last used server, this is useful as clients will automatically reconnect after a reboot.
