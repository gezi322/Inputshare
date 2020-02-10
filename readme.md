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
