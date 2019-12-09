using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Net;
using InputshareLib.Server;

namespace InputshareLib.FileController
{
    /// <summary>
    /// Controls network access to files for use with dragdrop and clipboard virtual files
    /// </summary>
    class FileAccessController
    {
        private Dictionary<Guid, IFileAccessToken> currentAccessTokens = new Dictionary<Guid, IFileAccessToken>();

        public Guid CreateFileReadToken(string sourceFile, Guid fileId, int timeout)
        {
            Guid id = Guid.NewGuid();
            LocalFileAccessToken newToken = new LocalFileAccessToken(id, new Guid[] { fileId }, new string[] { sourceFile }, timeout);
            currentAccessTokens.Add(id, newToken);
            return id;
        }

        /// <summary>
        /// Sets the token to be deleted if it has not being read in X ms
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="token"></param>
        public void SetTokenTimeout(int ms, Guid token)
        {
            if (!currentAccessTokens.TryGetValue(token, out IFileAccessToken ac))
                throw new TokenNotFoundException();

            ac.SetTimeout(ms);
            ISLogger.Write("Set timeout for token {0} to {1}MS", ac.TokenId, ms);
        }

        public async void Client_RequestedStreamRead(object sender, NetworkSocket.RequestStreamReadArgs args)
        {
            NetworkSocket client = sender as NetworkSocket;

            byte[] buff = new byte[args.ReadLen];
            int bytesRead = 0;

            //We need to determine if localhost is the host of the files, otherwise we need to forward the request 
            //onto the host of the files
            if (DoesTokenExist(args.Token))
            {
                try
                {
                    bytesRead = await ReadStream(args.Token, args.File, buff, 0, args.ReadLen);
                }
                catch (Exception ex)
                {
                    ISLogger.Write("Failed to read stream: " + ex.Message);
                    ISLogger.Write(ex.StackTrace);
                    client.SendFileErrorResponse(args.NetworkMessageId, "An error occurred while reading from stream: " + ex.Message);

                    return;
                }
            }

            if (bytesRead != buff.Length)
            {
                //resize the buffer so we don't send a buffer that ends with empty data.
                byte[] resizedBuffer = new byte[bytesRead];
                Buffer.BlockCopy(buff, 0, resizedBuffer, 0, bytesRead);
                buff = resizedBuffer;
            }

            client.SendReadRequestResponse(args.NetworkMessageId, buff);
        }

        /// <summary>
        /// Creates an access token for a group of files
        /// </summary>
        /// <param name="info">The file IDs and file sources to include in token</param>
        /// <param name="timeout">Time in seconds to automatically delete token if inactive. 0 = disabled</param>
        /// <returns></returns>
        private Guid CreateFileReadTokenForGroup(FileAccessInfo info, int timeout)
        {
            Guid accessId = Guid.NewGuid();
            ISLogger.Write("Access token timeout = " + timeout);
            LocalFileAccessToken token = new LocalFileAccessToken(accessId, info.FileIds, info.FileSources, timeout);
            token.TokenClosed += Token_TokenClosed;
            currentAccessTokens.Add(accessId, token);
            ISLogger.Write("FileAccessController: Created group token {0} for {1} files", accessId, info.FileIds.Length);
            return accessId;
        }

        public void AddRemoteAccessToken(ISServerSocket host, Guid remoteToken)
        {
            IFileAccessToken token = new RemoteFileAccessToken(remoteToken, host);
            //todo
            currentAccessTokens.Add(remoteToken, token);
            ISLogger.Write("Added remote access token!");
        }

        public Guid CreateTokenForOperation(DataOperation operation, int timeout)
        {
            if (operation.Data.DataType != Clipboard.DataTypes.ClipboardDataType.File)
                throw new ArgumentException("Data type must be 'File'");

            return CreateTokenForOperationLocal(operation, timeout);
        }


        private Guid CreateTokenForOperationLocal(DataOperation operation, int timeout)
        {
            Guid accessId = Guid.NewGuid();

            ClipboardVirtualFileData file = operation.Data as ClipboardVirtualFileData;
            Guid[] fIds = new Guid[file.AllFiles.Count];
            string[] fSources = new string[file.AllFiles.Count];

            for (int i = 0; i < file.AllFiles.Count; i++)
            {
                fIds[i] = file.AllFiles[i].FileRequestId;
                fSources[i] = file.AllFiles[i].FullPath;
            }

            return CreateFileReadTokenForGroup(new FileAccessController.FileAccessInfo(fIds, fSources), timeout);
        }

        private void Token_TokenClosed(object sender, Guid e)
        {
            ISLogger.Write("FileAccessController: Token {0} closed", e);
            currentAccessTokens.Remove(e);
        }

        /// <summary>
        /// Reads data from the specified file with the specified token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="file"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="readLen"></param>
        /// <returns></returns>
        /// <exception cref="TokenNotFoundException"></exception>"
        public async Task<int> ReadStream(Guid token, Guid file, byte[] buffer, int offset, int readLen)
        {
            if (currentAccessTokens.TryGetValue(token, out IFileAccessToken access))
            {
                int r = await access.ReadFile(file, buffer, offset, readLen);
                return r;
            }
            else
            {
                ISLogger.Write("FileAccessController: Token not found");
                throw new TokenNotFoundException();
            }

        }

        /*
        public long SeekStream(Guid token, Guid file, SeekOrigin origin, long offset)
        {
            if (currentAccessTokens.TryGetValue(token, out FileAccessToken access))
            {
                return access.SeekFile(file, origin, offset);
            }
            else
            {
                throw new TokenNotFoundException();
            }
        }*/

        public void DeleteAllTokens()
        {
            foreach(var token in currentAccessTokens)
            {
                DeleteToken(token.Key);
            }
        }

        public bool DoesTokenExist(Guid token)
        {
            return currentAccessTokens.ContainsKey(token);
        }

        public bool CloseStream(Guid token, Guid file)
        {
            if (currentAccessTokens.TryGetValue(token, out IFileAccessToken access))
            {
                access.CloseStream(file);
                return true;
            }

            return false;
        }

        public void DeleteToken(Guid token)
        {
            if (currentAccessTokens.ContainsKey(token))
            {
                currentAccessTokens.TryGetValue(token, out IFileAccessToken access);

                if (access == null)
                {
                    ISLogger.Write("FileAccessController: Could not delete access token: Access token was null");
                    return;
                }

                access.CloseAllStreams();

            }
            else
            {
                ISLogger.Write("FileAccessController: Could not delete access token: Key {0} not found", token);
            }

        }

        public class FileAccessInfo
        {
            public FileAccessInfo(Guid[] fileIds, string[] fileSources)
            {
                FileIds = fileIds;
                FileSources = fileSources;
            }

            public Guid[] FileIds { get; }
            public string[] FileSources { get; }
        }

        public class TokenNotFoundException : Exception
        {
            public TokenNotFoundException() : base("The specified access token was not found")
            {
            }
        }
    }
}
