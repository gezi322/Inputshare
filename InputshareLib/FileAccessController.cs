using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace InputshareLib
{
    /// <summary>
    /// Controls network access to files for use with dragdrop and clipboard virtual files
    /// </summary>
    class FileAccessController
    {
        private Dictionary<Guid, AccessToken> currentAccessTokens = new Dictionary<Guid, AccessToken>();

        /// <summary>
        /// An access token allows a client/server to access a specific instance of a filestream. each file in 
        /// a dragdrop or clipboard operation has a unique fileid that is generated when the file is copied/dragged.
        /// 
        /// </summary>
        private class AccessToken
        {
            public event EventHandler<Guid> TokenClosed;
            private int timeoutValue = 0;
            private Timer readTimeoutTimer;
            private Stopwatch timeoutStopwatch;

            public AccessToken(Guid tokenId, Guid[] allowedFiles, string[] allowedFileSources, int timeout)
            {
                if (timeout != 0)
                {
                    timeoutValue = timeout;
                    timeoutStopwatch = new Stopwatch();
                    timeoutStopwatch.Start();

                    readTimeoutTimer = new Timer(2000);
                    readTimeoutTimer.Elapsed += ReadTimeoutTimer_Elapsed;
                    readTimeoutTimer.AutoReset = true;
                    readTimeoutTimer.Start();
                }



                TokenId = tokenId;
                AllowedFiles = allowedFiles;

                for (int i = 0; i < allowedFiles.Length; i++)
                {
                    fileSourceDictionary.Add(allowedFiles[i], allowedFileSources[i]);
                }
            }

            private void ReadTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
            {
                //If this token has not been access in the past 10 seconds, close all streams.
                if (timeoutStopwatch.ElapsedMilliseconds > timeoutValue)
                {
                    CloseAllStreams();
                }
            }

            public void CloseAllStreams()
            {
                foreach (var stream in openFileStreams)
                {
                    stream.Value.Dispose();
                }

                openFileStreams.Clear();
                readTimeoutTimer.Dispose();
                timeoutStopwatch.Stop();
                TokenClosed?.Invoke(this, TokenId);
            }

            public void CloseStream(Guid file)
            {
                timeoutStopwatch.Restart();
                if (openFileStreams.TryGetValue(file, out FileStream stream))
                {
                    stream.Close();
                }
            }

            public int ReadFile(Guid file, byte[] buffer, int offset, int readLen)
            {
                timeoutStopwatch.Restart();
                if (openFileStreams.TryGetValue(file, out FileStream stream))
                {
                    int read = stream.Read(buffer, offset, readLen);

                    if (stream.Position == stream.Length)
                        CloseStream(file);

                    return read;
                }
                else
                {
                    if (fileSourceDictionary.TryGetValue(file, out string source))
                    {
                        FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
                        //ISLogger.Write("Debug: Filestream created for " + source);
                        openFileStreams.Add(file, fs);
                        int read = fs.Read(buffer, offset, readLen);

                        if (fs.Position == fs.Length)
                            CloseStream(file);

                        return read;
                    }
                    else
                    {
                        throw new ArgumentException("Stream not found in token");
                    }
                }
            }

            public long SeekFile(Guid file, SeekOrigin origin, long offset)
            {
                timeoutStopwatch.Restart();
                if (openFileStreams.TryGetValue(file, out FileStream stream))
                {
                    return stream.Seek(offset, origin);
                }
                else
                {
                    if (fileSourceDictionary.TryGetValue(file, out string source))
                    {
                        FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
                        // ISLogger.Write("Debug: Filestream created for " + source);
                        openFileStreams.Add(file, fs);
                        return fs.Seek(offset, origin);
                    }
                    else
                    {
                        throw new ArgumentException("Stream not found in token");
                    }

                }
            }


            public Guid TokenId { get; }
            public Guid[] AllowedFiles { get; }

            private Dictionary<Guid, string> fileSourceDictionary = new Dictionary<Guid, string>();
            private Dictionary<Guid, FileStream> openFileStreams = new Dictionary<Guid, FileStream>();
        }

        public Guid CreateFileReadToken(string sourceFile, Guid fileId, int timeout)
        {
            Guid id = Guid.NewGuid();
            AccessToken newToken = new AccessToken(id, new Guid[] { fileId }, new string[] { sourceFile }, timeout);
            currentAccessTokens.Add(id, newToken);
            return id;
        }

        /// <summary>
        /// Creates an access token for a group of files
        /// </summary>
        /// <param name="info">The file IDs and file sources to include in token</param>
        /// <param name="timeout">Time in seconds to automatically delete token if inactive. 0 = disabled</param>
        /// <returns></returns>
        public Guid CreateFileReadTokenForGroup(FileAccessInfo info, int timeout)
        {
            Guid accessId = Guid.NewGuid();
            AccessToken token = new AccessToken(accessId, info.FileIds, info.FileSources, timeout);
            token.TokenClosed += Token_TokenClosed;
            currentAccessTokens.Add(accessId, token);
            ISLogger.Write("FileAccessController: Created group token {0} for {1} files", accessId, info.FileIds.Length);
            return accessId;
        }

        private void Token_TokenClosed(object sender, Guid e)
        {
            ISLogger.Write("FileAccessController: Token {0} closed", e);
            currentAccessTokens.Remove(e);
        }

        public int ReadStream(Guid token, Guid file, byte[] buffer, int offset, int readLen)
        {
            if (currentAccessTokens.TryGetValue(token, out AccessToken access))
            {
                int r = access.ReadFile(file, buffer, offset, readLen);
                return r;
            }
            else
            {
                ISLogger.Write("FileAccessController: Token not found");
                throw new TokenNotFoundException();
            }

        }

        public long SeekStream(Guid token, Guid file, SeekOrigin origin, long offset)
        {
            if (currentAccessTokens.TryGetValue(token, out AccessToken access))
            {
                return access.SeekFile(file, origin, offset);
            }
            else
            {
                throw new TokenNotFoundException();
            }
        }

        public bool DoesTokenExist(Guid token)
        {
            return currentAccessTokens.ContainsKey(token);
        }

        public bool CloseStream(Guid token, Guid file)
        {
            if (currentAccessTokens.TryGetValue(token, out AccessToken access))
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
                currentAccessTokens.TryGetValue(token, out AccessToken access);

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
            public TokenNotFoundException() : base()
            {

            }
        }
    }
}
