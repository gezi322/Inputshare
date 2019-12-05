using System;

namespace InputshareLib.Clipboard.DataTypes
{
    /// <summary>
    /// Represents universal clipboard data that can be sent to any OS
    /// </summary>
    
        [Serializable]
    public abstract class ClipboardDataBase
    {
        public abstract byte[] ToBytes();
        public abstract ClipboardDataType DataType { get; }
        public Guid OperationId { get; set; }

        public static ClipboardDataBase FromBytes(byte[] data)
        {
            ClipboardDataType type = (ClipboardDataType)data[0];

            switch (type)
            {
                case ClipboardDataType.File:
                    return ClipboardVirtualFileData.FromBytes(data);
                case ClipboardDataType.Image:
                    return new ClipboardImageData(data);
                case ClipboardDataType.Text:
                    return new ClipboardTextData(data);
                default:
                    throw new ArgumentException("Invalid clipboard data");
            }

        }
    }
}
