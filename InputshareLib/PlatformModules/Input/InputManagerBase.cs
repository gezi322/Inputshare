using InputshareLib.Input;
using InputshareLib.Input.Hotkeys;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InputshareLib.PlatformModules.Input
{
    public abstract class InputManagerBase : PlatformModuleBase
    {
        public abstract event EventHandler<ISInputData> InputReceived;
        public event EventHandler<Guid> ClientHotkeyPressed;
        public event EventHandler<Hotkeyfunction> FunctionHotkeyPressed;

        public abstract bool LeftMouseDown { get; }
        public abstract bool InputBlocked { get; protected set; }
        public abstract MouseInputMode MouseRecordMode { get; protected set; }



        protected List<Hotkey> hotkeys = new List<Hotkey>();

        protected HotkeyModifiers currentActiveModifiers;

        /// <summary>
        /// Add or removes a hotkey for a client
        /// </summary>
        /// <exception cref="ArgumentException">Hotkey already in use</exception>
        /// <param name="hotkey"></param>
        public void AddUpdateClientHotkey(ClientHotkey hotkey)
        {
            if (CheckHotkeyInUse(hotkey))
                throw new ArgumentException("Hotkey already in use");

            if (GetClientHotkey(hotkey.TargetClient) != null)
            {
                hotkeys.Remove(GetClientHotkey(hotkey.TargetClient));
            }

            ISLogger.Write("Added client hotkey " + hotkey);
            hotkeys.Add(hotkey);
        }

        public abstract void SetMouseInputMode(MouseInputMode mode, int interval = 0);

        /// <summary>
        /// Adds or updates a current function hotkey
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="hotkey"></param>
        public void AddUpdateFunctionHotkey(FunctionHotkey hotkey)
        {
            if (CheckHotkeyInUse(hotkey))
                throw new ArgumentException("Hotkey already in use");

            if (GetFunctionHotkey(hotkey.Function) != null)
            {
                RemoveFunctionHotkey(hotkey.Function);
            }

            ISLogger.Write("Assigned {0} to {1}", hotkey, hotkey.Function);
            hotkeys.Add(hotkey);
        }

        /// <summary>
        /// Removes a hotkey bound to a client
        /// </summary>
        /// <exception cref="ArgumentException>">Client hotkey not found</exception>
        /// <param name="targetClient">Guid of the client to remove the hotkey from</param>
        public void RemoveClientHotkey(Guid targetClient)
        {
            ClientHotkey chk = GetClientHotkey(targetClient);

            if (chk == null)
                throw new ArgumentException("Client hotkey not found");

            hotkeys.Remove(chk);
        }

        /// <summary>
        /// Returns the hotkey for the specified client guid (NULL IF NOT FOUND)
        /// </summary>
        /// <param name="targetClient">Guid of the target client hotkey</param>
        /// <returns></returns>
        public ClientHotkey GetClientHotkey(Guid targetClient)
        {
            return (ClientHotkey)hotkeys.Where(key => key.GetType() == typeof(ClientHotkey))
                .Where(key => ((ClientHotkey)key).TargetClient == targetClient).FirstOrDefault();
        }

        /// <summary>
        /// Returns the hotkey bound to the specified function (NULL IF NOT FOUND)
        /// </summary>
        /// <param name="function">Function of hotkey</param>
        /// <returns></returns>
        public FunctionHotkey GetFunctionHotkey(Hotkeyfunction function)
        {
            return (FunctionHotkey)hotkeys.Where(key => key.GetType() == typeof(FunctionHotkey))
                .Where(key => ((FunctionHotkey)key).Function == function).FirstOrDefault();
        }

        /// <summary>
        /// Removes a hotkey for the specified function
        /// </summary>
        /// <exception cref="ArgumentException">hotkey not found</exception>"
        /// <param name="function">Function hotkey to remove</param>
        public void RemoveFunctionHotkey(Hotkeyfunction function)
        {
            FunctionHotkey fhk = GetFunctionHotkey(function);

            if (fhk == null)
                throw new ArgumentNullException("Hotkey not found");

            hotkeys.Remove(fhk);
        }

        /// <summary>
        /// Returns true if the specified key combination is already in use
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool CheckHotkeyInUse(Hotkey key)
        {
            if (hotkeys.Where(a => a == key).FirstOrDefault() == null)
                return false;
            else
                return true;
        }

        public List<Hotkey> GetallHotkeys()
        {
            return new List<Hotkey>(hotkeys.ToArray());
        }


        protected void OnFunctionHotkeyPressed(Hotkeyfunction function)
        {
            FunctionHotkeyPressed?.Invoke(this, function);
        }

        protected void OnClientHotkeyPressed(Guid client)
        {
            ClientHotkeyPressed?.Invoke(this, client);
        }

        /// <summary>
        /// if true, Disables local input and fires the InputReceived event 
        /// when an input is performed.
        /// </summary>
        /// <param name="block"></param>
        public abstract void SetInputBlocked(bool block);
    }
}
