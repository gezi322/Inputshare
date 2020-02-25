using Inputshare.Common.Clipboard;
using Inputshare.Common.PlatformModules.Base;
using Inputshare.Common.PlatformModules.Linux;
using Inputshare.Common.PlatformModules.Linux.Clipboard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11;
using static Inputshare.Common.PlatformModules.Linux.Native.LibX11Structs;
using static Inputshare.Common.PlatformModules.Linux.Native.LibXfixes;

namespace Inputshare.Common.PlatformModules.Linux.Modules
{
    public class X11ClipboardModule : ClipboardModuleBase
    {
        public override event EventHandler<ClipboardData> ClipboardChanged;

        private readonly XConnection _connection;
        private readonly IntPtr _xDisplay;
        private readonly IntPtr _xRootWindow;
        private readonly X11AtomList _atoms;
        private IntPtr _xWindow;

        private X11ClipboardReader _reader;
        private X11ClipboardWriter _writer;

        public X11ClipboardModule(XConnection connection)
        {
            _connection = connection;
            _xDisplay = connection.XDisplay;
            _xRootWindow = XDefaultRootWindow(_xDisplay);
            _atoms = new X11AtomList(_xDisplay);
        }

        private void OnINCRImageReceived(object sender, byte[] e)
        {
            ClipboardData cbData = new ClipboardData();
            cbData.SetBitmap(e);
            ClipboardChanged?.Invoke(this, cbData);
        }

        public override Task SetClipboardAsync(ClipboardData cbData)
        {
            _writer.SetClipboard(cbData);
            return Task.CompletedTask;
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

            Logger.Debug($"{ModuleName}: Handling selection request for type {GetAtomName(evt.target)}");

            if (evt.target == _atoms.Targets)
                _writer.ReturnCompatibleTargets(evt);
            else if (evt.target == _atoms.String || evt.target == _atoms.Text || evt.target == _atoms.Utf8String)
                _writer.ReturnText(evt);
            else if (evt.target == _atoms.ImagePng)
                _writer.ReturnImage(evt);

            XSendEvent(_xDisplay, retEvent.SelectionEvent.requestor, true, 0, ref retEvent);
        }

        protected override Task OnStart()
        {
            CreateWindow();
            _reader = new X11ClipboardReader(_xDisplay, _xWindow, _atoms);
            _writer = new X11ClipboardWriter(_xDisplay, _xWindow, _atoms);
            _reader.INCRImageReceived += OnINCRImageReceived;
            _connection.EventReceived += OnEventReceived;
            return Task.CompletedTask;
        }
        protected override Task OnStop()
        {
            return Task.CompletedTask;
        }

        private void OnEventReceived(object sender, XEvent evt)
        {
            if (evt.AnyEvent.window != _xWindow)
                return;

            Logger.Verbose($"{ModuleName}: Handling type {evt.type}");

            try
            {
                if (evt.type == XEventName.PropertyNotify && evt.PropertyEvent.atom == _atoms.ImageReturn)
                    _reader.HandleINCRImagePropertySet(evt.PropertyEvent);
                else if (evt.type == XEventName.XFixesNotify)
                    HandleXFixesNotify(evt.XFixesNotifyEvent);
                else if (evt.type == XEventName.SelectionNotify)
                    HandleSelectionNotify(evt.SelectionEvent);
                else if (evt.type == XEventName.SelectionRequest)
                    HandleSelectionRequest(evt.SelectionRequestEvent);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ModuleName}: An error occurred handling event type {evt.type}: {ex.Message}");
                Logger.Error(ex.StackTrace);
            }
        }

        private void HandleSelectionNotify(XSelectionEvent evt)
        {
            var cbData = new ClipboardData();

            //TODO - support reading multiple formats
            if (evt.property == _atoms.Targets)
            {
                var targets = _reader.ReadTargetsList(evt);
                ProcessTargetList(targets, evt.requestor);
            }else if (evt.property == _atoms.TextReturn)
            {
                string text = _reader.ReadText(evt);
                cbData.SetText(text);
                ClipboardChanged?.Invoke(this, cbData);
            }else if (evt.property == _atoms.ImageReturn)
            {
                byte[] image = _reader.ReadImagePng(evt);

                //ReadImagePNG returns 0 bytes if the image is transfered via INCR
                if(image.Length != 0)
                {
                    cbData.SetBitmap(image);
                    ClipboardChanged?.Invoke(this, cbData);
                }
            }else if (evt.property == _atoms.UriListReturn)
            {
                string[] files = _reader.ReadFileDropList(evt);
                cbData.SetLocalFilePaths(files);
                ClipboardChanged?.Invoke(this, cbData);
            }
        }

        private void ProcessTargetList(IntPtr[] targets, IntPtr ownerWindow)
        {
            Logger.Information($"{ModuleName}: Got {targets.Length} targets");

            foreach (var target in targets)
            {
                if (target == _atoms.Text)
                    RequestSelection(target, ownerWindow, _atoms.TextReturn);
                else if (target == _atoms.ImagePng)
                    RequestSelection(target, ownerWindow, _atoms.ImageReturn);
                else if (target == _atoms.UriList)
                    RequestSelection(target, ownerWindow, _atoms.UriListReturn);
            }
        }


        private void RequestSelection(IntPtr dataType, IntPtr owner, IntPtr returnAtom)
        {
            XEvent evt = new XEvent();
            evt.type = XEventName.SelectionRequest;
            evt.SelectionRequestEvent.display = _xDisplay;
            evt.SelectionRequestEvent.owner = owner;
            evt.SelectionRequestEvent.requestor = _xWindow;
            evt.SelectionRequestEvent.selection = _atoms.Clipboard;
            evt.SelectionRequestEvent.target = dataType;
            XConvertSelection(_xDisplay, _atoms.Clipboard, dataType, returnAtom, _xWindow, IntPtr.Zero);
        }

        private void HandleXFixesNotify(XFixesSelectionNotifyEvent evt)
        {
            if (evt.selection == _atoms.Primary)
            {
                Logger.Warning($"{ModuleName}: Primary selection changed (not yet implemented)");
                return;
            }

            if(XGetSelectionOwner(_xDisplay, _atoms.Clipboard) == _xWindow)
            {
                Logger.Debug($"{ModuleName}: Ignoring clipboard change set by current window");
                return;
            }

            Logger.Debug($"{ModuleName}: Clipboard selection changed");
            IntPtr selectionOwner = XGetSelectionOwner(_xDisplay, _atoms.Clipboard);
            _reader.SentTargetsRequest(selectionOwner, _atoms.Clipboard);
        }

        private string GetAtomName(IntPtr atom)
        {
            return XGetAtomName(_xDisplay, atom);
        }

        private void CreateWindow()
        {
            _xWindow = XCreateSimpleWindow(_xDisplay, _xRootWindow, 0, 0, 1, 1, 0, UIntPtr.Zero, UIntPtr.Zero);
            XFlush(_xDisplay);
            XSelectInput(_xDisplay, _xWindow, EventMask.PropertyChangeMask);
            XFixesSelectSelectionInput(_xDisplay, _xWindow, _atoms.Clipboard, 1);
        }
    }
}
