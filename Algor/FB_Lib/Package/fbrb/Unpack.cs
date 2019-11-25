using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using EndianIO;

namespace FB_Lib.Package.fbrb
{
    class Unpack
    {
        struct CLog
        {
            public long offset;
            public long size;
            public long offsetoffset; // Offset Within the Offset
        }
        struct fbRbFile
        {
            public uint Header;
            public uint Size;
        }
        EndianReader br;
        EndianWriter bw;
        fbRbFile fbrbFile;
        const uint FB_HEADER = 0x46625242; // FbRB
        List<CLog> memoryFiles = new List<CLog>();

        public Unpack(string InputFileName, string OutDirectory)
        {
            if (File.Exists(InputFileName))
            {
                br = new EndianReader(new FileStream(InputFileName, FileMode.Open, FileAccess.Read), EndianType.BigEndian);
                fbrbFile = new fbRbFile();
                fbrbFile.Header = br.ReadUInt32();
                fbrbFile.Size = br.ReadUInt32();

                if (fbrbFile.Header == FB_HEADER)
                {
                    long OFFSET = br.BaseStream.Position;
                    long MEM_FILE = 1;
                    gzip_fbrb_sux(OFFSET, MEM_FILE);
                }
            }
        }

        void gzip_fbrb_sux(long OFFSET, long MEM_FILE)
        {
            long TMP_OFF = br.BaseStream.Position;
            ushort GZIP_SIGN = br.ReadUInt16(EndianType.LittleEndian);
            long TMP;
            uint XSIZE;
            if (GZIP_SIGN == 0x1F8B)
            {
                byte CM = br.ReadByte();
                byte FLAGS = br.ReadByte();
                uint MTIME = br.ReadUInt32(EndianType.LittleEndian);
                byte XFL = br.ReadByte();
                byte OS = br.ReadByte();
                string mFileName;
                if ((FLAGS & 4) != 0)
                {
                    ushort strLen = br.ReadUInt16(EndianType.LittleEndian);
                    mFileName = new string(br.ReadChars(strLen));
                }
                if ((FLAGS & 8) != 0 || (FLAGS & 16) != 0)
                    mFileName = br.ReadNullTerminatedString();
                if ((FLAGS & 2) != 0)
                {
                    ushort tmp = br.ReadUInt16(EndianType.LittleEndian);
                }
                TMP = br.BaseStream.Position;
                fbrbFile.Size += (uint)OFFSET;
                fbrbFile.Size -= 4; // Uncompressed Size
                br.BaseStream.Position = fbrbFile.Size;

                XSIZE = br.ReadUInt32();
                fbrbFile.Size -= (uint)TMP;
                if (XSIZE < fbrbFile.Size)
                {
                    XSIZE = fbrbFile.Size;
                    XSIZE = XSIZE *= 12;
                }
                if (MEM_FILE == 1)
                {
                    CLog MEMORY_FILE = new CLog();
                    MEMORY_FILE.offset = TMP;
                    MEMORY_FILE.size = fbrbFile.Size;
                    MEMORY_FILE.offsetoffset = XSIZE;
                    memoryFiles.Add(MEMORY_FILE);
                }
                else
                {
                    CLog MEMORY_FILE2 = new CLog();
                    MEMORY_FILE2.offset = TMP;
                    MEMORY_FILE2.size = fbrbFile.Size;
                    MEMORY_FILE2.offsetoffset = XSIZE;
                    memoryFiles.Add(MEMORY_FILE2);
                }
            }
            else
            {
                if (MEM_FILE == 1)
                {
                    CLog MEMORY_FILE = new CLog();
                    MEMORY_FILE.offset = OFFSET;
                    MEMORY_FILE.size = fbrbFile.Size;
                    MEMORY_FILE.offsetoffset = 0;
                    memoryFiles.Add(MEMORY_FILE);
                }
                else
                {
                    CLog MEMORY_FILE2 = new CLog();
                    MEMORY_FILE2.offset = OFFSET;
                    MEMORY_FILE2.size = fbrbFile.Size;
                    MEMORY_FILE2.offsetoffset = 0;
                    memoryFiles.Add(MEMORY_FILE2);
                }
            }
        }

    }
}
