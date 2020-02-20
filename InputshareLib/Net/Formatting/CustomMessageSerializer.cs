using InputshareLib.Net.Messages;
using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Formatting
{
    /// <summary>
    /// Serializes messages without the overhead of a binaryformatter
    /// </summary>
    internal class CustomMessageSerializer
    {
        internal static byte[] SerializeCustom(NetMessageBase message)
        {
            if (message is RFSReadReply readReply)
                return SerializeRFSReadReply(readReply);
            else if (message is RFSReadRequest readRequest)
                return SerializeRFSReadRequest(readRequest);
            else
                throw new ArgumentException("Cannot custom serialize " + message.GetType().Name);
        }

        private static byte[] SerializeRFSReadReply(RFSReadReply reply)
        {
            byte[] data = new byte[reply.ReturnData.Length + 20];
            data.InsertGuid(reply.MessageId, 0);
            data.InsertInt(reply.ReturnData.Length, 16);
            Buffer.BlockCopy(reply.ReturnData, 0, data, 20, reply.ReturnData.Length);
            return InsertSerializedHeader(data, NetMessageType.RFSReadReply);
        }

        private static byte[] SerializeRFSReadRequest(RFSReadRequest message)
        {
            byte[] data = new byte[(16 * 4) + 4];
            data.InsertGuid(message.MessageId, 0);
            data.InsertGuid(message.GroupId, 16);
            data.InsertGuid(message.FileId, 32);
            data.InsertGuid(message.TokenId, 48);
            data.InsertInt(message.ReadLen, 64);
            return InsertSerializedHeader(data, NetMessageType.RFSReadRequest);
        }

        /// <summary>
        /// Appends a net message header to the begging of the given array
        /// </summary>
        /// <param name="original"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static byte[] InsertSerializedHeader(byte[] original, NetMessageType type)
        {
            byte[] data = new byte[original.Length + NetMessageHeader.HeaderSize];
            var header = NetMessageHeader.CreateCustomSerializedHeader(original.Length, type);
            Buffer.BlockCopy(header.Data, 0, data, 0, NetMessageHeader.HeaderSize);
            Buffer.BlockCopy(original, 0, data, NetMessageHeader.HeaderSize, original.Length);
            return data;
        }
    }
}
