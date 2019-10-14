# Inputshare #

The goal of inputshare is to make using multiple computers at the same time easier. Inputshare allows you to:

* Seamlessly switch mouse and keyboard input between computers
* Share a global clipboard between all connected computers
* Drag and drop text, images and files between computers 

## Demo ##
Quick demo:
[![](http://img.youtube.com/vi/rlR89GpMeCE/0.jpg)](http://www.youtube.com/watch?v=rlR89GpMeCE "Inputshare demo")

## Requirements ##
* Dotnet 3 runtime (runtime + desktop apps)
* Windows 7 or newer 

## Compiling ##
The solution can be compiled normally using visual studio. Builds are stored in /builds/release32 or /builds/debug64 etc depending on the build setting.

## Using ##

![](https://i.imgur.com/M3yt0Cr.png)

Run InputshareWindows.exe on the computers that you intend to use Inputshare with. When the program launches, the computer that has the keyboard and mouse that you wish to share with other computers should run the server and other computers should run the client. Both the client and server are started from InputshareWindows.exe

Once the server is running, start the client on the other computers and enter the address of the server.

![](https://i.imgur.com/oM1fDq0.png)

Once the server has started and the clients are connected, we need to configure the server. Input can be switched between clients in two ways; either by a hotkey, or by setting the position of the client.

### Setting the position of a client ###
Setting the position of a client allows you to simply move the cursor from one computer to another. To set a clients position, we use the set command. The set command allows you to assign client-A to be at an edge of client-B. For example, if you have a laptop on your desk that is to the left of your desktop computer (localhost), you would use the set command like so:

Set laptop left localhost

This would set the laptop to be at the left of your computer, so when you move the cursor to the very left of your screen it would appear on the laptop. This would also allow you to drag and drop files to-and-from your laptop by dragging files to the left of your screen.

To view the current position of all clients, use the list command.

![](https://i.imgur.com/VWxaibT.png)

The list command shows details about the client. It will also show any side of the screen that has a client attached. In the example above, the client 'ENVY15' is positioned at the bottom of localhost.

Note: the names of the clients can be shortened when typing, for example you could just use 'loc' as a shorthand for localhost.

Note: side names are: left, right, bottom, top

### Assigning a hotkey to a client ###
The input client can also be switched via a hotkey. A hotkey is a normal key with three possible modifiers; shift, control and alt.

To assign a hotkey to a client, use the assign command with the client name. EG 'assign localhost'. After entering the command, the console will ask for a hotkey to be used for that client, the next key (that is not a modifier key) that is pressed will be assigned to the client.

The current assigned hotkeys can be viewed via the list command.

## Current issues ##
* Copy/pasting files does not work yet.
* Memory issues when dealing with copy/pasting images
