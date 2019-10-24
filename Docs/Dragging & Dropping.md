## Detecting dragged files (windows) ##

When the cursor hits the edge of the screen and there is a client assigned to that edge, the WindowsDragDropManager shows the WindowsDropTarget window under the the mouse cursor. The window then uses SendInput to release the left mouse button, which will allow any files that are being dragged to be dropped into the WindowsDropTarget window, which will detect dropped file/text/images with the Form_DragDrop event.

Windows uses a DataObject to transfer data between applications. ClipboardTranslatorWindows is used to convert the windows DataObject into a generic format that can be used with any OS. Windows uses the same DataObjects for both drag/drop and clipboard data transfer.

Windows data objects will be translated into one of the following objects:

- ClipboardImageData

- ClibpoardTextData

- ClibpoardVirtualFileData

If the dragged data is an image or text, they are sent directly to whichever client is dropping the files. 

If the dragged data is files, the host (whichever client/server owns the files) creates an access token that another client (or the server) can use to access any file included in the ClipboardVirtualFileData. Each file has a unique GUID that can be sent alongside the token to read the file.

## Dropping files (windows) ##

When a client received data to be dropped, The data is converted back into an InputshareDataObject, which inherits from IDataObject to provide a way to communicate with the OLE. Text and Image data is written directly to the DataObject, however files are more difficult to implement. 

Each dragdrop operation can be identified by a GUID. When virtual file data is received by a client, the client sends a request to the server for an access token for the received operation that can be used to read each file from the file host. To read a file, a client needs to send the token guid, file guid and how many bytes it wants to read. 

A dragdrop file operation uses ClipboardVirtualFileData, which includes a list of FileAttributes that describes each file, and includes the file GUID. After the client receives a token to access the file within the operation, each FileAttribute object is given a delegate to a callback method that reads data from the file host to be used by the InputshareDataObject.

To start dropping the file, the WindowsDropSource window is shown at the cursor position and the left mouse button is pressed down. This starts the dragdrop process, which ends when the user either release the left mouse button or presses escape. The data can be dragged off screen onto another client, in which case the local dragdrop operation is cancelled, and the server will then pass the operation onto the next client. The DropSourceWindow creates an InputshareDataObject, which generates a stream of data (file descriptor) that is used by windows to determine what files will be copied and how much disk space is required.

A InputshareDataObject creates one ManagedRemoteIStream for each file in the operation. When the OLE requests data from a file, the ManagedRemoteIStream for that file is passed. ManagedRemoteIStream is a wrapper for a windows native IStream, which includes a method Read(byte[] buffer, int bufferSize, IntPtr bytesReadPtr). When the OLE calls the Read method, the callback delegate from the FileAttributes is used to read data from the file host client.

InputshareDataObject implements IAsyncOperation, so the DataObject uses an async operation instead of blocking the thread. When the async operation is complete, an event is fired which marks the completion of the operation, as all files have been fully read. The host client will then close the access token associated with the operation. The event is fired even if the data transfer failed, in which case the operation will be cancelled.

## Controlling global drag/drop operations ##

Data can be dragged and dropped between any client connected to an inputshare server, this results in a lot of possible states. The server implements a GlobalDragDropController to control drag/drop operations on each client. A dragdrop operation consists of the following:

- Data (can be Image/Text/Files)
- Host client (whichever client initiated the operation)
- A guid to allow clients to specify a specific operation.
- A remote access token (see below)
- Receiver client
- A state

A remote access token is only stored in the operation if the operation is a file operation. If the host is not the server, then the server must request an access token from the host client. Storing the access token in the operation allows another client to access files from the host client (as clients cannot directly communicate), allowing a client->server->client file transfer.

A global dragdrop operation is started when a client drags files to an edge of the screen, if there is a client at that edge. There can only be one 'active' dragdrop operation, however operations are stored temporarily if they are in the transferring files state. An operation can be in one of the following states:

- Dragging
- Dropped
- Complete
- Transferring files
- Cancelled

If the operation is in the dragging state, the operation will be sent to the input client whenever the input client changes.

When a client drops the data into a valid location, the client will send a DragDropSuccess message, indicating that the operation should not be sent to other clients. When the client has finished reading the data, it sends a DragDropComplete message to tell the server that the operation is now complete. DragDropComplete will be sent instantly for Text and Image operations as there is no external data to be read. A client can also send DragDropCancelled to tell the server that the user cancelled the dragdrop operation.

When DragDropSuccess is sent for a file operation, the operation will be put into the transferring files state, where the GlobalDragDropController will store the operation, but allow a new operation to start while files are being transferred. This allows multiple drag/drop file transfers to happen simultaneously.

