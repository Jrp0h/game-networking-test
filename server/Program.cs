using System;
using System.Collections.Generic;

namespace server
{

    class Program
    {
        enum RecivePackets {
            Welcome = 1,
            AlreadyExistingPlayers,
            NewPlayer,
            PlayerDisconnected
        }

        static void Main(string[] args)
        {
            Server server = new Server(9000);

            server.AddPacketHandler((int)RecivePackets.Welcome, (int _from, Packet _packet) => {
                Console.WriteLine(server.clients[_from].tcp.socket.Client.RemoteEndPoint + " has set a welcome");
            });

            server.OnPlayerDisconnected += (int _id) => {
                Packet p1 = new Packet((int)RecivePackets.PlayerDisconnected);
                p1.Write(_id);

                server.SendToAllExcept(_id, p1);
            };

            server.OnUpdate += () => {
                // Console.WriteLine("Updating");
            };

            server.OnPlayerConnected += (int _id) => {
                
                System.Console.WriteLine("User connected successfully");

                Packet p1 = new Packet((int)RecivePackets.Welcome);
                p1.Write("Hello, new player åäö yeay :)");
                p1.Write(_id);

                server.SendTo(_id, p1);

                Packet p2 = new Packet((int)RecivePackets.NewPlayer);
                p2.Write(_id);

                server.SendToAllExcept(_id, p2);

                Packet p3 = new Packet((int)RecivePackets.AlreadyExistingPlayers);

                List<Client> clients = server.ConnectedClients;

                if(clients.Count - 1 < 1)
                    return;

                p3.Write(clients.Count - 1);

                for(int i = 0; i < server.clients.Count; i++)
                {
                    if(server.clients[i].Id == _id)
                        continue;

                   p3.Write(server.clients[i].Id); 
                   p3.Write("Hej" + server.clients[i].Id);
                }

                server.SendTo(_id, p3);
            };

            server.Start();
        }
    }
}
