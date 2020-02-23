using System;
using System.Collections.Generic;
using System.Text;

namespace Inputshare.Common.PlatformModules
{
    /// <summary>
    /// A dependency that is shared between modules, EG an X server connection
    /// </summary>
    public interface IPlatformDependency : IDisposable
    {
    }
}
