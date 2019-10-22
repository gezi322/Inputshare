using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace InputshareLib.Clipboard.DataTypes
{
    public class ClipboardVirtualFileData : ClipboardDataBase
    {
        public DirectoryAttributes RootDirectory { get; }

        public override ClipboardDataType DataType { get => ClipboardDataType.File; }
        private List<FileAttributes> _allFiles;
        public List<FileAttributes> AllFiles { get => GetAllFiles(); }

        public Guid FileCollectionId { get; }

        public ClipboardVirtualFileData(DirectoryAttributes directories)
        {
            RootDirectory = directories;
            FileCollectionId = Guid.NewGuid();
        }

        public ClipboardVirtualFileData(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    br.ReadByte();
                    RootDirectory = (DirectoryAttributes)new BinaryFormatter().Deserialize(ms);
                }
            }
        }

        public override byte[] ToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write((byte)ClipboardDataType.File);
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, RootDirectory);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Represents a virtual file structure that can be written to a dataobject
        /// </summary>
        [Serializable]
        public class FileAttributes
        {
            public delegate Task<byte[]> RequestPartDelegate(Guid token,Guid operationId,  Guid fileId, int readLen);

            [field: NonSerialized]
            public event EventHandler ReadComplete;

            public override string ToString()
            {
                return FileName;
            }

            public FileAttributes(FileInfo info)
            {
                FileName = info.Name;
                FileSize = info.Length;
                LastChangeTime = info.LastWriteTime;
                FullPath = info.FullName;
                FileRequestId = Guid.NewGuid();
            }
            public string RelativePath { get; set; } = "";
            public string FileName { get; }
            public long FileSize { get; }
            public DateTime LastChangeTime { get; }
            public string FullPath { get; }
            public Guid FileRequestId { get; }

            public Guid FileOperationId { get; set; }
            public void MarkComplete()
            {
                ReadComplete?.Invoke(this, null);
            }

            /// <summary>
            /// Closes the IStream associated with this virtual file
            /// </summary>
            public void CloseStream()
            {
                CloseStreamRequested?.Invoke(this, null);
            }

            /// <summary>
            /// The access token that allows us to retrieve this file from the host PC
            /// </summary>
            public Guid RemoteAccessToken { get; set; }


            /// <summary>
            /// The shell will use this delegate to retreive data from the host
            /// </summary>
            [field: NonSerialized]
            public RequestPartDelegate ReadDelegate { get; set; }

            [field: NonSerialized]
            public event EventHandler CloseStreamRequested;
        }
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
            public string FullPath { get; }
        }

        /// <summary>
        /// Returns all files (excluding directories) that are stored in the virtual file
        /// </summary>
        /// <returns></returns>
        private List<ClipboardVirtualFileData.FileAttributes> GetAllFiles()
        {
            if (_allFiles == null)
            {
                _allFiles = new List<FileAttributes>();
                GetFileList(_allFiles, RootDirectory);
            }

            return _allFiles;
        }

        private void GetFileList(List<ClipboardVirtualFileData.FileAttributes> fileList, ClipboardVirtualFileData.DirectoryAttributes current)
        {
            if (current == RootDirectory)
            {
                foreach (var file in RootDirectory.Files)
                    fileList.Add(file);
            }

            foreach (var folder in current.SubFolders)
            {
                current = folder;

                foreach (var file in folder.Files)
                {
                    fileList.Add(file);
                }

                GetFileList(fileList, current);
            }
        }


    }


}
