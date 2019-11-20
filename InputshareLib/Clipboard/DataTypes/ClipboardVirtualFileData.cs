﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace InputshareLib.Clipboard.DataTypes
{
    public class ClipboardVirtualFileData : ClipboardDataBase
    {
        public delegate Task<byte[]> RequestPartDelegate(Guid token, Guid fileId, int readLen);
        public delegate Task<Guid> RequestTokenDelegate(Guid operationId);

        public DirectoryAttributes RootDirectory { get; }

        public override ClipboardDataType DataType { get => ClipboardDataType.File; }
        private List<FileAttributes> _allFiles;
        public List<FileAttributes> AllFiles { get => GetAllFiles(); }

        public Guid FileCollectionId { get; }

        [field:NonSerialized]
        public RequestPartDelegate RequestPartMethod { get; set; }
        [field:NonSerialized]
        public RequestTokenDelegate RequestTokenMethod { get; set; }

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
        /// Returns all files (excluding directories) that are stored in the virtual file
        /// </summary>
        /// <returns></returns>
        private List<FileAttributes> GetAllFiles()
        {
            if (_allFiles == null)
            {
                _allFiles = new List<FileAttributes>();
                GetFileList(_allFiles, RootDirectory);
            }

            return _allFiles;
        }

        private void GetFileList(List<FileAttributes> fileList, DirectoryAttributes current)
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
