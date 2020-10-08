using System;

using Andras.Net;
using Andras.Net.Server;

namespace server
{
    class Program
    {
        enum RecivePackets {
            Welcome = 1,
            WelcomeRecived,
            udpTest,
            udpRecived
        }

        static void Main(string[] args)
        {
            Server server = new Server(9000, 10, 30, true);

            server.OnUDPClientConnected += (int _id) => {
                Packet p = new Packet((int)RecivePackets.udpTest);

                p.Write("This is a udp test");

                server.SendTo(_id, p, true);
            };

            server.AddPacketHandler((int)RecivePackets.udpRecived, (int _from, Packet _packet) => {
                string message = _packet.ReadString();

                Console.WriteLine(message);
            });

            server.Start();

        }
    }
}
