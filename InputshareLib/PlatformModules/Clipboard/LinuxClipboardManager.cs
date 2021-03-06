﻿using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Linux;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static InputshareLib.Clipboard.DataTypes.ClipboardVirtualFileData;
using static InputshareLib.Linux.Native.LibX11;
using static InputshareLib.Linux.Native.LibX11Events;
using FileAttributes = InputshareLib.Clipboard.DataTypes.FileAttributes;

namespace InputshareLib.PlatformModules.Clipboard
{
    class LinuxClipboardManager : ClipboardManagerBase
    {
        private ClipboardDataBase copiedData;

        private byte[] incrBuff;
        private int incrBuffIndex = 0;

        //Common X11 atoms
        private IntPtr atomTargets;
        private IntPtr atomClipboard;
        private IntPtr atomPrimary;

        private IntPtr atomUtf8String;
        private IntPtr atomText;
        private IntPtr atomString;
        private IntPtr atomImagePng;
        private IntPtr atomINCR;
        private IntPtr atomFileUriList;
        //Custom properties for data callback
        private IntPtr atomTextReturn;
        private IntPtr atomImageReturn;
        private IntPtr atomFileReturn;

        private SharedXConnection xConnection;

        public LinuxClipboardManager(SharedXConnection xCon)
        {
            xConnection = xCon;
        }

        private void XConnection_EventArrived(XEvent evt)
        {
            if (evt.type != XEventName.XFixesNotify && evt.AnyEvent.window != xConnection.XCbWindow)
            {
                return;
            }

            switch (evt.type)
            {
                case XEventName.SelectionRequest:
                    HandleSelectionRequest(evt.SelectionRequestEvent);
                    break;
                case XEventName.XFixesNotify:
                    HandleXFixesNotify(evt.XFixesNotifyEvent);
                    break;
                case XEventName.SelectionNotify:
                    HandleSelectionNotify(evt.SelectionEvent);
                    break;
                case XEventName.PropertyNotify:
                    HandlePropertyChange(evt.PropertyEvent);
                    break;
                case XEventName.SelectionClear:
                    HandleSelectionClear();
                    break;
            }
            XFlush(xConnection.XDisplay);
        }

        public override void SetClipboardData(ClipboardDataBase data)
        {
            if(data.DataType == ClipboardDataType.File)
            {
                ISLogger.Write("{0}: Data type {1} not supported", ModuleName, data.DataType);
                return;
            }

            copiedData = data;
            XSetSelectionOwner(xConnection.XDisplay, atomClipboard, xConnection.XCbWindow);
            if (XGetSelectionOwner(xConnection.XDisplay, atomClipboard) != xConnection.XCbWindow)
                ISLogger.Write("{0}: Failed to set selection owner!", ModuleName);
        }

        protected override void OnStart()
        {
            InitWindow();
            xConnection.EventArrived += XConnection_EventArrived;
        }

        protected override void OnStop()
        {
            xConnection.EventArrived -= XConnection_EventArrived;
        }

       

        private void InitWindow()
        {
            InitAtoms();
            XFlush(xConnection.XDisplay);

            XFixesSelectSelectionInput(xConnection.XDisplay, xConnection.XCbWindow, atomClipboard, 1);
            //XFixesSelectSelectionInput(xConnection.XDisplay, xConnection.xConnection.XWindow, atomPrimary, 1); TODO - implement 'primary' selections (middle mouse paste)

            //XSelectInput(xConnection.XDisplay, xConnection.XWindow, EventMask.PropertyChangeMask); 
            //Because we are sharing one connection for all classes, using XSelectInput may break others.

            XFlush(xConnection.XDisplay);
            XStoreName(xConnection.XDisplay, xConnection.XCbWindow, "InputshareWindow");
        }

