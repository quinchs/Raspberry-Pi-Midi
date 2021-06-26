using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public struct MidiPacket
    {
        public int Value;
        public byte[] Data;
        public int DataOffset;
        public int DataLength;
        public byte[] Raw;
    }
}
