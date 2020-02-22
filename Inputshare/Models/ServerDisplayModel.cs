using Inputshare.Common;
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

        private ServerDisplayModel _leftDisplay = ServerDisplayModel.None;
        public ServerDisplayModel LeftDisplay { get => _leftDisplay; set => SetDisplayAtSide(Side.Left, value); }

        private ServerDisplayModel _rightDisplay = ServerDisplayModel.None;
        public ServerDisplayModel RightDisplay { get => _rightDisplay; set => SetDisplayAtSide(Side.Right, value); }

        private ServerDisplayModel _topDisplay = ServerDisplayModel.None;
        public ServerDisplayModel TopDisplay { get => _topDisplay; set => SetDisplayAtSide(Side.Top, value); }

        private ServerDisplayModel _bottomDisplay = ServerDisplayModel.None;
        public ServerDisplayModel BottomDisplay { get => _bottomDisplay; set => SetDisplayAtSide(Side.Bottom, value); }

        private DisplayBase _display;
        private ObservableCollection<ServerDisplayModel> _list;
        public ServerDisplayModel(DisplayBase display, ObservableCollection<ServerDisplayModel> modelList)
        {
            if (display == null)
            {
                DisplayName = "None";
                return;
            }

            _list = modelList;
            _display = display;
            DisplayName = _display.DisplayName;
            _display.DisplayAtSideChanged += OnDisplaySideChanged;
            RefreshSides();
        }

        public static ServerDisplayModel None { get; } = new ServerDisplayModel();

        private ServerDisplayModel()
        {
            DisplayName = "None";
        }

        private void SetDisplayAtSide(Side side, ServerDisplayModel display)
        {
            Console.WriteLine("set display at side");
            if (display == null)
            {
                Console.WriteLine("Ignoring null display " + side);
                return;
            }


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
            Console.WriteLine("side changed");
            RefreshSides();


            switch (e)
            {
                case Common.Side.Left:
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LeftDisplay)));
                        Console.WriteLine("Property changed to " + LeftDisplay.ToString());
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
            Console.WriteLine("Refresh " + DisplayName);

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
                var ret = _list.Where(i => i.DisplayName == display.DisplayName).FirstOrDefault();

                if (ret == null)
                    throw new Exception("Client not found");
                return ret;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
                return true;
            else
                return false;
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
