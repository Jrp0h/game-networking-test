using System;
using System.Threading;
using System.Collections.Generic;

namespace client
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

        static Client c;

        static void Main(string[] args)
        {
            Console.Write("Enter a Username: ");
            string name = Console.ReadLine();

            Dictionary<int, Player> chatters = new Dictionary<int, Player>();

            c = new Client("localhost", 9000);

            c.OnConnectionFailed += (ConnectionError error) => {
                Console.WriteLine("Connection Failed");
            };

            c.OnDisconnect += () => {
                Console.WriteLine("Disconnected");
                Environment.Exit(0);    
            };

            c.AddPacketHandler((int)RecivePackets.Welcome, (Packet _packet) => {
                int id = _packet.ReadInt();
                c.id = id;

                Packet p = new Packet((int)RecivePackets.Welcome);

                p.Write(name);
                c.Send(p);
            });

            c.AddPacketHandler((int)RecivePackets.NewPlayer, (Packet _packet) => {

                Player p = _packet.Read<Player>();

                Console.WriteLine(p.name + " joined");
                chatters.Add(p.id, p);
            });

            c.AddPacketHandler((int)RecivePackets.AlreadyExistingPlayers, (Packet _packet) => {
                Player[] players = _packet.ReadArray<Player>();

                if(players.Length <= 0)
                {
                    System.Console.WriteLine("You are alone here");
                    return;
                }

                System.Console.WriteLine("You are chatting with: ");
                for(int i = 0; i < players.Length; i++)
                {
                    chatters.Add(players[i].id, players[i]);
                    Console.WriteLine(players[i].name);
                }
            });

            c.AddPacketHandler((int)RecivePackets.PlayerDisconnected, (Packet _packet) => {
                
                int id = _packet.ReadInt();
                if(!chatters.ContainsKey(id))
                    return;

                System.Console.WriteLine($"{chatters[id].name} disconnected");
                chatters.Remove(id);
            });


            c.AddPacketHandler((int)RecivePackets.NewMessage, (Packet _packet) => {
                
                int chatter = _packet.ReadInt();
                
                if(!chatters.ContainsKey(chatter))
                    return;

                System.Console.WriteLine($"{chatters[chatter].name}: " + _packet.ReadString());
            });

            c.Connect();

            while(c.IsConnected) { 
                Console.Write("You: ");
                string newMessage = Console.ReadLine();
                
                Packet p = new Packet((int)RecivePackets.NewMessage);
                p.Write(newMessage);

                c.Send(p);
            }
        }
    }
}
