using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Outgoing
{
    public abstract class BaseOutgoing : OutgoingMidiMessage
    {
        public byte[] CompilePacket(byte channel, StatusType Status, params byte[] data)
        {
            if(channel > 16)
            {
                throw new ArgumentOutOfRangeException("Channel must be from 0-16");
            }

            byte statusByte = (byte)Status;

            if (statusByte < 0xF0)
            {
                statusByte += channel;
            }

            byte[] buff = new byte[1 + data.Length];
            buff[0] = statusByte;
            Array.Copy(data, 0, buff, 1, data.Length);

            return buff;
        }

        public abstract byte[] Build();
    }
}
