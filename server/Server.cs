using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace server {

    class Server {

        private int port;
        private int maxClients;

        private TcpListener listener = null; 

        public Dictionary<int, Client> clients = new Dictionary<int, Client>();

        public delegate void PacketHandler(int _from, Packet _packet);
        public Dictionary<int, PacketHandler> packetHandlers;

        public Action OnUpdate;
        public Action<int> OnPlayerConnected;
        public Action<int> OnPlayerDisconnected;

        public int TickRate { get; set; }
        public int MillisecoundsBetweenTicks { get { return 1000/TickRate; } }

        private bool isRunning = false;

        private Thread mainThread;

        public int ConnectedClients { 
            get 
            { 
                int count = 0;

                for(int i = 0; i < clients.Count; i++)
                {
                    if(clients[i].tcp.socket == null)
                        continue;

                    count++;
                }
                
                return count;
            }
        }

        public Server(int _port, int _maxClients = 10, int _tickRate = 30)
        {
            this.port = _port;
            this.maxClients = _maxClients;

            listener = new TcpListener(IPAddress.Any, port);

            this.packetHandlers = new Dictionary<int, PacketHandler>();

            this.TickRate = _tickRate;
        }

        public void PlayerDisconnected(int _id)
        {
            if(OnPlayerDisconnected != null)
                OnPlayerDisconnected(_id);
        }

        public void Start()
        {
            if(this.isRunning)
                return;

            this.isRunning = true;

            if(listener != null)
                listener.Start();

            listener.BeginAcceptTcpClient(AcceptTCPClientCallback, null);

            for(int i = 0; i < maxClients; i++)
                this.clients.Add(i, new Client(this, i));

           this.mainThread = new Thread(new ThreadStart(Run)); 
           this.mainThread.Start();
        }

        public void Shutdown()
        {
            this.isRunning = false;
            this.mainThread.Abort();
        }

        private void Run()
        {
            DateTime nextLoop = DateTime.Now;

            while(this.isRunning)
            {
                while(nextLoop < DateTime.Now)
                {
                    if(this.OnUpdate != null)
                        this.OnUpdate();

                    nextLoop = nextLoop.AddMilliseconds(this.MillisecoundsBetweenTicks);

                    if(nextLoop > DateTime.Now)
                        Thread.Sleep(nextLoop - DateTime.Now);
                }
            }
        }

        
        private void AcceptTCPClientCallback(IAsyncResult _result) {
        
            TcpClient client = listener.EndAcceptTcpClient(_result);

            for(int i = 0; i < maxClients; i++)
            {
                if(this.clients[i].tcp.socket == null)
                {
                    this.clients[i].tcp.Connect(client);
                    if(OnPlayerConnected != null)
                        OnPlayerConnected(this.clients[i].Id);
                    
                    listener.BeginAcceptTcpClient(AcceptTCPClientCallback, null);
                    
                    return;
                }
            }

            // Create new client and send "Server full"
            // Disconnect client

            listener.BeginAcceptTcpClient(AcceptTCPClientCallback, null);
            Console.WriteLine("Server full");
        }

        public void AddPacketHandler(int _id, PacketHandler _handler)
        {
            if(this.packetHandlers.ContainsKey(_id))
                throw new Exception($"Packet Handler with id {_id} already exists");

            packetHandlers.Add(_id, _handler);
        }
        
        public void SendTo(int _clientId, Packet _packet)
        {
           _packet.WriteLength();

           this.clients[_clientId].tcp.SendPacket(_packet);
        }

        public void SendToAll(Packet _packet)
        {
           _packet.WriteLength();

           for(int i = 0; i < maxClients; i++)
                this.clients[i].tcp.SendPacket(_packet);
        }

        public void SendToAllExcept(int _clientId, Packet _packet)
        {
           _packet.WriteLength();

           for(int i = 0; i < maxClients; i++)
           {
               if(i != _clientId)
                   this.clients[i].tcp.SendPacket(_packet);
           }
        }

        public void HandlePacket(int _packetId, int _fromId, Packet _packet)
        {
            if(!this.packetHandlers.ContainsKey(_packetId))
                throw new Exception("No Packet Handler for packetId " + _packetId);

            this.packetHandlers[_packetId](_fromId, _packet);
        }
    }
}
