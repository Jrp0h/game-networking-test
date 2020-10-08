using System;

namespace server
{

    public struct ReadData<T> {
        public T data;
        public int bytesRead;

        public ReadData(T _data, int _bytesRead)
        {
            data = _data;
            bytesRead = _bytesRead;
        }
    }

    interface IPacketable<T> where T : new()
    {
        void Write(ref Packet _packet);
        T Read(ref Packet _packet);
    }
}
