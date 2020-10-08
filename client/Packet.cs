using System;
using System.Collections.Generic;
using System.Text;

namespace client {

    class Packet : IDisposable {

        private List<byte> buffer;
        private byte[] readableBuffer;

        private int currentReadPosition;

        public int Length { get { return buffer.Count; } }
        public int UnreadLength { get { return buffer.Count - currentReadPosition; } }

        public int CurrentPosition { get { return currentReadPosition; } }
       
        public Packet()
        {
            buffer = new List<byte>();
            currentReadPosition = 0;
        }

        public Packet(int _id)
        {
            buffer = new List<byte>();
            currentReadPosition = 0;

            Write(_id);
        }

        public Packet(byte[] _data)
        {
            buffer = new List<byte>();
            currentReadPosition = 0;

            SetBytes(_data);
        }

        public void WriteLength()
        {
            buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));
        }

        public byte[] ToArray()
        {
            return readableBuffer = buffer.ToArray();
        }

        public void Clear()
        {
            buffer.Clear();
            currentReadPosition = 0;
            readableBuffer = null;
        }

        public void Reset(bool _shouldReset)
        {
            if(_shouldReset)
                Clear();
            else
                currentReadPosition -= 4;
        }

        public void SetBytes(byte[] _data)
        {
            Write(_data);
        }

        public void InsertInt(int _data)
        {
            buffer.InsertRange(0, BitConverter.GetBytes(_data));
        }

        public void Write(byte _data)
        {
            buffer.Add(_data);
        }

        public void Write(byte[] _data)
        {
            buffer.AddRange(_data);
        }

        public void Write(float _data)
        {
            buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(double _data)
        {
            buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(bool _data)
        {
            buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(short _data)
        {
            buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(int _data)
        {
            buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(long _data)
        {
            buffer.AddRange(BitConverter.GetBytes(_data));
        }

        public void Write(string _data)
        {
            // this.Write(_data.Length);
            Write(Encoding.UTF8.GetByteCount(_data));
            buffer.AddRange(Encoding.UTF8.GetBytes(_data));
        }

        public void Write<T>(IPacketable<T> _data) where T : IPacketable<T>, new()
        {
            buffer = PacketableHandler.Write(_data, this).buffer;
        }

        public void Write<T>(IPacketable<T>[] _data) where T : IPacketable<T>, new()
        {
            Write(_data.Length);

            for(int i = 0; i < _data.Length; i++)
                buffer = PacketableHandler.Write(_data[i], this).buffer;
        }


        private byte[] Read(int _count, int _size, bool _shouldMove = true)
        {
            if(buffer.Count > currentReadPosition)
            {
                byte[] value = buffer.GetRange(currentReadPosition, _count * _size).ToArray();

                if(_shouldMove)
                    currentReadPosition += _count * _size;

                return value;
            }
            else 
                throw new Exception($"Could not read {_count} of size {_size}");
        }

        public byte ReadByte(bool _shouldMove = true)
        {
            byte[] _value = Read(1, 1, _shouldMove);
            return _value[0];
        }

        public byte[] ReadBytes(int _length, bool _shouldMove = true)
        {
            return Read(_length, 1, _shouldMove);
        }

        public float ReadFloat(bool _shouldMove = true)
        {
            byte[] _value = Read(1, 4, _shouldMove);

            return BitConverter.ToSingle(_value, 0);
        }

        public double ReadDouble(bool _shouldMove = true)
        {
            byte[] _value = Read(1, 8, _shouldMove);

            return BitConverter.ToDouble(_value, 0);
        }

        public bool ReadBool(bool _shouldMove = true)
        {
            byte[] _value = Read(1, 1, _shouldMove);

            return BitConverter.ToBoolean(_value, 0);
        }

        public short ReadShort(bool _shouldMove = true)
        {
            byte[] _value = Read(1, 2, _shouldMove);

            return BitConverter.ToInt16(_value, 0);
        }

        public int ReadInt(bool _shouldMove = true)
        {
            byte[] _value = Read(1, 4, _shouldMove);

            return BitConverter.ToInt32(_value, 0);
        }

        public long ReadLong(bool _shouldMove = true)
        {
            byte[] _value = Read(1, 8, _shouldMove);

            return BitConverter.ToInt64(_value, 0);
        }

        public string ReadString(bool _shouldMove = true)
        {
            int length = ReadInt(_shouldMove);

            byte[] _value = Read(length, 1, _shouldMove);

            return Encoding.UTF8.GetString(_value, 0, length);
        }

        public T Read<T>() where T : IPacketable<T>, new()
        {
            ReadData<T> data = PacketableHandler.Read(new T(), this);
            
            currentReadPosition = data.bytesRead;

            return data.data; 
        }

        public T[] ReadArray<T>() where T : IPacketable<T>, new()
        {
            int size = ReadInt();
            
            T[] arr = new T[size];

            for (int i = 0; i < size; i++)
            {
                ReadData<T> data = PacketableHandler.Read(new T(), this);
                currentReadPosition = data.bytesRead;

                arr[i] = data.data;
            }

            return arr; 
        }

        public void Dispose()
        {
            buffer = null;
            readableBuffer = null;
            currentReadPosition = 0;
        }
    }
}
