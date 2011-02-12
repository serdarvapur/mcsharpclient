using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace MCSharpClient
{
    public class MinecraftClient
    {

        private String Username, Password;
        private Socket MainSocket;
        private IPEndPoint ServerAddress;
        private MinecraftServer Server;

        public const int VERSION = 12;
        public NetworkStream Stream;
        public Boolean UseAuthentication;
        public Boolean Connected;
        public Location PlayerLocation
        {
            get
            {
                return Private_PlayerLocation;
            }
            set 
            {
                Private_PlayerLocation = value;
                if (value != null)
                {
                    OnPlayerLocationChanged(this, new MinecraftClientLocationEventArgs(value));
                }
            }
        }
        public Rotation PlayerRotation
        {
            get
            {
                return Private_PlayerRotation;
            }
            set
            {
                Private_PlayerRotation = value;
                //OnPlayerRotationChanged(this, new MinecraftClientLocationEventArgs(value));
            }
        }

        private int EntityID;
        private String SessionID;
        private Location Private_PlayerLocation;
        private Rotation Private_PlayerRotation;
        private PacketHandler PacketHandler;

        public delegate void MinecraftClientConnectEventHandler(object sender, MinecraftClientConnectEventArgs args);
        public delegate void MinecraftClientDisconnectEventHandler(object sender, MinecraftClientDisconnectEventArgs args);
        public delegate void MinecraftClientChatEventHandler(object sender, MinecraftClientChatEventArgs args);
        public delegate void MinecraftClientLocationEventHandler(object sender, MinecraftClientLocationEventArgs args);
        public event MinecraftClientConnectEventHandler ConnectedToServer;
        public event MinecraftClientDisconnectEventHandler DisconnectedFromServer;
        public event MinecraftClientChatEventHandler ChatMessageReceived;
        public event MinecraftClientLocationEventHandler PlayerLocationChanged;
        
        /// <summary>
        ///  Instantiates a new MinecraftClient object. If the server is running in online mode, Username and Password must be valid Minecraft.net account credentials.
        ///  Also, UseAuthentication needs to be set to true.
        ///  Otherwise, Username can be any valid username, Password can be blank, and UseAuthentication should be set to false.
        /// </summary>
        public MinecraftClient(String Username, String Password, IPEndPoint Address)
        {
            this.Username = Username;
            this.Password = Password;
            this.ServerAddress = Address;
            this.SessionID = "";
            this.Connected = false;
        }

        public void Connect()
        {
            if (UseAuthentication)
            {
                this.SessionID = Authenticate();
                if (this.SessionID == "")
                {
                    Debug.Severe(new MinecraftClientConnectException("Authentication Failed."));
                    return;
                }
            }

            this.MainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            while (!MainSocket.Connected)
            {
                try
                {
                    this.MainSocket.Connect(ServerAddress);
                }
                catch (Exception e) { Debug.Severe(e.Message); }
            }

            this.Stream = new NetworkStream(MainSocket);
            this.PacketHandler = new PacketHandler(this);
            this.Server = new MinecraftServer(MainSocket);
            this.Server.Password = "Password";

            if (!SendInitialPackets())
            {
                Debug.Severe(new MinecraftClientConnectException());
                return;
            }

            this.Connected = true;
            this.PlayerLocation = null;

            OnConnectedToServer(this, new MinecraftClientConnectEventArgs());

            new Thread(HandleData).Start();
        }

        private void HandleData()
        {
            byte id;

            try
            {
                while (MainSocket.Connected && (int)(id = (byte)Stream.ReadByte()) != 255)
                {
                    try
                    {
                        PacketHandler.HandlePacket((PacketType)id);
                    }
                    catch (Exception e) { Debug.Warning(e.Message); }
                }
            }
            catch (Exception e) { Debug.Severe(new MinecraftClientGeneralException(e)); }

            if (Connected) // Disconnected due to exception
            {
                Disconnect("Generic Disconnect");
            }
        }

        private String Authenticate()
        {
            WebClient web = new WebClient();
            String data =
                web.DownloadString("http://www.minecraft.net/game/getversion.jsp?user=" + this.Username + "&password=" + this.Password + "&version=" + VERSION);
            
            if (!data.Contains(":"))
            {
                return "";
            }

            return data.Split(':')[3];
        }

        private Boolean CheckServer()
        {
            WebClient web = new WebClient();
            String data =
                web.DownloadString("http://www.minecraft.net/game/joinserver.jsp?user=" + this.Username + "&sessionId=" + this.SessionID + "&serverId=" + this.Server.Hash);

            if (data == "OK")
            {
                return true;
            }

            return false;
        }

        private Boolean SendInitialPackets()
        {
            try
            {
                // Handshake (Client)
                this.Stream.WriteByte((byte)PacketType.Handshake);
                StreamHelper.WriteString(Stream, this.Username);
                this.Stream.Flush();

                // Handshake (Server)
                this.Stream.ReadByte();
                this.Server.Hash = StreamHelper.ReadString(Stream); // Hash

                if (this.Server.Hash != "-" && this.Server.Hash != "+")
                {
                    if (this.SessionID == "")
                    {
                        Debug.Severe(new MinecraftClientConnectException("Server requires authenication but it was not enabled."));
                        return false;
                    }

                    if (!CheckServer())
                    {
                        Debug.Severe(new MinecraftClientConnectException("Name verification failed. How you managed this, I don't even know..."));
                        return false;
                    }
                }

                // Login (Client)
                this.Stream.WriteByte((byte)PacketType.LoginRequest);
                StreamHelper.WriteInt(this.Stream, 8); // Version
                StreamHelper.WriteString(this.Stream, this.Username); // Username
                StreamHelper.WriteString(this.Stream, this.Server.Password); // Server Password
                StreamHelper.WriteLong(this.Stream, 0L); // Not Used
                this.Stream.WriteByte(0x00); // Not Used
                this.Stream.Flush();

                // Login (Server)
                this.Stream.ReadByte();
                this.EntityID = StreamHelper.ReadInt(this.Stream); // Entity ID
                this.Server.ServerName = StreamHelper.ReadString(this.Stream); // Server Name
                this.Server.ServerMOTD = StreamHelper.ReadString(this.Stream); // MOTD
                this.Server.MapSeed = StreamHelper.ReadLong(this.Stream); // Map Seed
                this.Stream.ReadByte(); // Dimension

                this.Stream.WriteByte(0x00);
                this.Stream.Flush();

                return true;
            }
            catch (Exception e) { Debug.Severe(e); return false; }
        }

        public void Disconnect(String reason)
        {
            if (this.MainSocket.Connected)
            {
                try
                {
                    this.MainSocket.Close();
                }
                catch (Exception e)
                {
                    Debug.Warning(e.Message);
                }
            }
            OnDisconnectedFromServer(this, new MinecraftClientDisconnectEventArgs(reason));
            Connected = false;
        }

        public void OnConnectedToServer(object sender, MinecraftClientConnectEventArgs args)
        {
            if (ConnectedToServer != null)
            {
                ConnectedToServer(sender, args);
            }
        }

        public void OnDisconnectedFromServer(object sender, MinecraftClientDisconnectEventArgs args)
        {
            if (DisconnectedFromServer != null)
            {
               DisconnectedFromServer(sender, args);
            }
        }

        public void OnChatMessageReceived(object sender, MinecraftClientChatEventArgs args)
        {
            if (DisconnectedFromServer != null)
            {
                ChatMessageReceived(this, new MinecraftClientChatEventArgs(args.User, args.Message));
            }
        }

        public void OnPlayerLocationChanged(object sender, MinecraftClientLocationEventArgs args)
        {
            if (PlayerLocationChanged != null && this.Connected)
            {
                PlayerLocationChanged(sender, args);
            }
        }

        public void SetPlayerLocation(Location PlayerLocation) // Currently Does Nothing
        {
            this.PlayerLocation = PlayerLocation;
            OnPlayerLocationChanged(this, new MinecraftClientLocationEventArgs(this.PlayerLocation));
        }

        public MinecraftServer GetServer()
        {
            return this.Server;
        }
    }
}
