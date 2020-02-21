﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.Net.Messages
{
    [Serializable]
    internal class NetInputClientStateMessage : NetMessageBase
    {
        public NetInputClientStateMessage(bool inputClient)
        {
            InputClient = inputClient;
        }

        public bool InputClient { get; }
    }
}