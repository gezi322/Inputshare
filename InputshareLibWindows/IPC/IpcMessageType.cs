using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC
{
    public enum IpcMessageType
    {
        IpcHostOK,
        IpcClientOK,
        IpcPoll,
        IpcPollResponse,
        NetIpcStateRequest,
        NetIpcStateResponse,
        NetIpcNameRequest,
        NetIpcNameResponse,
        NetIpcSetName,
        NetIpcConnect,
        NetIpcDisconnect,
        NetIpcLogMessage,
        NetIpcAddressRequest,
        NetIpcAddressResponse,
        NetIpcClientConnected,
        NetIpcClientConnectionFailed,
        NetIpcClientConnectionError,
        NetIpcClientDisconnected,
        NetIpcAutoReconnectResponse,
        NetIpcAutoReconnectRequest,
        NetIpcEnableAutoReconnect,
        NetIpcDisableAutoReconnect,
        AnonIpcInputData,

        AnonIpcDisplayConfigRequest,
        AnonIpcDisplayConfigReply ,
        AnonIpcLMouseStateRequest,
        AnonIpcLMouseStateReply,
        AnonIpcEdgeHit,
        AnonIpcDoDragDrop,
        AnonIpcDragDropSuccess,
        AnonIpcDragDropCancelled,
        AnonIpcDragDropComplete,
        AnonIpcCheckForDrop,
        AnonIpcStreamReadRequest,
        AnonIpcStreamReadResponse,
        AnonIpcStreamReadError,
        AnonIpcClipboardData,
    }
}
