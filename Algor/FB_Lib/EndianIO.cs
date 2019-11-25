using System;
using System.Collections.Generic;
using System.IO;

namespace EndianIO
{
    public enum EndianType
    {
        BigEndian,
        LittleEndian
    }

    public class EndianIO
    {
        private bool isfile = false;
        private bool isOpen = false;
        private Stream s;
        private string filepath;
        private EndianType endiantype;

        public EndianReader In;
        public EndianWriter Out;
        public EndianIO(string filelocation, EndianType endianstyle)
        {

            this.endiantype = endianstyle;
            this.filepath = filelocation;
            this.isfile = true;
        }
        public EndianIO(MemoryStream memorystream, EndianType endianstyle)
        {
            this.endiantype = endianstyle;
            this.s = memorystream;
        }
        public EndianIO(Stream stream, EndianType endianstyle)
        {
            this.endiantype = endianstyle;
            this.s = stream;
        }
        public void Open()
        {
            if (this.isOpen == true) return;
            if (this.isfile) this.s = new FileStream(this.filepath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            switch (this.endiantype)
            {
                case EndianType.BigEndian:
                    this.In = new EndianReader(s, this.endiantype);
                    this.Out = new EndianWriter(s, this.endiantype);
                    break;
                case EndianType.LittleEndian:
                    this.In = new EndianReader(s, this.endiantype);
                    this.Out = new EndianWriter(s, this.endiantype);
                    break;
            }
            this.isOpen = true;
        }
        public void Close()
        {
            if (this.isOpen == false) return;
            if (!this.isfile) return;
            In.Close();
            Out.Close();
            this.isOpen = false;
        }
    }
    public class EndianReader : BinaryReader
    {
        EndianType endianstyle;

        public EndianReader(Stream stream, EndianType endianstyle)
            : base(stream)
        {
            this.endianstyle = endianstyle;
        }

        public void SeekTo(int offset)
        {
            this.BaseStream.Position = offset;
        }
        public void SeekTo(uint offset)
        {
            this.BaseStream.Position = offset;
        }

        public override short ReadInt16()
        {
            return ReadInt16(endianstyle);
        }
        public short ReadInt16(EndianType EndianType)
        {
            byte[] buffer = base.ReadBytes(2);

            if (EndianType == EndianType.BigEndian)
                Array.Reverse(buffer);

            return BitConverter.ToInt16(buffer, 0);
        }

        public override ushort ReadUInt16()
        {
            return ReadUInt16(endianstyle);
        }
        public ushort ReadUInt16(EndianType EndianType)
        {
            byte[] buffer = base.ReadBytes(2);

            if (EndianType == EndianType.BigEndian)
                Array.Reverse(buffer);

            return BitConverter.ToUInt16(buffer, 0);
        }

        public override int ReadInt32()
        {
            return ReadInt32(endianstyle);
        }
        public int ReadInt32(EndianType EndianType)
        {
            byte[] buffer = base.ReadBytes(4);

            if (EndianType == EndianType.BigEndian)
                Array.Reverse(buffer);

            return BitConverter.ToInt32(buffer, 0);
        }

        public override uint ReadUInt32()
        {
            return ReadUInt32(endianstyle);
        }
        public uint ReadUInt32(EndianType EndianType)
        {
            byte[] buffer = base.ReadBytes(4);

            if (EndianType == EndianType.BigEndian)
                Array.Reverse(buffer);

            return BitConverter.ToUInt32(buffer, 0);
        }

        public override long ReadInt64()
        {
            return ReadInt64(endianstyle);
        }
        public long ReadInt64(EndianType EndianType)
        {
            byte[] buffer = base.ReadBytes(8);

            if (EndianType == EndianType.BigEndian)
                Array.Reverse(buffer);

            return BitConverter.ToInt64(buffer, 0);
        }

        public override ulong ReadUInt64()
        {
            return ReadUInt64(endianstyle);
        }

