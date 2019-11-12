using InputshareLib;
using InputshareLib.Clipboard;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.PlatformModules.Clipboard;
using System;

namespace InputshareLibWindows.PlatformModules.Clipboard
{
    public class WindowsClipboardManager : ClipboardManagerBase
    {
        private HookWindow cbHookWindow;

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
            InputshareDataObject obj = null;
            try
            {
                obj = ClipboardTranslatorWindows.ConvertToWindows(data, Guid.Empty);
            }
            catch(Exception ex)
            {
                ISLogger.Write("Failed to convert clipboard data: " + ex.Message);
            }

            if(obj != null)
            {
                cbHookWindow.SetClipboardData(obj);
                ISLogger.Write("WindowsClipboardManager: Set clipboard type {0}", data.DataType);
            }
        }

        private void HookWnd_ClipboardCallback(System.Windows.Forms.IDataObject data)
        {
            try
            {
                ClipboardDataBase cb = ClipboardTranslatorWindows.ConvertToGeneric(data);

                if(cb.DataType == ClipboardDataType.File)
                {
                    ISLogger.Write("Copying/Pasting files currently disabled. Ignoring clipboard data");
                    return;
                }

                ISLogger.Write("WindowsClipboardManager: Copied type {0}", cb.DataType);
                OnClipboardDataChanged(cb);
            }catch(ClipboardTranslatorWindows.ClipboardTranslationException ex)
            {
                ISLogger.Write("Failed to red clipboard data: " + ex.Message);
            }
            
        }


        private void CbHookWindow_HandleCreated(object sender, EventArgs e)
        {
            cbHookWindow.InstallClipboardMonitor(HookWnd_ClipboardCallback);
        }
    }
}