        private void HandlePropertyChange(XPropertyEvent evt)
        {
            //event.state = 0 means a new property was added, 1 means a property was deleted, we don't care if a property got deleted.
            if (incrBuff == null || evt.atom != atomImageReturn || evt.state != 0)
            {
                return;
            }
            try
            {
                int ret = XGetWindowProperty(xConnection.XDisplay, xConnection.XCbWindow, evt.atom, 0, 0, true, new IntPtr(0), out IntPtr retType,
               out int format, out int nItems, out int dataSize, out IntPtr prop_return);
                XFree(prop_return);
                int rem = incrBuff.Length - incrBuffIndex;

                if (rem < dataSize)
                    dataSize = rem;

                XGetWindowProperty(xConnection.XDisplay, xConnection.XCbWindow, evt.atom, 0, rem, false, new IntPtr(0), out IntPtr returned_type,
                    out format, out nItems, out int remBytes, out prop_return);

                if (dataSize == 0)
                {
                    if (incrBuffIndex != 0)
                    {
                        ISLogger.Write("INCR transfer failed ({0}/{1})", incrBuffIndex, incrBuff.Length);
                        incrBuff = null;
                        incrBuffIndex = 0;
                    }

                    XFree(prop_return);
                    return;
                }

                Marshal.Copy(prop_return, incrBuff, incrBuffIndex, dataSize);
                incrBuffIndex += dataSize;
                ISLogger.Write("Complete: {0}/{1}", incrBuffIndex, incrBuff.Length);
                XDeleteProperty(xConnection.XDisplay, xConnection.XCbWindow, evt.atom);
                XFree(prop_return);
                XFlush(xConnection.XDisplay);
                if (incrBuff.Length == incrBuffIndex && incrBuff.Length > 0)
                {
                    ISLogger.Write("image transfer complete!");
                    OnClipboardDataChanged(new ClipboardImageData(incrBuff, true));
                    incrBuffIndex = 0;
                    incrBuff = null;
                }
            }
            catch (Exception ex)
            {
                ISLogger.Write("{0}: INCR transfer error: {1}", ModuleName, ex.Message);
            }

        }

        private void HandleXFixesNotify(XFixesSelectionNotifyEvent evt)
        {
            if(evt.selection == atomPrimary){
                ISLogger.Write("Primary selection changed");
            }else{
                ISLogger.Write("Clipboard selection changed");
            }

            IntPtr cbOwner = XGetSelectionOwner(xConnection.XDisplay, atomClipboard);

            //Ignore the event if our window set the clipboard data
            if (cbOwner == xConnection.XCbWindow)
                return;

            ISLogger.Write("Requesting TARGET atoms...");
            //We want a list of TARGET atoms so that we know what type of data the owner holds
            RequestTargets(cbOwner);
        }

        private void HandleSelectionNotify(XSelectionEvent evt)
        {
            if (evt.property == atomTargets)
                HandleReceivedTargets(evt);
            else if (evt.property == atomTextReturn)
                HandleReceivedText(atomTextReturn);
            else if (evt.property == atomImageReturn)
                HandleReceivedImage(evt);
            else if (evt.property == atomFileReturn)
                HandleReceivedFileDrop(evt);
        }

        private void HandleReceivedFileDrop(XSelectionEvent evt)
        {
            try
            {
                int ret = XGetWindowProperty(xConnection.XDisplay, xConnection.XCbWindow, evt.property, 0, 0, false, new IntPtr(0), out IntPtr retType,
                   out int format, out int nItems, out int dataSize, out IntPtr prop_return);
                XFree(prop_return);

                XGetWindowProperty(xConnection.XDisplay, xConnection.XCbWindow, evt.property, 0, dataSize, false, new IntPtr(0), out IntPtr returned_type,
                    out format, out nItems, out int remBytes, out prop_return);

                string rawFileList = Marshal.PtrToStringAuto(prop_return, nItems);

                ISLogger.Write(rawFileList);

                string[] list = rawFileList.Split('\n');

                string[] sanList = new string[list.Length];
                for (int i = 0; i < list.Length; i++)
                {

                    sanList[i] = list[i].Trim().Replace("file://", "");
                }

                XFree(prop_return);

                //Now we need to get the data for the listed files...

                ClipboardVirtualFileData files = ReadFileDrop(sanList);
                OnClipboardDataChanged(files);

            }catch(Exception ex)
            {
                ISLogger.Write("An error occurred while handling a clipboard file drop: " + ex.Message);
            }
        }

