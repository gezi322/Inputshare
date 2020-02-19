using InputshareLib.Net.Messages;
using InputshareLib.Net.Messages.Replies;
using InputshareLib.Net.Messages.Requests;
using InputshareLib.Net.RFS.Client;
using InputshareLib.Net.RFS.Host;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InputshareLib.Net.RFS
{
    /// <summary>
    /// Controls streaming files between clients
    /// </summary>
    internal class RFSController
    {
        private Dictionary<Guid, RFSHostFileGroup> _hostedGroups = new Dictionary<Guid, RFSHostFileGroup>();

        internal RFSController()
        {

        }

        /// <summary>
        /// Hosts the specified files and returns a group of file IDS
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        internal RFSFileGroup HostFiles(string[] originalSources)
        {
            if (originalSources == null)
                throw new ArgumentNullException(nameof(originalSources));

            CreateRelativePathList(originalSources, out var relativePaths, out var fullPaths);

            var fileHeaders = CreateHeaders(fullPaths, relativePaths);
            var group = new RFSHostFileGroup(Guid.NewGuid(), fileHeaders);
            _hostedGroups.Add(group.GroupId, group);
            return new RFSFileGroup(group.GroupId, fileHeaders);
        }

        /// <summary>
        /// Returns an array of fileheaders from a list of full paths and matching relative paths
        /// </summary>
        /// <param name="fullPaths"></param>
        /// <param name="relativePaths"></param>
        /// <returns></returns>
        private RFSFileHeader[] CreateHeaders(string[] fullPaths, string[] relativePaths)
        {
            RFSFileHeader[] headers = new RFSFileHeader[relativePaths.Length];
            for (int i = 0; i < fullPaths.Length; i++)
            {
                try
                {
                    FileInfo file = new FileInfo(fullPaths[i]);
                    headers[i] = new RFSFileHeader(Guid.NewGuid(), file.Name, file.Length, relativePaths[i], fullPaths[i]);
                }
                catch (Exception ex)
                {
                    Logger.Write($"Could not copy file {fullPaths[i]} : {ex.Message}");
                    headers[i] = null;
                }
            }

            return headers;
        }

        /// <summary>
        /// Sorts an array of full file and folder path names into
        /// full and relative file paths
        /// </summary>
        /// <param name="originalSources"></param>
        /// <param name="relativePaths"></param>
        /// <param name="fullPaths"></param>
        private void CreateRelativePathList(string[] originalSources, out string[] relativePaths, out string[] fullPaths)
        {
            List<string> fullPathsList = new List<string>();
            List<string> relativePathsList = new List<string>();

            int fileCount = 0;
            foreach (var file in originalSources)
            {
                if (File.GetAttributes(file).HasFlag(FileAttributes.Directory))
                {
                    AddFilesRecursive(fullPathsList, relativePathsList, file, "./" + new DirectoryInfo(file).Name, ref fileCount);
                }
                else
                {
                    fullPathsList.Add(file);
                    relativePathsList.Add("./" + new FileInfo(file).Name);
                }
            }

            relativePaths = relativePathsList.ToArray();
            fullPaths = fullPathsList.ToArray();
        }

        /// <summary>
        /// Converts a list of file/folder names into a relative folder structure.
        /// </summary>
        /// <param name="fullPaths"></param>
        /// <param name="relativePaths"></param>
        /// <param name="currentPath"></param>
        /// <param name="relativePath"></param>
        private void AddFilesRecursive(List<string> fullPaths, List<string> relativePaths, string currentPath, string relativePath, ref int count)
        {
            if (count > 10 * 1000)
                throw new InvalidDataException("Too many files");


            foreach(var path in Directory.GetFileSystemEntries(currentPath))
            {
                try
                {
                    if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                    {
                        string dirName = new DirectoryInfo(path).Name;
                        var nextPath = relativePath + "/" + dirName;
                        AddFilesRecursive(fullPaths, relativePaths, path, nextPath, ref count);
                    }
                    else
                    {
                        fullPaths.Add(path);
                        count++;
                        relativePaths.Add(relativePath + "/" + new FileInfo(path).Name);
                    }
                }catch(Exception ex) when (!(ex is InvalidDataException))
                {
                    Logger.Write("Failed to add path " + path + ": " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Reads data from a locally hosted file
        /// </summary>
        /// <param name="tokenId"></param>
        /// <param name="groupId"></param>
        /// <param name="fileId"></param>
        /// <param name="readLen"></param>
        /// <returns></returns>
        private async Task<byte[]> ReadHostedFile(Guid tokenId, Guid groupId, Guid fileId, int readLen)
        {
            if(_hostedGroups.TryGetValue(groupId, out var group))
            {
                byte[] buff = new byte[readLen];
                int rLen = await group.ReadAsync(tokenId, fileId, buff, readLen);

                //Make sure the returned array is the correct size
                if(rLen != buff.Length)
                {
                    byte[] resizedBuff = new byte[rLen];
                    Buffer.BlockCopy(buff, 0, resizedBuff, 0, rLen);
                    buff = resizedBuff;
                }

                return buff;
            }
            else
            {
                throw new RFSException("Group not found");
            }
        }

        /// <summary>
        /// Seeks the stream of a locally hosted file
        /// </summary>
        /// <param name="tokenId"></param>
        /// <param name="groupId"></param>
        /// <param name="fileId"></param>
        /// <param name="origin"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private long SeekHostedFile(Guid tokenId, Guid groupId, Guid fileId, SeekOrigin origin, long offset)
        {
            if (_hostedGroups.TryGetValue(groupId, out var group))
            {
                return group.Seek(tokenId, fileId, origin, offset);
            }
            else
            {
                throw new RFSException("Group not found");
            }
        }

        internal async Task HandleNetMessageAsync(NetMessageBase message, SocketBase sender)
        {
            if (message is RFSReadRequest readRequest)
                await HandleReadRequest(readRequest, sender);
            else if (message is RFSTokenRequest tokenRequest)
                await HandleTokenRequest(tokenRequest, sender);
            else if (message is RFSSeekRequest seekRequest)
                await HandleSeekRequest(seekRequest, sender);
        }

        private async Task HandleTokenRequest(RFSTokenRequest request, SocketBase sender)
        {
            if(_hostedGroups.TryGetValue(request.GroupId, out var group))
            {
                var token = group.CreateToken();
                await sender.SendMessageAsync(new RFSTokenReply(token.Id, request.MessageId));
                Logger.Write("Returned token!");
            }
            else
            {
                throw new RFSException("Group ID not found");
            }
        }

        private async Task HandleSeekRequest(RFSSeekRequest request, SocketBase sender)
        {
            long newPos = SeekHostedFile(request.TokenId, request.GroupId, request.FileId, request.Origin, request.Offset);
            await sender.SendMessageAsync(new RFSSeekReply(request.MessageId, newPos));
        }

        private async Task HandleReadRequest(RFSReadRequest readRequest, SocketBase sender)
        {
            byte[] data = await ReadHostedFile(readRequest.TokenId, readRequest.GroupId, readRequest.FileId, readRequest.ReadLen);
            await sender.SendMessageAsync(new RFSReadReply(readRequest.MessageId, data));
        }
    }
}
