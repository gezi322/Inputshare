using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace Inputshare.ViewModels
{
    public abstract class ViewModelBase : ReactiveObject
    {
        public virtual event EventHandler Leave;

        public abstract string BottomButtonText { get; protected set; }
        public abstract Task HandleBottomButtonPressAsync();
        public abstract Task HandleWindowClosingAsync();

    }
}