        private static ClipboardVirtualFileData ReadFileDrop(string[] data)
        {
            string[] files = data;
            DirectoryAttributes root = new DirectoryAttributes("", new List<FileAttributes>(), new List<DirectoryAttributes>(), "");
            int fCount = 0;
            foreach (var file in files)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(file))
                        continue;

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
                catch (Exception ex)
                {
                    ISLogger.Write("An error occurred while reading attributes for {0}. File not copied: {1}", file, ex.Message);
                    ISLogger.Write(ex.StackTrace);
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
                catch (Exception ex)
                {
                    ISLogger.Write("An error occurred while reading directory {0}. {1}", subd, ex.Message);
                }

            }
            return folder;
        }

        private void HandleSelectionClear()
        {
            copiedData = null;
        }

        private void HandleReceivedImage(XSelectionEvent evt)
        {
            try
            {
                int ret = XGetWindowProperty(xConnection.XDisplay, xConnection.XCbWindow, evt.property, 0, 0, false, new IntPtr(0), out IntPtr retType,
              out int format, out int nItems, out int dataSize, out IntPtr prop_return);
                XFree(prop_return);

                XGetWindowProperty(xConnection.XDisplay, xConnection.XCbWindow, evt.property, 0, dataSize, false, new IntPtr(0), out IntPtr returned_type,
                    out format, out nItems, out int remBytes, out prop_return);

                if (returned_type == XInternAtom(xConnection.XDisplay, "INCR", false))
                {
                    BeginImageINCRTransfer(evt, returned_type, prop_return);
                    return;
                }

                byte[] imgData = new byte[nItems];
                Marshal.Copy(prop_return, imgData, 0, nItems);
                XFree(prop_return);

                OnClipboardDataChanged(new ClipboardImageData(imgData, true));
            }
            catch (Exception ex)
            {
                ISLogger.Write("{0}: failed to handle received image: {1}", ModuleName, ex.Message);
            }

        }

        private void BeginImageINCRTransfer(XSelectionEvent evt, IntPtr returned_type, IntPtr prop_return)
        {
            ISLogger.Write("Begining INCR data transfer with {0} bytes", Marshal.ReadInt32(prop_return));
            int size = Marshal.ReadInt32(prop_return);
            incrBuff = new byte[size];
            incrBuffIndex = 0;
            XDeleteProperty(xConnection.XDisplay, xConnection.XCbWindow, evt.property);
            ISLogger.Write("Property deleted");
        }

        private void HandleReceivedText(IntPtr property)
        {
            int ret = XGetWindowProperty(xConnection.XDisplay, xConnection.XCbWindow, property, 0, 0, false, new IntPtr(0), out IntPtr retType,
                out int format, out int nItems, out int dataSize, out IntPtr prop_return);
            XFree(prop_return);

            XGetWindowProperty(xConnection.XDisplay, xConnection.XCbWindow, property, 0, dataSize, false, new IntPtr(0), out IntPtr returned_type,
                out format, out nItems, out int remBytes, out prop_return);

            if (remBytes > 0)
                ISLogger.Write("{0}: WARNING - XGetWindowProperty partial read", ModuleName);

            if (prop_return == new IntPtr(0))
            {
                ISLogger.Write("Failed to read clipboard text");
                return;
            }

            string text = Marshal.PtrToStringUTF8(prop_return, nItems);
            XFree(prop_return);
            OnClipboardDataChanged(new ClipboardTextData(text));
        }

