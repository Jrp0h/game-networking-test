using System;

namespace server
{
    class Vector3 : IPacketable<Vector3>
    {
        public int x, y, z;

        public Vector3()
        {
            x = 0;
            y = 0;
            z = 0;
        }

        public Vector3(int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public void Write(ref Packet _packet)
        {
            _packet.Write(x);
            _packet.Write(y);
            _packet.Write(z);
        }

        public Vector3 Read(ref Packet _packet)
        {
            Vector3 v3 = new Vector3();

            v3.x = _packet.ReadInt();
            v3.y = _packet.ReadInt();
            v3.z = _packet.ReadInt();

            return v3;
        }

        public override string ToString()
        {
            return $"Vector3({x}, {y}, {z})";
        }
    } 
}
