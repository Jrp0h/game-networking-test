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
            PlayerDisconnected,
            NewMessage
        }

        static Dictionary<int, Player> players = new Dictionary<int, Player>();

        static void Main(string[] args)
        {
            Server server = new Server(9000);

            server.AddPacketHandler((int)RecivePackets.Welcome, (int _from, Packet _packet) => {
                Console.WriteLine(server.clients[_from].tcp.socket.Client.RemoteEndPoint + " has sent a welcome");

                if(players.ContainsKey(_from))
                    return;

                Player player = new Player(_from, _packet.ReadString());
                players.Add(_from, player);

                Packet packet = new Packet((int)RecivePackets.NewPlayer);
                packet.Write(player);

                System.Console.WriteLine(_from + " : " + player.name);

                server.SendToAllExcept(_from, packet);
            });

            server.AddPacketHandler((int)RecivePackets.NewMessage, (int _from, Packet _packet) => {

                Packet p = new Packet((int)RecivePackets.NewMessage);

                p.Write(_from);
                p.Write(_packet.ReadString());

                server.SendToAllExcept(_from, p);
            });

            server.OnPlayerDisconnected += (int _id) => {
                Packet p1 = new Packet((int)RecivePackets.PlayerDisconnected);
                p1.Write(_id);

                server.SendToAllExcept(_id, p1);

                players.Remove(_id);
            };

            server.OnUpdate += () => {
                // Console.WriteLine("Updating");
            };

            server.OnPlayerConnected += (int _id) => {
                
                System.Console.WriteLine("User connected successfully");

                Packet p1 = new Packet((int)RecivePackets.Welcome);
                p1.Write(_id);

                server.SendTo(_id, p1);

                Packet p3 = new Packet((int)RecivePackets.AlreadyExistingPlayers);

                List<Player> ps = new List<Player>();

                foreach(KeyValuePair<int, Player> entry in players)
                {
                    ps.Add(entry.Value);
                }

                p3.Write(ps.ToArray());

                server.SendTo(_id, p3);
            };

            server.Start();
        }
    }
}
