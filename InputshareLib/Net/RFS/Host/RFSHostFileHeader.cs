using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InputshareLib.Net.RFS.Host
{
    internal class RFSHostFileHeader : RFSFileHeader
    {
        internal string FilePath { get; }

        internal RFSHostFileHeader(FileInfo file) : base(Guid.NewGuid(), file.Name, file.Length)
        {
            FilePath = file.FullName;
        }
    }
}
