using Inputshare.Common.Net.Messages;
using Inputshare.Common.Net.Messages.Replies;
using Inputshare.Common.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Formatting
{
    /// <summary>
    /// Deserializes messages without the overhead of a binaryformatter
    /// </summary>
    internal static class CustomMessageDeserializer
    {
        internal static NetMessageBase DeserializeCustom(byte[] data, NetMessageType type)
        {
            switch (type)
            {
                case NetMessageType.RFSReadReply:
                    return DeserializeRFSReadReply(data);
                case NetMessageType.RFSReadRequest:
                    return DeserializeRFSReadRequest(data);
                default:
                    throw new ArgumentException("Cannot custom deserialize " + type.ToString());
            }
        }

        private static RFSReadRequest DeserializeRFSReadRequest(byte[] data)
        {
            Guid messageId = data.ParseGuid(0);
            Guid group = data.ParseGuid(16);
            Guid file = data.ParseGuid(32);
            Guid token = data.ParseGuid(48);
            int readLen = data.ParseInt(64);
            var msg = new RFSReadRequest(token, group, file, readLen);
            msg.MessageId = messageId;
            return msg;
        }

        private static RFSReadReply DeserializeRFSReadReply(byte[] data)
        {
            Guid messageId = data.ParseGuid(0);
            int len = data.ParseInt(16);
            byte[] returnedData = new byte[len];
            Buffer.BlockCopy(data, 20, returnedData, 0, returnedData.Length);
            return new RFSReadReply(messageId, returnedData);
        }
    }
}
