# Examples #

Below is an example of how to setup a sample client (w/o authentication). It connects, notifies on connect/disconnect, displays chat messages, and displays location updates.

```
class Program
    {
        static void Main(string[] args)
        {
            MinecraftClient mc = new MinecraftClient("TestUser", "", new IPEndPoint(IPAddress.Parse("127.0.0.1"), 25565));

            mc.ConnectedToServer += new MinecraftClient.MinecraftClientConnectEventHandler(mc_ConnectedToServer);
            mc.DisconnectedFromServer += new MinecraftClient.MinecraftClientDisconnectEventHandler(mc_DisconnectedFromServer);
            mc.ChatMessageReceived += new MinecraftClient.MinecraftClientChatEventHandler(mc_ChatMessageReceived);

            Console.WriteLine("Connecting...");
            mc.Connect();

            Console.ReadLine();
            Environment.Exit(0);
        }

        static void mc_ChatMessageReceived(object sender, MinecraftClientChatEventArgs args)
        {
            Console.WriteLine("<" + args.User + "> " + args.Message);
        }

        static void mc_DisconnectedFromServer(object sender, MinecraftClientDisconnectEventArgs args)
        {
            Console.WriteLine("Disconnected from server: " + args.Reason);
        }

        static void mc_ConnectedToServer(object sender, MinecraftClientConnectEventArgs args)
        {
            Console.WriteLine("Connected to server.");
        }
    }
```