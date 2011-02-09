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
        private NetworkStream Stream;
        private MinecraftServer Server;

        public const int VERSION = 12;
        public Boolean UseAuthentication;
        public Boolean Connected
        {
            get
            {
                return Private_Connected;
            }
            set
            {
                Private_Connected = value;
                if (value == true)
                {
                    OnConnectedToServer(this, new MinecraftClientConnectEventArgs());
                }
                else
                {
                    OnDisconnectedFromServer(this, new MinecraftClientConnectEventArgs());
                }
            }
        }
        public Location PlayerLocation
        {
            get
            {
                return Private_PlayerLocation;
            }
            set 
            {
                Private_PlayerLocation = value;
                OnPlayerLocationChanged(this, new MinecraftClientLocationEventArgs(value));
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
        private Boolean Private_Connected, OnGround;

        public delegate void MinecraftClientConnectEventHandler(object sender, MinecraftClientConnectEventArgs args);
        public delegate void MinecraftClientChatEventHandler(object sender, MinecraftClientChatEventArgs args);
        public delegate void MinecraftClientLocationEventHandler(object sender, MinecraftClientLocationEventArgs args);
        public event MinecraftClientConnectEventHandler ConnectedToServer, DisconnectedFromServer;
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
                catch (Exception e) { Debug.Severe(e); }
            }

            this.Stream = new NetworkStream(MainSocket);
            this.Server = new MinecraftServer(MainSocket);
            this.Server.Password = "Password";

            if (!SendInitialPackets())
            {
                Debug.Severe(new MinecraftClientConnectException());
                return;
            }

            this.Connected = true;
            this.PlayerLocation = null;

            new Thread(HandleData).Start();
        }

        private void HandleData()
        {
            byte id, oldid = 0x00;
            double x, y, z, stance;

            try
            {
                while (MainSocket.Connected && (int)(id = (byte)Stream.ReadByte()) != -1)
                {
                    try
                    {
                        switch (id)
                        {
                            case 0x00: //Ping
                                Stream.WriteByte(0x00);
                                break;
                            case 0x03: //Chat
                                try
                                {
                                    String s = StreamHelper.ReadString(Stream);
                                    if (s[0] != '<') break;
                                    String user = s.Substring(1, s.IndexOf(">") - 1);
                                    String msg = s.Replace("<" + user + "> ", "");
                                    OnChatMessageReceived(this, new MinecraftClientChatEventArgs(user, msg));
                                }
                                catch (IndexOutOfRangeException e) { }
                                break;
                            case 0x04:
                                this.Server.Time = StreamHelper.ReadLong(Stream);
                                break;
                            case 0x05: StreamHelper.ReadBytes(Stream, 10); break;
                            case 0x06:
                                x = StreamHelper.ReadInt(Stream);
                                y = StreamHelper.ReadInt(Stream);
                                z = StreamHelper.ReadInt(Stream);
                                stance = y + 1.6;
                                this.PlayerLocation = new Location(x, y, z, stance);
                                break;
                            case 0x07: StreamHelper.ReadBytes(Stream, 9); break;
                            case 0x08: StreamHelper.ReadBytes(Stream, 2); break;
                            case 0x09: break;
                            case 0x0A: StreamHelper.ReadBytes(Stream, 1); break;
                            case 0x0B: StreamHelper.ReadBytes(Stream, 33); break;
                            case 0x0C: StreamHelper.ReadBytes(Stream, 9); break;
                            case 0x0D:
                                //Set Location
                                float pitch, yaw;
                                x = StreamHelper.ReadDouble(Stream);
                                y = StreamHelper.ReadDouble(Stream);
                                stance = StreamHelper.ReadDouble(Stream);
                                z = StreamHelper.ReadDouble(Stream);
                                pitch = StreamHelper.ReadFloat(Stream);
                                yaw = StreamHelper.ReadFloat(Stream);
                                this.PlayerLocation = new Location(x, y, z, stance);
                                this.OnGround = StreamHelper.ReadBoolean(Stream);
                                break;
                            case 0x0E: StreamHelper.ReadBytes(Stream, 11); break;
                            case 0x0F:
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadBytes(Stream, 1);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadBytes(Stream, 1);
                                short itemid = StreamHelper.ReadShort(Stream);
                                if (itemid > 0)
                                {
                                    byte amount = StreamHelper.ReadBytes(Stream, 1)[0];
                                    short damage = StreamHelper.ReadShort(Stream);
                                }
                                break;
                            case 0x10: StreamHelper.ReadBytes(Stream, 2); break;
                            case 0x12: StreamHelper.ReadBytes(Stream, 5); break;
                            case 0x13: StreamHelper.ReadBytes(Stream, 5); break;
                            case 0x14:
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadString(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadBytes(Stream, 2);
                                StreamHelper.ReadShort(Stream);
                                break;
                            case 0x15: StreamHelper.ReadBytes(Stream, 24); break;
                            case 0x16: StreamHelper.ReadBytes(Stream, 8); break;
                            case 0x17: StreamHelper.ReadBytes(Stream, 17); break;
                            case 0x18: //
                                StreamHelper.ReadBytes(Stream, 19);
                                while ((id = (byte)Stream.ReadByte()) != 0x7f) { } //Metadata
                                break;
                            case 0x19:
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadString(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                break;
                            case 0x1C: StreamHelper.ReadBytes(Stream, 10); break;
                            case 0x1D: StreamHelper.ReadBytes(Stream, 4); break;
                            case 0x1E: StreamHelper.ReadBytes(Stream, 4); break;
                            case 0x1F: StreamHelper.ReadBytes(Stream, 7); break;
                            case 0x20: StreamHelper.ReadBytes(Stream, 6); break;
                            case 0x21: StreamHelper.ReadBytes(Stream, 9); break;
                            case 0x22: StreamHelper.ReadBytes(Stream, 18); break;
                            case 0x26: StreamHelper.ReadBytes(Stream, 5); break;
                            case 0x27: StreamHelper.ReadBytes(Stream, 8); break;
                            case 0x28:
                                StreamHelper.ReadInt(Stream);
                                while ((id = (byte)Stream.ReadByte()) != 0x7f) { } //Metadata
                                break;
                            case 0x32: StreamHelper.ReadBytes(Stream, 9); break;
                            case 0x33: 
                                StreamHelper.ReadBytes(Stream, 13);
                                int size = StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadBytes(Stream, size);
                                break;
                            case 0x34:
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                short length = StreamHelper.ReadShort(Stream); //Number of elements per array
                                StreamHelper.ReadBytes(Stream, length * 2); //Short array (coordinates)
                                StreamHelper.ReadBytes(Stream, length); //Byte array (types)
                                for (int i = 0; i < length; i++) //Metadata array
                                {
                                    StreamHelper.ReadShort(Stream);
                                    Stream.ReadByte();
                                    Stream.ReadByte();
                                }
                                break;
                            case 0x35: StreamHelper.ReadBytes(Stream, 11); break;
                            case 0x3C:
                                StreamHelper.ReadDouble(Stream);
                                StreamHelper.ReadDouble(Stream);
                                StreamHelper.ReadDouble(Stream);
                                StreamHelper.ReadFloat(Stream);
                                int count = StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadBytes(Stream, count * 3);
                                break;
                            case 0x64: StreamHelper.ReadBytes(Stream, 3); break;
                            case 0x65: StreamHelper.ReadBytes(Stream, 1); break;
                            case 0x66: StreamHelper.ReadBytes(Stream, 8); break;
                            case 0x67: StreamHelper.ReadBytes(Stream, 5); break;
                            case 0x68: StreamHelper.ReadBytes(Stream, 3); break;
                            case 0x69: StreamHelper.ReadBytes(Stream, 5); break;
                            case 0x6A: StreamHelper.ReadBytes(Stream, 4); break;
                            case 0x82:
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadShort(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadString(Stream);
                                StreamHelper.ReadString(Stream);
                                StreamHelper.ReadString(Stream);
                                break;
                            case 0xFF:
                                String reason = StreamHelper.ReadString(Stream);
                                if (reason.Length < 5) break;
                                Debug.Warning("Received disconnect packet. Reason: " + reason);
                                break;
                            default:
                                //Debug.Warning("Unknown packet received. [" + (int)id + "]");
                                break;
                        }
                        oldid = id;
                    }
                    catch (Exception e) { Debug.Warning(e); }
                }
            }
            catch (Exception e) { Debug.Severe(new MinecraftClientGeneralException(e)); }

            Connected = false;

            Debug.Info("Disconnected from server.");
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
                //handshake (client)
                this.Stream.WriteByte(0x02);
                StreamHelper.WriteString(Stream, this.Username);
                this.Stream.Flush();

                //handshake (server)
                this.Stream.ReadByte();
                this.Server.Hash = StreamHelper.ReadString(Stream); //hash

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

                //login (client)
                this.Stream.WriteByte(0x01);
                StreamHelper.WriteInt(this.Stream, 8); //version
                StreamHelper.WriteString(this.Stream, this.Username); //username
                StreamHelper.WriteString(this.Stream, this.Server.Password); //server password
                StreamHelper.WriteLong(this.Stream, 0L); //not used
                this.Stream.WriteByte(0x00); //not used
                this.Stream.Flush();

                //login (server)
                this.Stream.ReadByte();
                this.EntityID = StreamHelper.ReadInt(this.Stream); //entity id
                this.Server.ServerName = StreamHelper.ReadString(this.Stream); //server name
                this.Server.ServerMOTD = StreamHelper.ReadString(this.Stream); //motd
                this.Server.MapSeed = StreamHelper.ReadLong(this.Stream); //map seed
                this.Stream.ReadByte(); //dimension

                this.Stream.WriteByte(0x00);
                this.Stream.Flush();

                return true;
            }
            catch (Exception e) { Debug.Severe(e); return false; }
        }

        protected void OnConnectedToServer(object sender, MinecraftClientConnectEventArgs args)
        {
            if (ConnectedToServer != null)
            {
                ConnectedToServer(sender, args);
            }
        }

        protected void OnDisconnectedFromServer(object sender, MinecraftClientConnectEventArgs args)
        {
            if (DisconnectedFromServer != null)
            {
               DisconnectedFromServer(sender, args);
            }
        }

        protected void OnChatMessageReceived(object sender, MinecraftClientChatEventArgs args)
        {
            if (DisconnectedFromServer != null)
            {
                ChatMessageReceived(this, new MinecraftClientChatEventArgs(args.User, args.Message));
            }
        }

        protected void OnPlayerLocationChanged(object sender, MinecraftClientLocationEventArgs args)
        {
            if (PlayerLocationChanged != null && this.Connected)
            {
                PlayerLocationChanged(sender, args);
            }
        }

        public void SetPlayerLocation(Location PlayerLocation) //Currently does nothing
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
