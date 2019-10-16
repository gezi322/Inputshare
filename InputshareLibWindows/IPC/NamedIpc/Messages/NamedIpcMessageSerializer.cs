using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace InputshareLibWindows.IPC.NamedIpc.Messages
{
    internal static class NamedIpcMessageSerializer
    {
        private static BinaryFormatter binF = new BinaryFormatter();

        public static byte[] Serialize(NamedIpcMessage message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                binF.Serialize(ms, message);
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }

        public static NamedIpcMessage DeSerialize(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                ms.Seek(0, SeekOrigin.Begin);
                return (NamedIpcMessage)binF.Deserialize(ms);
            }
        }
    }
}
