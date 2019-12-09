namespace InputshareLib.Net.Messages
{
    internal enum MessageType
    {
        Unknown = 0,
        ClientInitialInfo = 1,
        ClientDisconnecting = 2,
        ServerStopping = 3,
        ServerOK = 4,
        MessagePart = 5,
        DisplayConfig = 6,
        ServerRequestInitialInfo = 7,
        ClientDeclined = 8,
        InputData = 9,
        ClipboardData = 10,
        EdgeHitTop = 11,
        EdgeHitBottom = 12,
        EdgeHitRight = 13,
        EdgeHitLeft = 14,
        ClientActive = 15,
        ClientInactive = 16,
        DragDropData = 17,
        ClientEdgeStates = 18,
        DragDropSuccess = 19,
        DragDropCancelled = 20,
        RequestFileGroupToken = 21,
        RequestFileGroupTokenReponse = 22,
        FileStreamReadRequest = 23,
        FileStreamReadResponse = 24,
        RemoteFileError = 25,
        FileStreamCloseRequest = 26,
        CancelAnyDragDrop = 27,
    }
}
