readme

# Inputshare #

The goal of inputshare is to make using multiple computers at the same time easier. Inputshare allows you to:

* Seamlessly switch mouse and keyboard input between computers
* Share a global clipboard between all connected computers
* Drag and drop text, images and files between computers 




## Demo (outdated)##
Quick demo:
[![](http://img.youtube.com/vi/rlR89GpMeCE/0.jpg)](http://www.youtube.com/watch?v=rlR89GpMeCE "Inputshare demo")

## Requirements ##
* Dotnet 3 runtime (runtime + desktop apps)
* Windows 7 or newer 

## Compiling ##
The solution can be compiled normally using visual studio. Builds are stored in /builds/release32 or /builds/debug64 etc depending on the build setting.

Logs are stored at C:\ProgramData\sbarrac1\inputshare

## Using ##

Run InputshareWindows.exe on the computers that you intend to use Inputshare with. When the program launches, the computer that has the keyboard and mouse that you wish to share with other computers should run the server and other computers should run the client. Both the client and server are started from InputshareWindows.exe

Once the server is running, start the client on the other computers and enter the address of the server.

![](https://i.imgur.com/G1Kv2S6.png)

Once the server has started and the clients are connected, we need to configure the server. Input can be switched between clients in two ways; either by a hotkey, or by setting the position of the client.

## Using the client service ##
The inputshare client can be run as a service, allowing more functionality. 

Benefits of the service version:
- Runs in the background
- Alt+ctrl+delete support
- Runs outside of the user space, allowing it to access the windows logon and alt+ctrl+delte screen.
- Can be set to run automatically on boot, automatically connecting to the last used server
- Can access UAC prompts

### Installing the service ###
The service can be simply installed from the command line. In this example the inputshare binaries (inputshareservice.exe, inputsharesp.exe ect) are located in c:\inputshare

'sc create inputshareservice binpath= C:\Inputshare\inputshareservice.exe"

To allow the service to automatically start on boot, add 'start= auto' to the end of the command.

**Important: Enter the command into CMD as administrator (not powershell as sc is a different command)**


## Server usage ##

### Setting the position of a client ###
Setting the position of a client allows you to simply move the cursor from one computer to another. To set a clients position, we use the display config editor. The display config editor allows you to move clients around a virtual desktop space, so when the cursor hits an edge of a client, the input is moved to the client assigned to that edge (if any).


![](https://i.imgur.com/U9ggGBr.png)

To set a clients position in the display config editor, first select a client from the left list. Then drag a client from the right list to any side of the client (drag into the label). When setting the edge of client X to client Y, the opposite edge of client X will also be assigned to client Y.


### Assigning a hotkey to a client ###
To assign a hotkey to a client, or to edit a function hotkey, double click on the list box item. To assign a key, click the button on the popup window and press the hotkey, once done click the button again and the hotkey will be assigned.

![](https://i.imgur.com/W62w0vb.png)

Hotkey modifiers can be any of the following: Alt, Ctrl, shift.



## Current issues ##
* Copy/pasting files is not yet implemented
* Memory issues when dealing with copy/pasting images
