using System;
using System.Collections.Generic;
using System.Text;

namespace server {

    class Packet : IDisposable {

        private List<byte> buffer;
        private byte[] readableBuffer;

        private int currentReadPosition;
       
        public Packet()
        {
            this.buffer = new List<byte>();
            this.currentReadPosition = 0;
        }

        public Packet(int _id)
        {
            this.buffer = new List<byte>();
            this.currentReadPosition = 0;

            Write(_id);
        }

        public Packet(byte[] _data)
        {
            this.buffer = new List<byte>();
            this.currentReadPosition = 0;

            SetBytes(_data);
        }

        public void WriteLength()
        {
            this.buffer.InsertRange(0, BitConverter.GetBytes(this.buffer.Count));
        }

        public byte[] ToArray()
        {
            return this.readableBuffer = this.buffer.ToArray();
        }

        public int Length()
        {
            return this.buffer.Count;
        }

        public int GetUnreadLength()
        {
            return this.buffer.Count - this.currentReadPosition;
        }

        public void Clear()
        {
            this.buffer.Clear();
            this.currentReadPosition = 0;
            this.readableBuffer = null;
        }

        public void Reset(bool _shouldReset)
        {
            if(_shouldReset)
                this.Clear();
            else
                this.currentReadPosition -= 4;
        }

        public void SetBytes(byte[] _data)
        {
            this.Write(_data);
        }

        public void Write(byte _data)
        {
            this.buffer.Add(_data);
        }

        public void Write(byte[] _data)
        {
            this.buffer.AddRange(_data);
        }

        public void Write(float _data)
        {
            this.buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(double _data)
        {
            this.buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(bool _data)
        {
            this.buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(short _data)
        {
            this.buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(int _data)
        {
            this.buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(long _data)
        {
            this.buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(string _data)
        {
            this.Write(_data.Length);
            this.buffer.AddRange(Encoding.UTF8.GetBytes(_data));
        }



        private byte[] Read(int _count, int _size, bool _shouldMove = true)
        {
            if(this.buffer.Count > this.currentReadPosition)
            {
                byte[] value = buffer.GetRange(this.currentReadPosition, _count * _size).ToArray();

                if(_shouldMove)
                    this.currentReadPosition += _count * _size;

                return value;
            }
            else 
                throw new Exception($"Could not read {_count} of size {_size}");
        }

        public byte ReadByte(bool _shouldMove = true)
        {
            byte[] _value = this.Read(1, 1, _shouldMove);
            return _value[0];
        }

        public byte[] ReadBytes(int _length, bool _shouldMove = true)
        {
            return this.Read(_length, 1, _shouldMove);
        }

        public float ReadFloat(bool _shouldMove = true)
        {
            byte[] _value = this.Read(1, 4, _shouldMove);

            return BitConverter.ToSingle(_value, 0);
        }

        public double ReadDouble(bool _shouldMove = true)
        {
            byte[] _value = this.Read(1, 8, _shouldMove);

            return BitConverter.ToDouble(_value, 0);
        }

        public bool ReadBool(bool _shouldMove = true)
        {
            byte[] _value = this.Read(1, 1, _shouldMove);

            return BitConverter.ToBoolean(_value, 0);
        }

        public short ReadShort(bool _shouldMove = true)
        {
            byte[] _value = this.Read(1, 2, _shouldMove);

            return BitConverter.ToInt16(_value, 0);
        }

        public int ReadInt(bool _shouldMove = true)
        {
            byte[] _value = this.Read(1, 4, _shouldMove);

            return BitConverter.ToInt32(_value, 0);
        }

        public long ReadLong(bool _shouldMove = true)
        {
            byte[] _value = this.Read(1, 8, _shouldMove);

            return BitConverter.ToInt64(_value, 0);
        }

        public string ReadString(bool _shouldMove = true)
        {
            int length = this.ReadInt(_shouldMove);

            byte[] _value = this.Read(length, 4 * length, _shouldMove);

            return Encoding.UTF8.GetString(_value, 0, length);
        }

        public void Dispose()
        {
            this.buffer = null;
            this.readableBuffer = null;
            this.currentReadPosition = 0;
        }
    }
}
