using System;
using System.Net;
using System.Net.Sockets;

using System.Collections.Generic;

namespace client {

    class Client {
        
        public int id;

        public static int dataBufferSize = 4096;

        private string ip;
        private int port;

        TCP tcp;

        public delegate void PacketHandler(Packet _packet);
        public Dictionary<int, PacketHandler> packetHandlers;

        public bool IsConnected { get { return isConnected; } }
        private bool isConnected;

        public Client(string _ip, int _port)
        {
            this.ip = _ip;
            this.port = _port;

            packetHandlers = new Dictionary<int, PacketHandler>();
        }

        public void Connect()
        {
            tcp = new TCP(HandlePacket, Disconnect);

            isConnected = true;
            tcp.Connect(ip, port);

        }

        public void AddPacketHandler(int _id, PacketHandler _handler)
        {
            if(this.packetHandlers.ContainsKey(_id))
                throw new Exception($"Packet Handler with id {_id} already exists");

            packetHandlers.Add(_id, _handler);
        }

        public void HandlePacket(int _packetId, Packet _packet)
        {
            if(this.packetHandlers == null)
                throw new Exception("No registed packet handlers");

            if(!this.packetHandlers.ContainsKey(_packetId))
                throw new Exception("No Packet Handler for packetId " + _packetId);

            this.packetHandlers[_packetId](_packet);
        }

        public void Disconnect()
        {
            if(isConnected)
            {
                isConnected = false;
                tcp.socket.Close();
            }
        }

        public class TCP {
            public TcpClient socket;

            private NetworkStream stream;
            private Packet recivedData;
            private byte[] reciveBuffer;

            public delegate void HandlePacket(int _packetId, Packet _packet);

            private HandlePacket OnHandlePacket;
            private Action OnDisconnect;
                
            public TCP(HandlePacket _hp, Action _onDisconnect)
            {
                this.OnHandlePacket = _hp;
                this.OnDisconnect = _onDisconnect;

                recivedData = new Packet();
            }

            public void Connect(string _ip, int _port)
            {
                this.socket = new TcpClient {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };

                this.reciveBuffer = new byte[dataBufferSize];
                this.socket.BeginConnect(_ip, _port, ConnectCallback, null);
            }

            private void ConnectCallback(IAsyncResult _result)
            {
                this.socket.EndConnect(_result);

                this.stream = this.socket.GetStream();

                this.stream.BeginRead(reciveBuffer, 0, dataBufferSize, ReciveCallback, null);
            }

            private void ReciveCallback(IAsyncResult _result)
            {
                try
                {
                    int byteLength = stream.EndRead(_result);

                    if(byteLength <= 0)
                    {
                        Disconnect();
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(this.reciveBuffer, data, byteLength);

                    recivedData.Reset(HandleData(data));
                    stream.BeginRead(this.reciveBuffer, 0, dataBufferSize,  this.ReciveCallback, null);
                }
                catch (System.Exception ex)
                {
                    // Disconnect();
                    throw new Exception("Failed reciving data! Ex: " + ex.Message);
                }
            }

            public void SendPacket(Packet _packet)
            {
                try
                {
                    if(this.socket != null)
                        this.stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
                catch (System.Exception e)
                {
                    throw new Exception($"Failed to send packet to server! Ex: {e.Message}");
                }
            }

            private bool HandleData(byte[] _data)
            {
                int packetLength = 0;
                this.recivedData.SetBytes(_data);

                if(this.recivedData.GetUnreadLength() >= 4)
                {
                    packetLength = this.recivedData.ReadInt();

                    if(packetLength <= 0)
                        return true;
                }

                while(packetLength > 0 && packetLength <= this.recivedData.GetUnreadLength())
                {
                    byte[] packetsByte = this.recivedData.ReadBytes(packetLength);

                    // Execute on main thread

                    using (Packet packet = new Packet(packetsByte))
                    {
                        int packetId = packet.ReadInt();
                        // Call server handler
                        OnHandlePacket(packetId, packet);
                    }

                    packetLength = 0;

                    if(this.recivedData.GetUnreadLength() >= 4)
                    {
                        packetLength = this.recivedData.ReadInt();

                        if(packetLength <= 0)
                            return true;
                    }
                }

                if(packetLength <= 1)
                    return true;

                return false;
            }

            void Disconnect()
            {
                this.OnDisconnect();

                stream = null;
                recivedData = null;
                reciveBuffer = null;
                socket = null;
            }
        }
    }
}
