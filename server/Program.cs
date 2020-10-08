using System;

namespace server
{

    class Program
    {
        enum RecivePackets {
            Welcome = 1
        }

        static void Main(string[] args)
        {
            Server server = new Server(9000);

            server.AddPacketHandler((int)RecivePackets.Welcome, (int _from, Packet _packet) => {
                Console.WriteLine(server.clients[_from].tcp.socket.Client.RemoteEndPoint + " has set a welcome");
            });

            server.OnUpdate += () => {
                Console.WriteLine("Updating");
            };

            server.Start();
        }
    }
}
