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
            byte id;
            double x, y, z, stance;

            try
            {
                while (MainSocket.Connected && (int)(id = (byte)Stream.ReadByte()) != -1)
                {
                    try
                    {
                        switch ((PacketType)id)
                        {
                            case PacketType.KeepAlive:
                                Stream.WriteByte((byte)PacketType.KeepAlive);
                                break;
                            case PacketType.ChatMessage:
                                try // Once the client is 100% stable, this try/catch will be removed.
                                {
                                    String s = StreamHelper.ReadString(Stream);
                                    if (s[0] != '<') break;
                                    String user = s.Substring(1, s.IndexOf(">") - 1);
                                    String msg = s.Replace("<" + user + "> ", "");
                                    OnChatMessageReceived(this, new MinecraftClientChatEventArgs(user, msg));
                                }
                                catch (IndexOutOfRangeException e) { }
                                break;
                            case PacketType.TimeUpdate:
                                this.Server.Time = StreamHelper.ReadLong(Stream);
                                break;
                            case PacketType.EntityEquipment: StreamHelper.ReadBytes(Stream, 10); break;
                            case PacketType.SpawnPosition:
                                x = StreamHelper.ReadInt(Stream);
                                y = StreamHelper.ReadInt(Stream);
                                z = StreamHelper.ReadInt(Stream);
                                stance = y + 1.6;
                                this.PlayerLocation = new Location(x, y, z, stance);
                                break;
                            case PacketType.UseEntity: StreamHelper.ReadBytes(Stream, 9); break;
                            case PacketType.UpdateHealth: StreamHelper.ReadBytes(Stream, 2); break;
                            case PacketType.Respawn: break;
                            // case PacketType.Player: StreamHelper.ReadBytes(Stream, 1); break; (Client->Server Only)
                            case PacketType.PlayerPosition: StreamHelper.ReadBytes(Stream, 33); break;
                            case PacketType.PlayerLook: StreamHelper.ReadBytes(Stream, 9); break;
                            case PacketType.PlayerPositionLook:
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
                            case PacketType.PlayerDigging: StreamHelper.ReadBytes(Stream, 11); break;
                            case PacketType.PlayerBlockPlacement:
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
                            case PacketType.HoldingChange: StreamHelper.ReadBytes(Stream, 2); break;
                            case PacketType.Animation: StreamHelper.ReadBytes(Stream, 5); break;
                            case PacketType.EntityAction: StreamHelper.ReadBytes(Stream, 5); break;
                            case PacketType.NamedEntitySpawn:
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadString(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadBytes(Stream, 2);
                                StreamHelper.ReadShort(Stream);
                                break;
                            case PacketType.PickupSpawn: StreamHelper.ReadBytes(Stream, 24); break;
                            case PacketType.CollectItem: StreamHelper.ReadBytes(Stream, 8); break;
                            case PacketType.AddObjectVehicle: StreamHelper.ReadBytes(Stream, 17); break;
                            case PacketType.MobSpawn:
                                StreamHelper.ReadBytes(Stream, 19);
                                while ((byte)Stream.ReadByte() != 0x7f) { } // Metadata
                                break;
                            case PacketType.EntityPainting:
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadString(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                break;
                            case PacketType.EntityVelocity: StreamHelper.ReadBytes(Stream, 10); break;
                            case PacketType.DestroyEntity: StreamHelper.ReadBytes(Stream, 4); break;
                            case PacketType.Entity: StreamHelper.ReadBytes(Stream, 4); break;
                            case PacketType.EntityRelativeMove: StreamHelper.ReadBytes(Stream, 7); break;
                            case PacketType.EntityLook: StreamHelper.ReadBytes(Stream, 6); break;
                            case PacketType.EntityLookRelativeMove: StreamHelper.ReadBytes(Stream, 9); break;
                            case PacketType.EntityTeleport: StreamHelper.ReadBytes(Stream, 18); break;
                            case PacketType.EntityStatus: StreamHelper.ReadBytes(Stream, 5); break;
                            case PacketType.AttachEntity: StreamHelper.ReadBytes(Stream, 8); break;
                            case PacketType.EntityMetadata:
                                StreamHelper.ReadInt(Stream);
                                while ((byte)Stream.ReadByte() != 0x7f) { } // Metadata
                                break;
                            case PacketType.PreChunk: StreamHelper.ReadBytes(Stream, 9); break;
                            case PacketType.MapChunk: 
                                StreamHelper.ReadBytes(Stream, 13);
                                int size = StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadBytes(Stream, size);
                                break;
                            case PacketType.MultiBlockChange:
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadInt(Stream);
                                short length = StreamHelper.ReadShort(Stream); // Number of elements per array
                                StreamHelper.ReadBytes(Stream, length * 2); // Short array (Coordinates)
                                StreamHelper.ReadBytes(Stream, length); // Byte array (Types)
                                StreamHelper.ReadBytes(Stream, length); // Byte array (Metadata)
                                break;
                            case PacketType.BlockChange: StreamHelper.ReadBytes(Stream, 11); break;
                            case PacketType.PlayNoteBlock: StreamHelper.ReadBytes(Stream, 12); break;
                            case PacketType.Explosion:
                                StreamHelper.ReadDouble(Stream);
                                StreamHelper.ReadDouble(Stream);
                                StreamHelper.ReadDouble(Stream);
                                StreamHelper.ReadFloat(Stream);
                                int count = StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadBytes(Stream, count * 3);
                                break;
                            case PacketType.OpenWindow: StreamHelper.ReadBytes(Stream, 3); break;
                            // case PacketType.CloseWindow: StreamHelper.ReadBytes(Stream, 1); break; (Client->Server Only)
                            // case PacketType.WindowClick: StreamHelper.ReadBytes(Stream, 8); break; (Client->Server Only)
                            case PacketType.SetSlot: StreamHelper.ReadBytes(Stream, 5); break;
                            case PacketType.WindowItems: StreamHelper.ReadBytes(Stream, 3); break;
                            case PacketType.UpdateProgressBar: StreamHelper.ReadBytes(Stream, 5); break;
                            case PacketType.Transaction: StreamHelper.ReadBytes(Stream, 4); break;
                            case PacketType.UpdateSign:
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadShort(Stream);
                                StreamHelper.ReadInt(Stream);
                                StreamHelper.ReadString(Stream);
                                StreamHelper.ReadString(Stream);
                                StreamHelper.ReadString(Stream);
                                break;
                            case PacketType.DisconnectKick:
                                String reason = StreamHelper.ReadString(Stream);
                                Debug.Warning("Received disconnect/kick packet. Reason: " + reason);
                                break;
                            default:
                                //Debug.Warning("Unknown packet received. [" + (int)id + "]");
                                break;
                        }
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

        public void SetPlayerLocation(Location PlayerLocation) // Currently does nothing
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
