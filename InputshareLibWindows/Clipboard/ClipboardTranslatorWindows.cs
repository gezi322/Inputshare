﻿using InputshareLib;
using InputshareLib.Clipboard.DataTypes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using DirectoryAttributes = InputshareLib.Clipboard.DataTypes.DirectoryAttributes;
using FileAttributes = InputshareLib.Clipboard.DataTypes.FileAttributes;

namespace InputshareLibWindows.Clipboard
{
    public static class ClipboardTranslatorWindows
    {
        /// <summary>
        /// Converts a window dataobject to an inputshare clipboard data object
        /// </summary>
        /// <param name="data"></param>
        /// <param name="attempt"></param>
        /// <returns></returns>
        public static ClipboardDataBase ConvertToGeneric(System.Windows.Forms.IDataObject data, int attempt = 0)
        {
            try
            {
                System.Windows.Forms.DataObject obj = data as System.Windows.Forms.DataObject;
                if (data.GetDataPresent(DataFormats.Bitmap, true))
                {
                    using (Image i = obj.GetImage())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            i.Save(ms, Settings.ImageEncodeFormat);
                            return new ClipboardImageData(ms.ToArray(), true);
                        }
                    }
                }
                else if (data.GetDataPresent(DataFormats.Text))
                {
                    return new ClipboardTextData((string)data.GetData(DataFormats.UnicodeText));
                }
                else if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    return ReadFileDrop(data);
                }

                else
                {
                    ISLogger.Write("possible data formats: ");
                    foreach(var format in data.GetFormats()){
                        ISLogger.Write(format);
                    }

                    throw new ClipboardTranslationException("Dataobject not implemented");
                }
            }
            catch (COMException ex)
            {
                ISLogger.Write("COM exception: " + ex.Message);
                Thread.Sleep(25);
                if (attempt > 10)
                {
                    throw new Exception("Could not read clipboard after 10 attempts.");
                }

                int n = attempt + 1;
                return ConvertToGeneric(data, n);
            }
        }

        private static ClipboardVirtualFileData ReadFileDrop(System.Windows.Forms.IDataObject data)
        {
            string[] files = (string[])data.GetData(DataFormats.FileDrop);
            DirectoryAttributes root = new DirectoryAttributes("", new List<FileAttributes>(), new List<DirectoryAttributes>(), "");
            int fCount = 0;
            foreach (var file in files)
            {
                try
                {
                    System.IO.FileAttributes fa = System.IO.File.GetAttributes(file);
                    if (fa.HasFlag(System.IO.FileAttributes.Directory))
                    {
                        DirectoryAttributes di = new DirectoryAttributes(new DirectoryInfo(file));
                        root.SubFolders.Add(di);

                        foreach (var baseFile in Directory.GetFiles(file))
                        {
                            FileAttributes df = new FileAttributes(new FileInfo(baseFile));
                            df.RelativePath = Path.Combine(di.Name, df.FileName);
                            di.Files.Add(df);
                        }

                    }
                    else
                    {
                        root.Files.Add(new FileAttributes(new FileInfo(file)));
                    }

                }
                catch (ClipboardTranslationException ex)
                {
                    ISLogger.Write("An error occurred while reading attributes for {0}. File not copied\n{1}", file, ex.Message);
                }
            }

            foreach (var folder in root.SubFolders)
            {
                AddDirectoriesRecursive(folder, folder.Name, ref fCount);
            }

            return new ClipboardVirtualFileData(root);
        }
        private static DirectoryAttributes AddDirectoriesRecursive(DirectoryAttributes folder, string current, ref int fCount)
        {
            //current = Path.Combine(current, folder.RelativePath);
            foreach (var subd in Directory.GetDirectories(folder.FullPath))
            {
                try
                {

                    //Todo - this is a cheap fix to stop massive folders with thousands of files being copied
                    //File transfer needs to be changed to allow better support for a large ammount of files
                    if (fCount > Settings.MaxFileTransferFiles)
                        throw new ClipboardTranslationException("File count limit reached - " + fCount);

                    DirectoryAttributes subda = new DirectoryAttributes(new DirectoryInfo(subd));
                    string p = Path.Combine(current, subda.Name);
                    subda.RelativePath = p;
                    folder.SubFolders.Add(subda);

                    foreach (var subf in Directory.GetFiles(subd))
                    {
                        FileAttributes a = new FileAttributes(new FileInfo(subf));
                        fCount++;
                        a.RelativePath = Path.Combine(p, a.FileName);
                        subda.Files.Add(a);
                    }

                    AddDirectoriesRecursive(subda, Path.Combine(current, subda.Name), ref fCount);
                }
                catch (UnauthorizedAccessException ex)
                {
                    ISLogger.Write("An error occurred while reading directory {0}.\n{1}", subd, ex.Message);
                }

                
            }
            return folder;
        }

        public class ClipboardTranslationException : Exception
        {
            public ClipboardTranslationException(string message) : base(message)
            {

            }
        }
    }




}
