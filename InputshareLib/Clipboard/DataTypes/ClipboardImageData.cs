using System;
using System.IO;

namespace InputshareLib.Clipboard.DataTypes
{
    public class ClipboardImageData : ClipboardDataBase
    {
        /// <summary>
        /// Creates an instance of clipboardimagedata with the specified raw image data
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="constructor"></param>
        public ClipboardImageData(byte[] imageData, bool constructor = false)
        {
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            ImageData = imageData;

        }

        /// <summary>
        /// Creates an instance of clipboardimagedata from a byte array
        /// </summary>
        /// <param name="rawData"></param>
        public ClipboardImageData(byte[] rawData)
        {
            ImageData = new byte[rawData.Length - 1];
            Buffer.BlockCopy(rawData, 1, ImageData, 0, ImageData.Length);
        }

        public override byte[] ToBytes()
        {
            byte[] data = new byte[ImageData.Length + 1];
            data[0] = (byte)ClipboardDataType.Image;
            Buffer.BlockCopy(ImageData, 0, data, 1, ImageData.Length);
            return data;
        }

        public byte[] ImageData { get; }

        public override ClipboardDataType DataType { get => ClipboardDataType.Image; }
    }
}
