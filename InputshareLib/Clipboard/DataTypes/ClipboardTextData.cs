using System;

namespace InputshareLib.Clipboard.DataTypes
{
    public class ClipboardTextData : ClipboardDataBase
    {
        public ClipboardTextData(string text)
        {
            Text = text;
        }

        public ClipboardTextData(byte[] data)
        {
            Text = Settings.NetworkMessageTextEncoder.GetString(data, 1, data.Length - 1);
        }

        public override byte[] ToBytes()
        {
            byte[] nameBuff = Settings.NetworkMessageTextEncoder.GetBytes(Text);
            byte[] data = new byte[nameBuff.Length + 1];
            data[0] = (byte)ClipboardDataType.Text;
            Buffer.BlockCopy(nameBuff, 0, data, 1, nameBuff.Length);
            return data;
        }

        public string Text { get; }

        public override ClipboardDataType DataType { get => ClipboardDataType.Text; }
    }
}
