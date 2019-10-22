using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace InputshareLibWindows.IPC
{
    /// <summary>
    /// Serializes objects that can be sent over IPC
    /// </summary>
    internal static class IpcMessageSerializer
    {
        private static BinaryFormatter binF = new BinaryFormatter();

        public static byte[] Serialize(IpcMessage message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                binF.Serialize(ms, message);
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }

        public static IpcMessage DeSerialize(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                ms.Seek(0, SeekOrigin.Begin);
                return (IpcMessage)binF.Deserialize(ms);
            }
        }
    }
}
