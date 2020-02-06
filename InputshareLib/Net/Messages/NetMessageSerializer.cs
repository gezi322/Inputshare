using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace InputshareLib.Net.Messages
{
    /// <summary>
    /// Serializes or deserializes a network message
    /// </summary>
    internal class NetMessageSerializer
    {
        private static readonly BinaryFormatter _formatter = new BinaryFormatter();

        /// <summary>
        /// Serializes a network message into a byte array
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static byte[] Serialize(NetMessageBase message)
        {
            using MemoryStream ms = new MemoryStream();

            //Leave space at the start of the buffer to insert a header struct
            ms.Seek(NetMessageHeader.HeaderSize, SeekOrigin.Begin);
            //Write the serialized object to the memory stream
            _formatter.Serialize(ms, message);
            //Calculate the length of the message
            int len = (int)ms.Position - NetMessageHeader.HeaderSize;
            ms.Seek(0, SeekOrigin.Begin);
            //Write a header struct to the start of the array
            ms.Write(new NetMessageHeader(len).Data, 0, NetMessageHeader.HeaderSize);
            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes a byte array into a network message
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static T Deserialize<T>(byte[] data)
        {
            using MemoryStream ms = new MemoryStream(data);
            return (T)_formatter.Deserialize(ms);
        }
    }
}