        private void HandleReceivedTargets(XSelectionEvent evt)
        {
            IntPtr owner = evt.requestor;

            //Get the size in bytes of the return data. 
            XGetWindowProperty(xConnection.XDisplay, owner, atomTargets, 0, 0, false, new IntPtr(0), out _, out _, out _, out int dataSize, out IntPtr _);

            XGetWindowProperty(xConnection.XDisplay, owner, atomTargets, 0, dataSize, false, new IntPtr(0), out IntPtr retType, out int format,
               out int numItems, out int remBytes, out IntPtr buff);

            //Even though XGetWindowProperty shows the return items as 32 bit, atoms are 64 bit on 64 bit systems and we 
            //only support 64 bit
            if (format == 32)
                format = 64;

            
            long[] targets = new long[numItems];
            for (int i = 0; i < numItems; i++)
            {
                int offset = i * (format / 8);
                targets[i] = Marshal.ReadInt64(buff, offset);

                if (IsFormatFileDrop((IntPtr)targets[i]))
                {
                    RequestSelection((IntPtr)targets[i], owner, atomFileReturn);
                    XFree(buff);
                    return;
                }

                //TODO - implement priority
                if (IsFormatImage((IntPtr)targets[i]))
                {
                    RequestSelection((IntPtr)targets[i], owner, atomImageReturn);
                    XFree(buff);
                    return;
                }
                else if (IsFormatText((IntPtr)targets[i]))
                {
                    RequestSelection((IntPtr)targets[i], owner, atomTextReturn);
                    XFree(buff);
                    return;
                }
            }
        }

        private bool IsFormatFileDrop(IntPtr target)
        {
            return target == atomFileUriList;
        }

        private void RequestSelection(IntPtr dataType, IntPtr ownerWindow, IntPtr returnAtom)
        {
            //Request text from client

            XEvent ev = new XEvent();
            ev.type = XEventName.SelectionRequest;
            ev.SelectionRequestEvent.display = xConnection.XDisplay;
            ev.SelectionRequestEvent.owner = ownerWindow;
            ev.SelectionRequestEvent.requestor = xConnection.XCbWindow;
            ev.SelectionRequestEvent.selection = atomClipboard;
            ev.SelectionRequestEvent.target = dataType;

            XConvertSelection(xConnection.XDisplay, atomClipboard, dataType, returnAtom, xConnection.XCbWindow, new IntPtr(0));        }

        /// <summary>
        /// Requests a window to sent supported clipboard formats
        /// </summary>
        /// <param name="window"></param>
        private void RequestTargets(IntPtr window)
        {
            XEvent evt = new XEvent();
            evt.type = XEventName.SelectionRequest;
            evt.SelectionRequestEvent.display = xConnection.XDisplay;
            evt.SelectionRequestEvent.owner = window;
            evt.SelectionRequestEvent.selection = atomClipboard;
            evt.SelectionRequestEvent.requestor = xConnection.XCbWindow;
            evt.SelectionRequestEvent.property = atomTargets;
            evt.SelectionRequestEvent.target = atomTargets;

            XSendEvent(xConnection.XDisplay, window, true, 0, ref evt);        
        }

        private void HandleSelectionRequest(XSelectionRequestEvent evt)
        {
            XEvent retEvent = new XEvent();
            retEvent.type = XEventName.SelectionNotify;
            retEvent.SelectionEvent.display = evt.display;
            retEvent.SelectionEvent.requestor = evt.requestor;
            retEvent.SelectionEvent.selection = evt.selection;
            retEvent.SelectionEvent.time = evt.time;
            retEvent.SelectionEvent.target = evt.target;
            retEvent.SelectionEvent.property = evt.property;

            if (evt.target == atomTargets)
                HandleTargetRequest(evt);

            if (IsFormatText(evt.target))
                HandleRequestString(evt);

            if (IsFormatImage(evt.target))
                HandleRequestImage(evt);

            XSendEvent(xConnection.XDisplay, retEvent.SelectionEvent.requestor, false, 0, ref retEvent);
        }

