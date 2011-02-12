using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCSharpClient
{
    public class MinecraftClientEventArgs : EventArgs
    {
        public MinecraftClientEventArgs() : base() { }
    }

    public class MinecraftClientConnectEventArgs : EventArgs
    {
        public MinecraftClientConnectEventArgs() : base() { }
    }

    public class MinecraftClientDisconnectEventArgs : EventArgs
    {
        public String Reason;

        public MinecraftClientDisconnectEventArgs(String Reason) : base() {
            this.Reason = Reason;
        }
    }

    public class MinecraftClientChatEventArgs : EventArgs
    {
        public String User, Message;

        public MinecraftClientChatEventArgs(String User, String Message)
            : base()
        {
            this.User = User;
            this.Message = Message;
        }
    }

    public class MinecraftClientLocationEventArgs : EventArgs
    {
        public Location PlayerLocation;

        public MinecraftClientLocationEventArgs(Location PlayerLocation)
            : base()
        {
            this.PlayerLocation = PlayerLocation;
        }
    }
}
