using InputshareLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLibWindows.IPC.AnonIpc.Messages
{
    public class AnonIpcEdgeHitMessage : AnonIpcMessage
    {
        public Edge HitEdge { get; }
        public AnonIpcEdgeHitMessage(byte[] data) : base(data)
        {
            HitEdge = (Edge)data[17];
        }

        public override byte[] ToBytes()
        {
            byte[] data = CreateArray(1);
            data[17] = (byte)HitEdge;
            return data;
        }

        public AnonIpcEdgeHitMessage(Edge edge, Guid messageId = default) : base(AnonIpcMessageType.EdgeHit, messageId)
        {
            HitEdge = edge;
        }
    }
}
