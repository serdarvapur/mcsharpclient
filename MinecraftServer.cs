using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace MCSharpClient
{
    public class MinecraftServer
    {

        private Socket Socket;

        public IPEndPoint ServerAddress;
        public String ServerName, ServerMOTD, Password, Hash;
        public long MapSeed;

        public MinecraftServer(Socket socket)
        {
            this.Socket = socket;
            this.ServerAddress = (IPEndPoint)this.Socket.RemoteEndPoint;
        }
    }
}
