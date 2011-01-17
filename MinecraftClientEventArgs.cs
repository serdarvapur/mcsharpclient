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

    public class MinecraftClientChatEventArgs : EventArgs
    {

        public String User, Message;

        public MinecraftClientChatEventArgs(String User, String Message) : base() 
        {
            this.User = User;
            this.Message = Message;
        }
    }
}
