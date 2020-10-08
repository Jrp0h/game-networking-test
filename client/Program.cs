using System;
using System.Threading;

namespace client
{
    class Program
    {
    
        enum RecivePackets {
            Welcome = 1,
            AlreadyExistingPlayers,
            NewPlayer,
            PlayerDisconnected
        }

        static Client c;

        static void Main(string[] args)
        {
            c = new Client("localhost", 9000);

            c.AddPacketHandler((int)RecivePackets.Welcome, (Packet _packet) => {
                string msg = _packet.ReadString();
                int id = _packet.ReadInt();

                c.id = id;
                Console.WriteLine(msg);
                Console.WriteLine("My new id is: " + id);
            });

            c.AddPacketHandler((int)RecivePackets.NewPlayer, (Packet _packet) => {
                int id = _packet.ReadInt();

                Console.WriteLine("New player connected: " + id);
            });

            c.AddPacketHandler((int)RecivePackets.AlreadyExistingPlayers, (Packet _packet) => {
                System.Console.WriteLine("Already Existing Players");
                int userCount = _packet.ReadInt();

                for(int i = 0; i < userCount; i++)
                {
                    Console.WriteLine(_packet.ReadInt() + " : " + _packet.ReadString());
                }
            });

            c.AddPacketHandler((int)RecivePackets.PlayerDisconnected, (Packet _packet) => {
                
                int id = _packet.ReadInt();

                System.Console.WriteLine($"User {id} disconnected");
            });

            c.Connect();

            while(c.IsConnected) { Thread.Sleep(1); }
        }
    }
}
