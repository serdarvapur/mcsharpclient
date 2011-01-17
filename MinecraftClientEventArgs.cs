using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCClientSharp
{
    public class MinecraftClientEventArgs : EventArgs
    {
        public MinecraftClientEventArgs() : base() { }
    }

    public class MinecraftClientConnectEventArgs : EventArgs
    {
        public MinecraftClientConnectEventArgs() : base() { }
    }
}
