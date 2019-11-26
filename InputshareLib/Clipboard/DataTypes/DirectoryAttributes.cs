using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InputshareLib.Clipboard.DataTypes
{

    [Serializable]
    public class DirectoryAttributes
    {
        public DirectoryAttributes(string name, List<FileAttributes> files, List<DirectoryAttributes> subFolders, string fullPath)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Files = files;
            SubFolders = subFolders;
            FullPath = fullPath;
        }

        public DirectoryAttributes(DirectoryInfo dir)
        {
            Name = dir.Name;
            FullPath = dir.FullName;
            SubFolders = new List<DirectoryAttributes>();
            Files = new List<FileAttributes>();
        }

        public override string ToString()
        {
            return Name;
        }

        public string RelativePath { get; set; } = "";
        public string Name { get; }
        public List<FileAttributes> Files { get; }
        public List<DirectoryAttributes> SubFolders { get; }

        [field: NonSerialized]
        public string FullPath { get; }
    }
}
