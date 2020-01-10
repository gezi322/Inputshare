using InputshareLib;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.PlatformModules.Clipboard;
using InputshareLibWindows.Clipboard;
using System;
using System.Runtime.InteropServices.ComTypes;

namespace InputshareLibWindows.PlatformModules.Clipboard
{
    public class WindowsClipboardManager : ClipboardManagerBase
    {
        private HookWindow cbHookWindow;
        private ClipboardVirtualFileData currentClipboardFiles;

        protected override void OnStart()
        {
            cbHookWindow = new HookWindow("ClipboardManager window");
            cbHookWindow.HandleCreated += CbHookWindow_HandleCreated;
            cbHookWindow.InitWindow();
        }

        protected override void OnStop()
        {
            if (!cbHookWindow.Closed)
            {
                cbHookWindow.CloseWindow();
            }
        }

        public override void SetClipboardData(ClipboardDataBase data)
        {
            InputshareDataObject obj = new InputshareDataObject(data, false);

            if(data.DataType == ClipboardDataType.File)
            {
                //If two applications paste data, we can't know which of the two we are interacting with.
                //In other words, we don't know if its program A or program B calling GetData() on the InputshareDataObject
                //this means that files will just be corrupted if two programs try to paste the data

                //To fix this, as soon as the data is pasted, we put another InputshareDataObject onto the clipboard.
                //Each dataobject gets their own access token from the host to allow them to have seperate filestreams
                //and allow any number of programs to paste at the same time
                currentClipboardFiles = data as ClipboardVirtualFileData;
                obj.FilesPasted += Obj_ObjectPasted;
            }
            

            cbHookWindow.SetClipboardData(obj);
        }

        private void Obj_ObjectPasted(object sender, EventArgs e)
        {
            if (currentClipboardFiles == null)
                return;

            SetClipboardData(currentClipboardFiles);
        }

        private void HookWnd_ClipboardCallback(System.Windows.Forms.IDataObject data)
        {
            try
            {
                //Check to make sure that we didn't set the clipboard data
                if (data.GetDataPresent("InputshareData"))
                    return;

                ClipboardDataBase cb = ClipboardTranslatorWindows.ConvertToGeneric(data);

                if(cb is ClipboardTextData cbText)
                {
                    ISLogger.Write("Copied " + cbText.Text);
                }

                ISLogger.Write("WindowsClipboardManager: Copied type {0}", cb.DataType);
                OnClipboardDataChanged(cb);
            }catch(ClipboardTranslatorWindows.ClipboardTranslationException ex)
            {
                ISLogger.Write("Failed to read clipboard data: " + ex.Message);
            }
            
        }
        private void CbHookWindow_HandleCreated(object sender, EventArgs e)
        {
            cbHookWindow.InstallClipboardMonitor(HookWnd_ClipboardCallback);
        }
    }
}
