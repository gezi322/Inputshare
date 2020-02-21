using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages
{
    /// <summary>
    /// Base class for creating network messages
    /// </summary>
    [Serializable]
    internal abstract class NetMessageBase
    {
        /// <summary>
        /// If true, the message should be serialize/deserialized without the use
        /// of a binaryformatter
        /// </summary>
        internal virtual bool UseCustomSerialization { get; } = false;
        internal NetMessageBase()
        {

        }
    }
}
