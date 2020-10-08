using System;
using System.Net.Sockets;

namespace server {

    class Client 
    {
        public static int dataBufferSize = 4096;

        public int id;
        public int Id { get { return id; } }
        public TCP tcp;
        private Server server;

        public Client(Server _server, int _id)
        {
            this.id = _id;
            server = _server;
            this.tcp = new TCP(_server, _id);

            this.tcp.OnDisconnect = Disconnect;
        }

        void Disconnect()
        {
            tcp.Disconnect();
            server.PlayerDisconnected(id);
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
                this.id = _id;
                this.server = _server;
            }

            public void Connect(TcpClient _socket)
            {
                System.Console.WriteLine("New user id: " + id);
                this.socket = _socket;

                this.socket.ReceiveBufferSize = dataBufferSize;
                this.socket.SendBufferSize = dataBufferSize;

                this.stream = _socket.GetStream();

                this.recivedData = new Packet();
                this.recivedBuffer = new byte[dataBufferSize];

                stream.BeginRead(this.recivedBuffer, 0, dataBufferSize,  this.ReciveCallback, null);
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
                    Array.Copy(this.recivedBuffer, data, byteLength);

                    recivedData.Reset(HandleData(data));
                    stream.BeginRead(this.recivedBuffer, 0, dataBufferSize,  this.ReciveCallback, null);
                }
                catch (System.Exception e)
                {
                    throw new Exception($"Failed to Recive data! Ex: {e.Message}");
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
                    throw new Exception($"Failed to send packet to user {id}! Ex: {e.Message}");
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
                        server.HandlePacket(packetId, this.id, packet);
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

        }

    }
}
