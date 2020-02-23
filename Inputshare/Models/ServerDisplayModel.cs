using Inputshare.Common;
using Inputshare.Common.Input.Hotkeys;
using Inputshare.Common.Server.Display;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;

namespace Inputshare.Models
{
    public class ServerDisplayModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string DisplayName { get; } 
        public Rectangle Bounds { get => _display.DisplayBounds; }

        private ServerHotkeyModel _hotkey = new ServerHotkeyModel();
        public ServerHotkeyModel Hotkey { get => _hotkey; set => SetHotkey(value); }

        private ServerDisplayModel _leftDisplay = ServerDisplayModel.None;
        public ServerDisplayModel LeftDisplay { get => _leftDisplay; set => SetDisplayAtSide(Side.Left, value); }

        private ServerDisplayModel _rightDisplay = ServerDisplayModel.None;
        public ServerDisplayModel RightDisplay { get => _rightDisplay; set => SetDisplayAtSide(Side.Right, value); }

        private ServerDisplayModel _topDisplay = ServerDisplayModel.None;
        public ServerDisplayModel TopDisplay { get => _topDisplay; set => SetDisplayAtSide(Side.Top, value); }

        private ServerDisplayModel _bottomDisplay = ServerDisplayModel.None;
        public ServerDisplayModel BottomDisplay { get => _bottomDisplay; set => SetDisplayAtSide(Side.Bottom, value); }

        private DisplayBase _display;
        private ObservableCollection<ServerDisplayModel> _displayList;
        public ServerDisplayModel(DisplayBase display, ObservableCollection<ServerDisplayModel> modelList)
        {
            if (display == null)
            {
                DisplayName = "None";
                return;
            }

            _displayList = modelList;
            _display = display;
            DisplayName = _display.DisplayName;
            _display.DisplayAtSideChanged += OnDisplaySideChanged;
            _display.HotkeyChanged += OnHotkeychanged;
            RefreshSides();
        }

        private ServerDisplayModel()
        {
            DisplayName = "None";
        }

        private void OnHotkeychanged(object sender, Hotkey hk)
        {
            _hotkey = new ServerHotkeyModel(hk);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Hotkey)));
        }

        private void SetHotkey(ServerHotkeyModel hkModel)
        {
            _display.SetHotkey(hkModel.GetInputshareHotkey());
        }

        public void PushHotkey()
        {
            SetHotkey(_hotkey);
        }

        public static ServerDisplayModel None { get; } = new ServerDisplayModel();

      
        private void SetDisplayAtSide(Side side, ServerDisplayModel display)
        {
            if (display == null)
                return;

            switch (side)
            {
                case Side.Left:
                    if (LeftDisplay == display)
                        return;
                    break;
                case Side.Right:
                    if (RightDisplay == display)
                        return;
                    break;
                case Side.Top:
                    if (TopDisplay == display)
                        return;
                    break;
                case Side.Bottom:
                    if (BottomDisplay == display)
                        return;
                    break;

            }


            _display.SetDisplayAtSide(side, display.DisplayName);
        }

        private void OnDisplaySideChanged(object sender, Common.Side e)
        {
            RefreshSides();


            switch (e)
            {
                case Common.Side.Left:
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LeftDisplay)));
                        break;
                    }
                case Common.Side.Right:
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RightDisplay)));
                        break;
                    }
                case Common.Side.Top:
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopDisplay)));
                        break;
                    }
                case Common.Side.Bottom:
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BottomDisplay)));
                        break;
                    }

            }

            
        }

        private void RefreshSides()
        {
            //Prevent a stackoverflow from calling leftclient.rightclient.leftclient etc
            //instead get the client from the list matching the display name
            _leftDisplay = GetDisplayCopy(_display.GetDisplayAtSide(Common.Side.Left));
            _rightDisplay = GetDisplayCopy(_display.GetDisplayAtSide(Common.Side.Right));
            _bottomDisplay = GetDisplayCopy(_display.GetDisplayAtSide(Common.Side.Bottom));
            _topDisplay = GetDisplayCopy(_display.GetDisplayAtSide(Common.Side.Top));
        }

        private ServerDisplayModel GetDisplayCopy(DisplayBase display)
        {
            if (display == null)
            {
                return ServerDisplayModel.None;
            }
            else
            {
                var ret = _displayList.Where(i => i.DisplayName == display.DisplayName).FirstOrDefault();

                if (ret == null)
                    return ServerDisplayModel.None;

                return ret;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
                return true;

            if (obj == null)
                return false;

            if (!(obj is ServerDisplayModel model))
                return false;

            return model.DisplayName == DisplayName;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
