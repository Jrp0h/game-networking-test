using System;

namespace server {

    class Player : IPacketable<Player> {
        public int id;
        public string name;

        public Player()
        {}

        public Player(int _id, string _name)
        {
            id = _id;
            name = _name;
        }

        public void Write(ref Packet _packet)
        {
            _packet.Write(id);
            _packet.Write(name);
        }

        public Player Read(ref Packet _packet)
        {
            Player p = new Player();

            p.id = _packet.ReadInt();
            p.name = _packet.ReadString();

            return p;
        }
    }
}
