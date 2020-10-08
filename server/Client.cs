using System;
using System.Net;
using System.Net.Sockets;

namespace Andras.Net.Server 
{
    class Client 
    {
        public static int dataBufferSize = 4096;

        public int id;
        public int Id { get { return id; } }

        public TCP tcp;
        public UDP udp;

        private Server server;

        public Client(Server _server, int _id)
        {
            id = _id;
            server = _server;
            tcp = new TCP(_server, _id);
            udp = new UDP(_server, _id);

            tcp.OnDisconnect = Disconnect;
        }

        void Disconnect()
        {
            tcp.Disconnect();
            udp.Disconnect();
            server.ClientDisconnected(id);
        }
        
        public class TCP {
            public TcpClient socket;
            
            private Server server;

            private readonly int id;
            private NetworkStream stream;

            private Packet recivedData;
            private byte[] recivedBuffer;

            public Action OnDisconnect;

            public TCP(Server _server, int _id)
            {
                id = _id;
                server = _server;
            }

            public void Connect(TcpClient _socket)
            {
                Console.WriteLine("New user id: " + id);
                socket = _socket;

                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = _socket.GetStream();

                recivedData = new Packet();
                recivedBuffer = new byte[dataBufferSize];

                stream.BeginRead(recivedBuffer, 0, dataBufferSize,  ReciveCallback, null);
            }

            public void Disconnect()
            {
                socket.Close();
                socket = null;
                stream = null;
                recivedData = null;
                recivedBuffer = null;
            }

            private void ReciveCallback(IAsyncResult _result)
            {
                try
                {
                    int byteLength = stream.EndRead(_result);

                    if(byteLength <= 0)
                    {
                        if(OnDisconnect != null)
                            OnDisconnect();

                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(recivedBuffer, data, byteLength);

                    recivedData.Reset(HandleData(data));
                    stream.BeginRead(recivedBuffer, 0, dataBufferSize, ReciveCallback, null);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to Recive data! Ex: {e.Message}");
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
                    throw new Exception($"Failed to send packet to user {id}! Ex: {e.Message}");
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
                        // Call server handler
                        server.HandlePacket(packetId, id, packet);
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

        }

        public class UDP {
            public IPEndPoint endPoint;

            private Server server;

            private int id;

            public UDP(Server _server, int _id)
            {
                id = _id;
                server = _server;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
            }

            public void SendPacket(Packet _packet)
            {
                server.SendUDPPacket(endPoint, _packet);
            }

            public void Disconnect()
            {
                endPoint = null;
            }

            public void HandlePacket(Packet _packet)
            {
                int packetLength = _packet.ReadInt();
                byte[] packetBytes = _packet.ReadBytes(packetLength);

                using(Packet packet = new Packet(packetBytes))
                {
                    int packetId = packet.ReadInt();
                    server.HandlePacket(packetId, id, packet);
                }
            }
        }

    }
}
