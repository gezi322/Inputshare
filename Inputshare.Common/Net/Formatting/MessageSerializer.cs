using Inputshare.Common.Net.Messages;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Inputshare.Common.Net.Formatting
{
    /// <summary>
    /// Serializes or deserializes a network message
    /// </summary>
    internal class MessageSerializer
    {
        private static readonly BinaryFormatter _formatter = new BinaryFormatter();

        /// <summary>
        /// Serializes a network message into a byte array
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static byte[] Serialize(NetMessageBase message)
        {
            if (message.UseCustomSerialization)
                return CustomMessageSerializer.SerializeCustom(message);

            using MemoryStream ms = new MemoryStream();

            //Leave space at the start of the buffer to insert a header struct
            ms.Seek(NetMessageHeader.HeaderSize, SeekOrigin.Begin);
            //Write the serialized object to the memory stream
            _formatter.Serialize(ms, message);
            //Calculate the length of the message
            int len = (int)ms.Position - NetMessageHeader.HeaderSize;
            ms.Seek(0, SeekOrigin.Begin);
            //Write a header struct to the start of the array
            ms.Write(NetMessageHeader.CreateStandardHeader(len).Data, 0, NetMessageHeader.HeaderSize);
            return ms.ToArray();
        }

        /// <summary>
        /// Serializes a message without adding a header prefix
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static byte[] SerializeNoHeader(NetMessageBase message)
        {
            using MemoryStream ms = new MemoryStream();
            _formatter.Serialize(ms, message);
            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes a byte array into a network message
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static NetMessageBase Deserialize(byte[] data, ref NetMessageHeader header)
        {
            if((int)header.MessageType > (int)NetMessageType.CustomSerializedStart)
                return CustomMessageDeserializer.DeserializeCustom(data, header.MessageType);

            using MemoryStream ms = new MemoryStream(data);
            return (NetMessageBase)_formatter.Deserialize(ms);
        }

        internal static NetMessageBase Deserialize(MemoryStream stream)
        {
            return (NetMessageBase)_formatter.Deserialize(stream);
        }
    }
}
