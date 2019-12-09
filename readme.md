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

## Requirements

 - Dotnet core 3 runtime (including desktop apps for windows)
 - Windows or linux OS supported by dotnet core.

## Quickstart
Inputshare can be run two ways; with a GUI or in the command line (to run in background). This section covers using the UI. For starting from the command line, see [Command line and start arguments](https://github.com/sbarrac1/Inputshare/wiki/Command-line-and-start-arguments). The current UI is just a mock-up using avaloniaUI and will be changed.

To get started, start Inputshare on the PC that has the keyboard and mouse that you want to share and select start server. Then run Inputshare on the client(s) that you want to share the keyboard and mouse with and select client and connect to the server.

![enter image description here](https://i.imgur.com/gIdqZMz.png)

### Assigning edges

The client 'localhost' represents the server PC. To set a client to an edge of another client, select the client that you want to assign the edge to, then use the dropdown boxes to select the client to assign to the edge. The server will automatically set the opposite edge of the target client to the selected client. For example, if you had a PC to the left of the server, you could assign the PC to the left edge of localhost. This would allow you to simply move the mouse to the left of your screen to switch keyboard and mouse input to the PC, doing so would also allow you to switch back to the server by moving the mouse to the right of the PCs screen.

### Assigning hotkeys

Hotkeys can be assigned to clients by selecting the modifier keys that you want, then clicking on the hotkey button (Displaying F2 in the above image), the next pressed key will then be assigned to the client along with the selected modifiers. For example to assign Alt+Ctrl+F to a client, you would check the Alt and Ctrl checkboxes, then click the hotkey button and press the F key. The hotkey is only set after the button is clicked and a key is pressed. Function hotkeys are assigned the same way by using the function hotkey list at the bottom of the window.

## Using the windows service client
The Inputshare windows service allows much more functionality for clients. The service runs without a logged in user, meaning that It can be running from startup and can be used to log in. The service automatically connects to the last connected server, meaning that a client can be restarted and reconnected with no direct interaction. The service also runs in the background requiring to user interface except for connecting/disconnecting from servers etc, which is done from the Inputshare UI.

The service also allows the server to send alt+ctrl+delete to access the windows SAS (secure attention sequence) screen (registry edit required).

### Installing the service
The windows service must be installed before use, which is done via CMD. To install the service, run command prompt as admin and enter the command 

'sc create Inputshareservice binpath= "path/to/inputshareservice.exe" start= auto'

The service can then be started with 'sc start inputshareservice'

### Allowing alt+ctrl+delete
By default, windows does not allow services or programs to sent alt+ctrl+delete, however this can be changed with a registry tweak. 

Create the DWORD key SoftwareSASGeneration in HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System and set the value to 1. This will allow the service to use the [SendSAS](https://docs.microsoft.com/en-us/windows/win32/api/sas/nf-sas-sendsas) function.
### Connecting to  a server
To connect to service to a server, start Inputshare and select the windows service option. This should allow you to connect to a server and enter a client name. When the service is started/restarted, it will automatically keep trying to reconnect to the last used server, this is useful as clients will automatically reconnect after a reboot.
