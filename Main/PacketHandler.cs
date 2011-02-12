using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace MCSharpClient
{
    public class PacketHandler
    {

        private MinecraftClient Client;
        private NetworkStream Stream;

        // Switch Vars
        private short itemid, length;
        private int size, count;
        private String s, user, msg;

        public PacketHandler(MinecraftClient c)
        {
            Client = c;
            Stream = Client.Stream;
        }

        public void HandlePacket(PacketType id)
        {
            // TODO: Clean up and add methods for some types of packets.

            switch (id)
            {
                case PacketType.KeepAlive:
                    Stream.WriteByte((byte)PacketType.KeepAlive);
                    break;
                case PacketType.ChatMessage:
                    s = StreamHelper.ReadString(Stream);
                    if (s[0] != '<') break;
                    user = s.Substring(1, s.IndexOf(">") - 1);
                    msg = s.Replace("<" + user + "> ", "");
                    Client.OnChatMessageReceived(Client, new MinecraftClientChatEventArgs(user, msg));
                    break;
                case PacketType.TimeUpdate:
                    Client.GetServer().Time = StreamHelper.ReadLong(Stream);
                    break;
                case PacketType.EntityEquipment: StreamHelper.ReadBytes(Stream, 10); break;
                case PacketType.SpawnPosition: StreamHelper.ReadBytes(Stream, 12); break; // Removed
                case PacketType.UseEntity: StreamHelper.ReadBytes(Stream, 9); break;
                case PacketType.UpdateHealth: StreamHelper.ReadBytes(Stream, 2); break;
                case PacketType.Respawn: break;
                // case PacketType.Player: StreamHelper.ReadBytes(Stream, 1); break; (Client->Server Only)
                case PacketType.PlayerPosition: StreamHelper.ReadBytes(Stream, 33); break;
                case PacketType.PlayerLook: StreamHelper.ReadBytes(Stream, 9); break;
                case PacketType.PlayerPositionLook: StreamHelper.ReadBytes(Stream, 41); break; // Removed
                case PacketType.PlayerDigging: StreamHelper.ReadBytes(Stream, 11); break;
                case PacketType.PlayerBlockPlacement:
                    StreamHelper.ReadInt(Stream);
                    StreamHelper.ReadBytes(Stream, 1);
                    StreamHelper.ReadInt(Stream);
                    StreamHelper.ReadBytes(Stream, 1);
                    itemid = StreamHelper.ReadShort(Stream);
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
                    size = StreamHelper.ReadInt(Stream);
                    StreamHelper.ReadBytes(Stream, size);
                    break;
                case PacketType.MultiBlockChange:
                    StreamHelper.ReadInt(Stream);
                    StreamHelper.ReadInt(Stream);
                    length = StreamHelper.ReadShort(Stream); // Number of elements per array
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
                    count = StreamHelper.ReadInt(Stream);
                    StreamHelper.ReadBytes(Stream, count * 3);
                    break;
                case PacketType.OpenWindow: StreamHelper.ReadBytes(Stream, 3); break;
                // case PacketType.CloseWindow: StreamHelper.ReadBytes(Stream, 1); break; (Client->Server Only)
                // case PacketType.WindowClick: StreamHelper.ReadBytes(Stream, 8); break; (Client->Server Only)
                case PacketType.SetSlot: StreamHelper.ReadBytes(Stream, 5); break;
                case PacketType.WindowItems:
                    Stream.ReadByte(); // Window ID
                    short c = StreamHelper.ReadShort(Stream); // Count
                    for (int i = 0; i < c; i++) // Payload
                    {
                        itemid = StreamHelper.ReadShort(Stream);
                        if (itemid != -1)
                        {
                            Stream.ReadByte();
                            StreamHelper.ReadShort(Stream);
                        }
                    }
                    break;
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
                    Client.Disconnect(reason);
                    break;
                default:
                    Debug.Warning("Unknown packet received. [" + (int)id + "]");
                    break;
            }
        }

    }
}
