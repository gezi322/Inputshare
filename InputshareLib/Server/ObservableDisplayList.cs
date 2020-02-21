using Inputshare.Common.Server.Display;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Inputshare.Common.Server
{
    public class ObservableDisplayList : ObservableCollection<DisplayBase>
    {
        public event EventHandler<DisplayBase> DisplayAdded;
        public event EventHandler<DisplayBase> DisplayRemoved;

        protected override void InsertItem(int index, DisplayBase item)
        {
            DisplayAdded?.Invoke(this, item);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            if (this.Count > index)
                DisplayRemoved?.Invoke(this, this[index]);

            base.RemoveItem(index);
        }
    }
}
