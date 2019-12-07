using System.Drawing.Imaging;
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
        public const int NetworkMessageChunkSize = 1024 * 256; //256KB

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
        /// Size of network socket buffers
        /// </summary>
        public const int SocketBufferSize = 1024 * 260; //260KB

        /// <summary>
        /// Image format used to transfer copied images
        /// </summary>
        public static readonly ImageFormat ImageEncodeFormat = ImageFormat.Jpeg;

        /// <summary>
        /// If true, SP processes are launched in the users desktop with a visible console
        /// </summary>
        public const bool DEBUG_SPCONSOLEENABLED = true;

        /// <summary>
        /// InputshareSP launches in the specified session (-1 for current console session)
        /// </summary>
        public const int DEBUG_SPECIFYSPSESSION = -1;

        public const bool DEBUG_PRINTINPUTKEYS = false;
        public const bool DEBUG_PRINTOUTPUTKEYS = false;

        /// <summary>
        /// Rate at which to poll the X server (temp)
        /// </summary>
        public const int XServerPollRateMS = 1;

        public const int MaxFileTransferFiles = 10000;
    }
}
