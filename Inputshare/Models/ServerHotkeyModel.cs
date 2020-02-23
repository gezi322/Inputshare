using Inputshare.Common.Input.Hotkeys;
using Inputshare.Common.Input.Keys;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Models
{
    /// <summary>
    /// Interops between avalonia key/modifiers and inputshare key/modifiers
    /// </summary>
    public class ServerHotkeyModel
    {
        public bool Alt { get => HasMod(KeyModifiers.Alt); set => SetMod(KeyModifiers.Alt, value); }
        public bool Ctrl { get => HasMod(KeyModifiers.Ctrl); set => SetMod(KeyModifiers.Ctrl, value); }
        public bool Shift { get => HasMod(KeyModifiers.Shift); set => SetMod(KeyModifiers.Shift, value); }
        public bool Windows { get => HasMod(KeyModifiers.Win); set => SetMod(KeyModifiers.Win, value); }
        public WindowsVirtualKey Key { get; set; }

        private KeyModifiers _currentMods;

        public ServerHotkeyModel()
        {
            _currentMods = 0;
            Key = 0;
        }

        public ServerHotkeyModel(Hotkey hk)
        {
            _currentMods = hk.Modifiers;
            Key = hk.Key;
        }

        public Hotkey GetInputshareHotkey()
        {
            return new Hotkey(Key, _currentMods);
        }

        private bool HasMod(KeyModifiers mod)
        {
            return _currentMods.HasFlag(mod);
        }

        private void SetMod(KeyModifiers mod, bool set)
        {
            if (set)
                _currentMods |= mod;
            else
                _currentMods &= ~mod;
        }

        /*
        private Common.Input.Hotkeys.KeyModifiers ConvertToInputshareModifiers(KeyModifiers mods)
        {
            Common.Input.Hotkeys.KeyModifiers isMods = 0;

            isMods = mods.HasFlag(KeyModifiers.Alt) ? isMods |= Common.Input.Hotkeys.KeyModifiers.Alt : isMods;
            isMods = mods.HasFlag(KeyModifiers.Control) ? isMods |= Common.Input.Hotkeys.KeyModifiers.Ctrl : isMods;
            isMods = mods.HasFlag(KeyModifiers.Meta) ? isMods |= Common.Input.Hotkeys.KeyModifiers.Win : isMods;
            isMods = mods.HasFlag(KeyModifiers.Shift) ? isMods |= Common.Input.Hotkeys.KeyModifiers.Shift : isMods;

            return isMods;
        }

        private WindowsVirtualKey ConvertToInputshareKey(Key key)
        {
            //TODO
            return (WindowsVirtualKey)Enum.Parse(typeof(WindowsVirtualKey), key.ToString());
        }*/
    }
}
