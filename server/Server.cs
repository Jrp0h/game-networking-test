using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Andras.Net.Server
{
    class Server {

        private int port;
        private int maxClients;

        private bool useUDP;

        private TcpListener tcpListener = null; 
        private UdpClient udpListener = null; 

        public Dictionary<int, Client> clients = new Dictionary<int, Client>();

        public delegate void PacketHandler(int _from, Packet _packet);
        public Dictionary<int, PacketHandler> packetHandlers;

        public Action OnUpdate;
        public Action<int> OnClientConnected;
        public Action<int> OnClientDisconnected;
        public Action<int> OnUDPClientConnected;

        public int TickRate { get; set; }
        public int MillisecoundsBetweenTicks { get { return 1000/TickRate; } }

        private bool isRunning = false;

        private Thread mainThread;

        public List<Client> ConnectedClients { 
            get 
            { 
                List<Client> c = new List<Client>();

                for(int i = 0; i < clients.Count; i++)
                {
                    if(clients[i].tcp.socket == null)
                        continue;

                    c.Add(clients[i]);
                }
                
                return c;
            }
        }

        public Server(int _port, int _maxClients = 10, int _tickRate = 30, bool _useUDP = false)
        {
            port = _port;
            maxClients = _maxClients;

            useUDP = _useUDP;

            tcpListener = new TcpListener(IPAddress.Any, port);

            if(_useUDP)
            {
                udpListener = new UdpClient(port);
            }

            packetHandlers = new Dictionary<int, PacketHandler>();

            TickRate = _tickRate;
        }

        public void ClientDisconnected(int _id)
        {
            if(OnClientDisconnected != null)
                OnClientDisconnected(_id);
        }

        public void Start()
        {
            if(isRunning)
                return;

            isRunning = true;

            if(tcpListener != null)
                tcpListener.Start();

            tcpListener.BeginAcceptTcpClient(AcceptTCPClientCallback, null);

            for(int i = 0; i < maxClients; i++)
                clients.Add(i, new Client(this, i));

            if(useUDP)
                udpListener.BeginReceive(UDPReciveCallback, null);

           mainThread = new Thread(new ThreadStart(Run)); 
           mainThread.Start();
        }

        public void Shutdown()
        {
            isRunning = false;
            mainThread.Abort();
        }

        private void Run()
        {
            DateTime nextLoop = DateTime.Now;

            while(isRunning)
            {
                while(nextLoop < DateTime.Now)
                {
                    if(OnUpdate != null)
                        OnUpdate();

                    nextLoop = nextLoop.AddMilliseconds(MillisecoundsBetweenTicks);

                    if(nextLoop > DateTime.Now)
                        Thread.Sleep(nextLoop - DateTime.Now);
                }
            }
        }

        private void UDPReciveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.EndReceive(_result, ref clientEndPoint);
                udpListener.BeginReceive(UDPReciveCallback, null);

                if(data.Length < 4)
                    return;

                using(Packet packet = new Packet(data))
                {
                    int clientId = packet.ReadInt();

                    if(!clients.ContainsKey(clientId))
                        return;

                    if(clients[clientId].udp.endPoint == null)
                    {
                        clients[clientId].udp.Connect(clientEndPoint);

                        if(OnUDPClientConnected != null)
                            OnUDPClientConnected(clientId);
                        return;
                    }

                    if(clients[clientId].udp.endPoint.ToString() == clientEndPoint.ToString())
                        clients[clientId].udp.HandlePacket(packet);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to recive UDP data: " + e.Message);
            }
        }
        
        private void AcceptTCPClientCallback(IAsyncResult _result) {
        
            TcpClient client = tcpListener.EndAcceptTcpClient(_result);

            for(int i = 0; i < maxClients; i++)
            {
                if(clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);

                    // if(useUDP)
                    // {
                        // clients[i].udp.Connect(((IPEndPoint)clients[i].tcp.socket.Client.LocalEndPoint).Port);
                    // }

                    Packet p = new Packet(0);
                    p.Write(i);
                    SendTo(i, p);

                    if(OnClientConnected != null)
                        OnClientConnected(clients[i].Id);
                    
                    tcpListener.BeginAcceptTcpClient(AcceptTCPClientCallback, null);
                    
                    return;
                }
            }

            // Create new client and send "Server full"
            // Disconnect client

            tcpListener.BeginAcceptTcpClient(AcceptTCPClientCallback, null);
            Console.WriteLine("Server full");
        }

        public void AddPacketHandler(int _id, PacketHandler _handler)
        {
            if(packetHandlers.ContainsKey(_id))
                throw new Exception($"Packet Handler with id {_id} already exists");

            packetHandlers.Add(_id, _handler);
        }
        
        public void SendTo(int _clientId, Packet _packet, bool _overUDP = false)
        {
           _packet.WriteLength();

           if(_overUDP)
               clients[_clientId].udp.SendPacket(_packet);
           else 
               clients[_clientId].tcp.SendPacket(_packet);
        }

        public void SendToAll(Packet _packet, bool _overUDP = false)
        {
           _packet.WriteLength();

           for(int i = 0; i < maxClients; i++)
           {
               if(_overUDP)
                   clients[i].udp.SendPacket(_packet);
               else 
                   clients[i].tcp.SendPacket(_packet);
           }
        }

        public void SendToAllExcept(int _clientId, Packet _packet, bool _overUDP = false)
        {
           _packet.WriteLength();

           for(int i = 0; i < maxClients; i++)
           {
               if(i != _clientId)
               {
                   if(_overUDP)
                       clients[i].udp.SendPacket(_packet);
                   else 
                       clients[i].tcp.SendPacket(_packet);
               }
           }
        }

        public void SendUDPPacket(IPEndPoint _clientEndPoint, Packet _packet)
        {
            if(!useUDP)
                throw new Exception("Cannot send UDP Packets if you dont have it enabled");
            try
            {
                if(_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length, _clientEndPoint, null, null);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to send over UDP: " + e.Message);
            }
        }

        public void HandlePacket(int _packetId, int _fromId, Packet _packet)
        {
            if(!packetHandlers.ContainsKey(_packetId))
                throw new Exception("No Packet Handler for packetId " + _packetId);

            packetHandlers[_packetId](_fromId, _packet);
        }
    }
}
