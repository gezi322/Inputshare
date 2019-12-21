using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;
using ReactiveUI;

namespace Inputshare.ViewModels
{
    public abstract class ViewModelBase : ReactiveObject
    {
        private string _bottomButtonText = "";
        public string BottomButtonText
        {
            get
            {
                return _bottomButtonText;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _bottomButtonText, value);
            }
        }

        public abstract void HandleBottomButtonPressed();

        public virtual void HandleKeyPress(KeyEventArgs args)
        {

        }

        public abstract void HandleExit();
    }
}
