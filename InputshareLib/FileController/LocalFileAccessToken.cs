using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace InputshareLib.FileController
{
    /// <summary>
    /// An access token allows a client/server to access a specific instance of a filestream. each file in 
    /// a dragdrop or clipboard operation has a unique fileid that is generated when the file is copied/dragged.
    /// 
    /// </summary>
    internal class LocalFileAccessToken : IFileAccessToken
    {
        public Guid TokenId { get; }
        public Guid[] AllowedFiles { get; }

        private Dictionary<Guid, string> fileSourceDictionary = new Dictionary<Guid, string>();
        private Dictionary<Guid, FileStream> openFileStreams = new Dictionary<Guid, FileStream>();

        public event EventHandler<Guid> TokenClosed;
        private int timeoutValue = 0;
        private Timer readTimeoutTimer;
        private Stopwatch timeoutStopwatch;

        public LocalFileAccessToken(Guid tokenId, Guid[] allowedFiles, string[] allowedFileSources, int timeout)
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

        public void SetTimeout(int ms)
        {
            timeoutValue = ms;
            timeoutStopwatch = new Stopwatch();
            timeoutStopwatch.Start();

            readTimeoutTimer = new Timer(2000);
            readTimeoutTimer.Elapsed += ReadTimeoutTimer_Elapsed;
            readTimeoutTimer.AutoReset = true;
            readTimeoutTimer.Start();
        }

        private void ReadTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //If this token has not been access in the past timeoutValue seconds, close all streams.
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
            readTimeoutTimer?.Dispose();
            timeoutStopwatch?.Stop();
            TokenClosed?.Invoke(this, TokenId);
        }

        public void CloseStream(Guid file)
        {
            timeoutStopwatch?.Restart();
            if (openFileStreams.TryGetValue(file, out FileStream stream))
            {
                stream.Close();
            }
        }

        public async Task<int> ReadFile(Guid file, byte[] buffer, int offset, int readLen)
        {
            if (timeoutValue != 0)
                timeoutStopwatch.Restart();

            if (openFileStreams.TryGetValue(file, out FileStream stream))
            {
                int read = await stream.ReadAsync(buffer, offset, readLen);

                if (stream.Position == stream.Length)
                    CloseStream(file);

                return read;
            }
            else
            {
                if (fileSourceDictionary.TryGetValue(file, out string source))
                {
                    FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
                    openFileStreams.Add(file, fs);
                    int read = await fs.ReadAsync(buffer, offset, readLen);

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

        /*
        public long SeekFile(Guid file, SeekOrigin origin, long offset)
        {
            if (timeoutValue > 0)
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
        }*/
    }

    
}
