using System;

namespace client {

    static class PacketableHandler
    {

        public static Packet Write<T>(IPacketable<T> _thing, Packet _packet) where T : IPacketable<T>, new()
        {
            _thing.Write(ref _packet);

            return _packet;
        }

       public static ReadData<T> Read<T>(IPacketable<T> _thing, Packet _packet) where T : IPacketable<T>, new() 
       {
            T data = _thing.Read(ref _packet);

            return new ReadData<T>(data, _packet.CurrentPosition);
       }
    }

}
