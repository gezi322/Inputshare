using System.Text;

namespace InputshareLib
{
    public static class Settings
    {

        /// <summary>
        /// Disables windowsinputmanager mouse and keyboard hooks
        /// </summary>
        public const bool DEBUG_DISABLEHOOKS = false;

        /// <summary>
        /// Max size  at which packets are split up before being sent
        /// </summary>
        public const int NetworkMessageChunkSize = 1024*256; //256KB

        /// <summary>
        /// Max size of a network message chunk ignoring size,type and ID bytes
        /// </summary>
        public const int NetworkMessageChunkSizeNoHeader = NetworkMessageChunkSize - 100;

        public const string InputshareVersion = "0.0.0.3";

        /// <summary>
        /// Encoder used to encode text to send over TCP socket
        /// </summary>
        public static Encoding NetworkMessageTextEncoder = Encoding.UTF8;

        /// <summary>
        /// Size of network socket buffers - 128KB
        /// </summary>
        public const int SocketBufferSize = 1024 * 260; //260KB

        public static bool DEBUG_SPCONSOLEENABLED = false;
        
    }
}
