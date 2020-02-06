using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages
{
    /// <summary>
    /// Base class for creating network messages
    /// </summary>
    [Serializable]
    internal abstract class NetMessageBase
    {
        internal Guid MessageId { get; }

        internal NetMessageBase(Guid messageId = default)
        {
            MessageId = messageId;
        }
    }
}
