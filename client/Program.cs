using System;
using System.Threading;
using System.Collections.Generic;

namespace client
{
    class Program
    {
    
        enum RecivePackets {
            Welcome = 1,
            WelcomeRecived,
            udpTest,
            udpRecived
        }

        static Client c;

        static void Main(string[] args)
        {
            c = new Client("127.0.0.1", 9000, true);

            c.OnConnectionFailed += (ConnectionError error) => {
                Console.WriteLine("Connection Failed");
            };

            c.OnDisconnect += () => {
                Console.WriteLine("Disconnected");
                Environment.Exit(0);    
            };

            c.AddPacketHandler((int)RecivePackets.udpTest, (Packet _packet) => {

                string message = _packet.ReadString();
                
                Console.WriteLine(message);

                Packet p = new Packet((int)RecivePackets.udpRecived);
                p.Write("I got the udp package!");

                c.Send(p, true);
            }); 

            c.Connect();

            while(c.IsConnected) { 
                Thread.Sleep(1000);
                


                // Packet p = new Packet((int)RecivePackets.udpRecived);
                // p.Write("I got the udp package!");

                // c.Send(p, true);
            }
        }
    }
}
