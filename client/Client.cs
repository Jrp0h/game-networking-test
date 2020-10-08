using System;
using System.Net;
using System.Net.Sockets;

using System.Collections.Generic;

namespace Andras.Net.Client 
{
    enum ConnectionError {
        CONNECTION_REFUSED = 111
    }

    class Client {
        
        public int id;

        public static int dataBufferSize = 4096;

        private string ip;
        private int port;

        TCP tcp;
        UDP udp;

        bool useUDP;

        public delegate void PacketHandler(Packet _packet);
        public Dictionary<int, PacketHandler> packetHandlers;

        public bool IsConnected { get { return isConnected; } }
        private bool isConnected;

        public Action OnDisconnect;
        public Action OnConnect;
        public Action<ConnectionError> OnConnectionFailed;

        public Client(string _ip, int _port, bool _useUDP = false)
        {
            ip = _ip;
            port = _port;
            useUDP = _useUDP;

            packetHandlers = new Dictionary<int, PacketHandler>();
        }

        public void Connect()
        {
            tcp = new TCP(HandlePacket, Disconnect, OnConnectInvoked, OnConnectionFailedInvoked, (int _id) => { id = _id; });

            isConnected = true;
            tcp.Connect(ip, port);

        }

        private void OnConnectionFailedInvoked(ConnectionError error)
        {
            isConnected = false;

            if(OnConnectionFailed != null)
                OnConnectionFailed(error);
        }

        private void OnConnectInvoked()
        {
            if(useUDP)
            {
                udp = new UDP(ip, port, id, HandlePacket);
                udp.Connect(((IPEndPoint)tcp.socket.Client.LocalEndPoint).Port);
            }

            if(OnConnect != null)
                OnConnect();
        }

        public void AddPacketHandler(int _id, PacketHandler _handler)
        {
            if(_id == 0)
                throw new Exception("Packet Id of 0 is Taken for Client Identification");

            if(packetHandlers.ContainsKey(_id))
                throw new Exception($"Packet Handler with id {_id} already exists");

            packetHandlers.Add(_id, _handler);
        }

        public void HandlePacket(int _packetId, Packet _packet)
        {
            if(packetHandlers == null)
                throw new Exception("No registed packet handlers");

            if(!packetHandlers.ContainsKey(_packetId))
                throw new Exception("No Packet Handler for packetId " + _packetId);

            packetHandlers[_packetId](_packet);
        }

        public void Send(Packet _packet, bool _overUDP = false)
        {
            if(!isConnected)
            {
                Console.WriteLine("Can't send Packets when you aren't connected!");
                return;
            }
            _packet.WriteLength();

            if(_overUDP)
                if(udp != null)
                    udp.SendPacket(_packet);
            else
                tcp.SendPacket(_packet);
        }

        public void Disconnect()
        {
            if(isConnected)
            {
                isConnected = false;
                tcp.socket.Close();

                if(OnDisconnect != null)
                    OnDisconnect();
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
            private Action OnConnect;
            private Action<ConnectionError> OnConnectionFailed;
            public Action<int> SetId;
                
            public TCP(HandlePacket _hp, Action _onDisconnect, Action _onConnect, Action<ConnectionError> _onConnectionFailed, Action<int> _SetId)
            {
                OnHandlePacket = _hp;
                OnDisconnect = _onDisconnect;
                OnConnect = _onConnect;
                OnConnectionFailed = _onConnectionFailed;
                SetId = _SetId;

                recivedData = new Packet();
            }

            public void Connect(string _ip, int _port)
            {
                socket = new TcpClient {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };

                reciveBuffer = new byte[dataBufferSize];
                socket.BeginConnect(_ip, _port, ConnectCallback, null);
            }

            private void ConnectCallback(IAsyncResult _result)
            {
                try
                {
                    socket.EndConnect(_result);
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.ErrorCode);

                    if(OnConnectionFailed != null)
                        OnConnectionFailed((ConnectionError)e.ErrorCode);

                    return;
                }

                stream = socket.GetStream();

                if(OnConnect != null)
                    OnConnect();

                stream.BeginRead(reciveBuffer, 0, dataBufferSize, ReciveCallback, null);


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
                    Array.Copy(reciveBuffer, data, byteLength);

                    recivedData.Reset(HandleData(data));
                    stream.BeginRead(reciveBuffer, 0, dataBufferSize,  ReciveCallback, null);
                }
                catch (Exception ex)
                {
                    // Disconnect();
                    throw new Exception("Failed reciving data! Ex: " + ex.Message);
                }
            }

            public void SendPacket(Packet _packet)
            {
                try
                {
                    if(socket != null)
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length, null, null);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to send packet to server! Ex: {e.Message}");
                }
            }

            private bool HandleData(byte[] _data)
            {
                int packetLength = 0;
                recivedData.SetBytes(_data);

                if(recivedData.UnreadLength >= 4)
                {
                    packetLength = recivedData.ReadInt();

                    if(packetLength <= 0)
                        return true;
                }

                while(packetLength > 0 && packetLength <= recivedData.UnreadLength)
                {
                    byte[] packetsByte = recivedData.ReadBytes(packetLength);

                    // Execute on main thread

                    using (Packet packet = new Packet(packetsByte))
                    {
                        int packetId = packet.ReadInt();

                        if(packetId == 0)
                            SetId(packet.ReadInt()); 
                        else 
                            OnHandlePacket(packetId, packet);
                    }

                    packetLength = 0;

                    if(recivedData.UnreadLength >= 4)
                    {
                        packetLength = recivedData.ReadInt();

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
                OnDisconnect();

                stream = null;
                recivedData = null;
                reciveBuffer = null;
                socket = null;
            }
        }

        public class UDP {
            public UdpClient socket;
            public IPEndPoint endPoint;

            int id;

            public delegate void HandlePacket(int _packetId, Packet _packet);
            private HandlePacket OnHandlePacket;


            public UDP(string _ip, int _port, int _id, HandlePacket _hp)
            {
                endPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
                OnHandlePacket = _hp;
                id = _id;
            }

            public void Connect(int _localPort)
            {
                socket = new UdpClient(_localPort);

                socket.Connect(endPoint);
                socket.BeginReceive(ReciveCallback, null);

                using(Packet packet = new Packet())
                {
                    SendPacket(packet);
                }
            }

            public void SendPacket(Packet _packet)
            {
                try
                {
                    _packet.InsertInt(id);

                    if(socket != null)
                        socket.BeginSend(_packet.ToArray(), _packet.Length, null, null);
                }
                catch (Exception e)
                {
                   throw new Exception("Error sending data to server over UDP: " + e.Message); 
                }
            }

            private void ReciveCallback(IAsyncResult _result)
            {
                try
                {
                    byte[] data = socket.EndReceive(_result, ref endPoint);
                    socket.BeginReceive(ReciveCallback, null);

                    if(data.Length < 4)
                        return;

                    HandleData(data);
                }
                catch (Exception e)
                {
                    // Disconnect?
                    throw new Exception("Failed to recive UDP data: " + e.Message);
                }
            }

            private void HandleData(byte[] _data)
            {
                using(Packet packet = new Packet(_data))
                {
                    int packetLength = packet.ReadInt();
                    _data = packet.ReadBytes(packetLength);
                }

                using(Packet packet = new Packet(_data))
                {
                    int packetId = packet.ReadInt();
                    OnHandlePacket(packetId, packet);
                }
            }
        }
    }
}
