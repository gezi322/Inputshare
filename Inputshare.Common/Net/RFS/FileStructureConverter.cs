using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

namespace Inputshare.Common.Net.RFS
{
    /// <summary>
    /// Converts a list of folder & files into 
    /// relative and absolute paths.
    /// 
    /// Used when reading copied files.
    /// </summary>
    internal static class FileStructureConverter
    {
        /// <summary>
        /// Converts a list of file source paths into a group of file headers
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        internal static RFSFileHeader[] CreateFileHeaders(string[] sources)
        {
            CreateRelativePathList(sources, out var relPaths, out var absPaths);

            RFSFileHeader[] headers = new RFSFileHeader[relPaths.Length];
            for (int i = 0; i < absPaths.Length; i++)
            {
                try
                {
                    FileInfo file = new FileInfo(absPaths[i]);
                    headers[i] = new RFSFileHeader(Guid.NewGuid(), file.Name, file.Length, relPaths[i], absPaths[i]);
                }
                catch (Exception ex)
                {
                    Logger.Verbose($"Could not copy file {absPaths[i]} : {ex.Message}");
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
        private static void CreateRelativePathList(string[] originalSources, out string[] relativePaths, out string[] fullPaths)
        {
            List<string> fullPathsList = new List<string>();
            List<string> relativePathsList = new List<string>();

            int fileCount = 0;
            foreach (var fileUnfiltered in originalSources)
            {
                if (string.IsNullOrEmpty(fileUnfiltered))
                    continue;

                var file = HttpUtility.UrlDecode(fileUnfiltered);

                if (File.GetAttributes(file).HasFlag(FileAttributes.Directory))
                {
                    Logger.Verbose($"Adding directory {file}");
                    AddFilesRecursive(fullPathsList, relativePathsList, file, "./" + new DirectoryInfo(file).Name, ref fileCount);
                }
                else
                {
                   
                    fullPathsList.Add(file);
                    var relPath = "./" + new FileInfo(file).Name;
                    relativePathsList.Add(relPath);
                    Logger.Verbose($"Added file {file} (relative path: {relPath}");
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
        private static void AddFilesRecursive(List<string> fullPaths, List<string> relativePaths, string currentPath, string relativePath, ref int count)
        {
            if (count > 10 * 1000)
                throw new InvalidDataException("Too many files");


            foreach (var path in Directory.GetFileSystemEntries(currentPath))
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
                        var rel = relativePath + "/" + new FileInfo(path).Name;
                        fullPaths.Add(path);
                        count++;
                        relativePaths.Add(rel);
                    }
                }
                catch (Exception ex) when (!(ex is InvalidDataException))
                {
                    Logger.Verbose("Failed to add path " + path + ": " + ex.Message);
                }
            }
        }
    }
}
