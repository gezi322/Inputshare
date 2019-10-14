using InputshareLib;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using System;

namespace InputshareLibWindows.Clipboard
{
    public class WindowsClipboardManager : ClipboardManagerBase
    {
        private HookWindow cbHookWindow;

        public override void SetClipboardData(ClipboardDataBase data)
        {
            InputshareDataObject obj = null;
            try
            {
                obj = ClipboardTranslatorWindows.ConvertToWindows(data);
            }
            catch(Exception ex)
            {
                ISLogger.Write("Failed to convert clipboard data: " + ex.Message);
            }

            if(obj != null)
                cbHookWindow.SetClipboardData(obj);
        }

        private void HookWnd_ClipboardCallback(System.Windows.Forms.IDataObject data)
        {
            try
            {
                ClipboardDataBase cb = ClipboardTranslatorWindows.ConvertToGeneric(data);
                OnClipboardDataChanged(cb);
            }catch(ClipboardTranslatorWindows.ClipboardTranslationException ex)
            {
                ISLogger.Write("Failed to red clipboard data: " + ex.Message);
            }
            
        }

        public override void Start()
        {
            if (Running)
                throw new InvalidOperationException("Clipboard manager already running");

            cbHookWindow = new HookWindow("ClipboardManager window");
            cbHookWindow.HandleCreated += CbHookWindow_HandleCreated;
            Running = true;
        }

        private void CbHookWindow_HandleCreated(object sender, EventArgs e)
        {
            cbHookWindow.InstallClipboardMonitor(HookWnd_ClipboardCallback);
        }

        public override void Stop()
        {
            if (!Running)
                throw new InvalidOperationException("Clipboard manager not running");

            if (!cbHookWindow.Closed)
            {
                cbHookWindow.CloseWindow();
            }

            Running = false;
        }
    }
}