        private void HandleRequestString(XSelectionRequestEvent evt)
        {
            if (copiedData == null || copiedData.DataType != ClipboardDataType.Text)
            {
                ISLogger.Write("{0}: cannot handle text request: Copied data is not text", ModuleName);
                return;
            }

            ClipboardTextData data = (ClipboardTextData)copiedData;
            XChangeProperty(xConnection.XDisplay, evt.requestor, evt.property, atomUtf8String, 8, 0, Encoding.UTF8.GetBytes(data.Text), data.Text.Length);
        }

        private void HandleRequestImage(XSelectionRequestEvent evt)
        {
            if (copiedData == null || copiedData.DataType != ClipboardDataType.Image)
            {
                ISLogger.Write("{0}: cannot handle text request: Copied data is not image", ModuleName);
                return;
            }

            ClipboardImageData data = (ClipboardImageData)copiedData;
            XChangeProperty(xConnection.XDisplay, evt.requestor, evt.property, atomImagePng, 8, 0, data.ImageData, data.ImageData.Length);
        }

        private void HandleTargetRequest(XSelectionRequestEvent evt)
        {
            if (copiedData == null)
            {
                ISLogger.Write("Ignoring target request... no data is copied");
                return;
            }

            byte[] raw = new byte[0];
            if (copiedData.DataType == ClipboardDataType.Text)
                raw = GetAtomsBytesText();
            else if (copiedData.DataType == ClipboardDataType.Image)
                raw = GetAtomsBytesImage();

            XChangeProperty(xConnection.XDisplay, evt.requestor, evt.property, new IntPtr(4), 32, 0, raw, 1);
        }

        private byte[] GetAtomsBytesText()
        {
            long[] allowedAtoms = new long[]
            {
                (long)atomString,
                (long)atomText,
                (long)atomUtf8String,
            };

            return LongArrayToByteArray(allowedAtoms);
        }

        private byte[] GetAtomsBytesImage()
        {
            long[] allowedAtoms = new long[]
            {
                (long)atomImagePng
            };

            return LongArrayToByteArray(allowedAtoms);
        }

        private byte[] LongArrayToByteArray(long[] array)
        {
            List<byte> bytes = new List<byte>();
            foreach (var item in array)
            {
                foreach (var b in BitConverter.GetBytes(item))
                    bytes.Add(b);
            }

            return bytes.ToArray();
        }

        private void InitAtoms()
        {
            atomClipboard = XInternAtom(xConnection.XDisplay, "CLIPBOARD", false);
            atomPrimary = XInternAtom(xConnection.XDisplay, "PRIMARY", false);
            atomTargets = XInternAtom(xConnection.XDisplay, "TARGETS", false);

            atomUtf8String = XInternAtom(xConnection.XDisplay, "UTF8_STRING", false);
            atomText = XInternAtom(xConnection.XDisplay, "TEXT", false);
            atomString = XInternAtom(xConnection.XDisplay, "STRING", false);
            atomImagePng = XInternAtom(xConnection.XDisplay, "image/png", false);
            atomFileReturn = XInternAtom(xConnection.XDisplay, "cbReturnPropFile", false);
            atomTextReturn = XInternAtom(xConnection.XDisplay, "cbReturnPropText", false);
            atomImageReturn = XInternAtom(xConnection.XDisplay, "cbReturnPropImage", false);
            atomINCR = XInternAtom(xConnection.XDisplay, "INCR", false);

            atomFileUriList = XInternAtom(xConnection.XDisplay, "text/uri-list", false);
        }

        private string GetAtomName(IntPtr atom)
        {
            return XGetAtomName(xConnection.XDisplay, atom);
        }

        private string GetWindowName(IntPtr window)
        {
            XFetchName(xConnection.XDisplay, window, out string name);
            return name;
        }

        private bool IsFormatText(IntPtr atom)
        {
            return (atom == atomString || atom == atomText || atom == atomUtf8String);
        }

        private bool IsFormatImage(IntPtr atom)
        {
            return (atom == atomImagePng);
        }
    }
}