        public ulong ReadUInt64(EndianType EndianType)
        {
            byte[] buffer = base.ReadBytes(8);

            if (EndianType == EndianType.BigEndian)
                Array.Reverse(buffer);

            return BitConverter.ToUInt64(buffer, 0);
        }

        public override float ReadSingle()
        {
            return ReadSingle(endianstyle);
        }
        public float ReadSingle(EndianType EndianType)
        {
            byte[] buffer = base.ReadBytes(4);

            if (EndianType == EndianType.BigEndian)
                Array.Reverse(buffer);

            return BitConverter.ToSingle(buffer, 0);
        }

        public override double ReadDouble()
        {
            return ReadDouble(endianstyle);
        }

        public double ReadDouble(EndianType EndianType)
        {
            byte[] buffer = base.ReadBytes(4);

            if (EndianType == EndianType.BigEndian)
                Array.Reverse(buffer);

            return BitConverter.ToDouble(buffer, 0);
        }
        /// <summary>
        /// Reads a null-terminated string. Usefull for reading from tables...
        /// </summary>
        /// <returns></returns>
        public string ReadNullTerminatedString()
        {
            char tempChar;
            string returnString = "";
            while ((tempChar = ReadChar()) != '\0')
            {
                if (tempChar != '\0')
                    returnString += tempChar;
            }
            return returnString;
        }

        public string ReadUnicodeString(int length)
        {
            string returnString = "";
            while (length-- > 0)
            {
                ushort ch = ReadUInt16();
                returnString += (char)ch;
            }
            return returnString.Replace("\0", "");
        }

    }

    public class EndianWriter : BinaryWriter
    {
        EndianType endianstyle;
        public EndianWriter(Stream stream, EndianType endianstyle)
            : base(stream)
        {
            this.endianstyle = endianstyle;
        }

        public override void Write(float value)
        {
            float temp = value;
            if (endianstyle == EndianType.BigEndian)
            {
                byte[] b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                temp = BitConverter.ToSingle(b, 0);
            }
            base.Write(temp);
        }
        public override void Write(double value)
        {
            double temp = value;
            if (endianstyle == EndianType.BigEndian)
            {
                byte[] b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                temp = BitConverter.ToDouble(b, 0);
            }
            base.Write(temp);
        }
        public override void Write(int value)
        {
            int temp = value;
            if (endianstyle == EndianType.BigEndian)
            {
                byte[] b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                temp = BitConverter.ToInt32(b, 0);
            }
            base.Write(temp);
        }
        public override void Write(uint value)
        {
            uint temp = value;
            if (endianstyle == EndianType.BigEndian)
            {
                byte[] b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                temp = BitConverter.ToUInt32(b, 0);
            }
            base.Write(temp);
        }

        public override void Write(short value)
        {
            short temp = value;
            if (endianstyle == EndianType.BigEndian)
            {
                byte[] b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                temp = BitConverter.ToInt16(b, 0);
            }
            base.Write(temp);
        }
        public override void Write(ushort value)
        {
            ushort temp = value;
            if (endianstyle == EndianType.BigEndian)
            {
                byte[] b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                temp = BitConverter.ToUInt16(b, 0);
            }
            base.Write(temp);
        }
        public override void Write(long value)
        {
            long temp = value;
            if (endianstyle == EndianType.BigEndian)
            {
                byte[] b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                temp = BitConverter.ToInt64(b, 0);
            }
            base.Write(temp);
        }
        public override void Write(ulong value)
        {
            ulong temp = value;
            if (endianstyle == EndianType.BigEndian)
            {
                byte[] b = BitConverter.GetBytes(value);
                Array.Reverse(b);
                temp = BitConverter.ToUInt64(b, 0);
            }
            base.Write(temp);
        }

        public void SeekTo(int offset)
        {
            this.BaseStream.Position = offset;
        }

        public void SeekTo(uint offset)
        {
            this.BaseStream.Position = offset;
        }
    }
}
