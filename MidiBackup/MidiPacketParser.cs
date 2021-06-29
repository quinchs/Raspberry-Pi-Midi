using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class MidiPacketParser
    {
        public IEnumerable<MidiMessage> Parse(byte[] bytes, int index, int size)
        {
            int i = index;
            int end = index + size;
            while (i < end)
            {
                if (bytes[i] == 0xF0)
                {
                    yield return ConvertBuff(0xF0, 0, 0, bytes.Skip(i + 1).ToArray(), i, size - i - 1, bytes);
                    i += size;
                }
                else
                {
                    var z = FixedDataSize(bytes[i]);
                    if (end < i + z)
                        throw new Exception(string.Format(
                            "Received data was incomplete to build MIDI status message for '{0:X}' status.",
                            bytes[i]));
                    yield return ConvertBuff(bytes[i],
                        (byte)(z > 0 ? bytes[i + 1] : 0),
                        (byte)(z > 1 ? bytes[i + 2] : 0),
                        bytes.Skip(i).Take(z + 1).ToArray(), i, z + 1, bytes);
                    i += z + 1;
                }
            }
        }

        private MidiMessage ConvertBuff(byte type, byte arg1, byte arg2, byte[] data, int dataOffset, int dataLength, byte[] raw)
        {
            var value = type + (arg1 << 8) + (arg2 << 16);

            var packet = new MidiPacket()
            {
                Data = data,
                DataLength = dataLength,
                DataOffset = dataOffset,
                Raw = raw,
                Value = value
            };

            if (type < 0xF0)
            {
                // removes the channel bit, idk if theres a better way to do this, (0xB5 >> 4 << 4 == 0xB0)
                switch ((type >> 4) << 4)
                {
                    case (byte)StatusType.NoteOff or (byte)StatusType.NoteOn:
                        return new NoteMessage(packet);

                    case (byte)(StatusType.Program):
                        return new ProgramMessage(packet);

                    case (byte)StatusType.CC:
                        {
                            switch ((CCType)arg1)
                            {
                                case CCType.Hold:
                                    return new SustainMessage(packet);

                                default: return new DefaultMidiMessage(packet);
                            }
                        }
                    default: return new DefaultMidiMessage(packet);
                }
            }
            else
            {
                switch (type)
                {
                    case 0xF0:
                        return new SystemExclusiveMessage(packet);

                    default: return new DefaultMidiMessage(packet);
                }
            }

        }


        public byte FixedDataSize(byte statusByte)
        {
            switch ((StatusType)(statusByte & 0xF0))
            {
                case StatusType.SysEx1: // and 0xF7, 0xFF
                    switch ((StatusType)statusByte)
                    {
                        case StatusType.MtcQuarterFrame:
                        case StatusType.SongSelect:
                            return 1;
                        case StatusType.SongPositionPointer:
                            return 2;
                        default:
                            return 0; // no fixed data
                    }
                case StatusType.Program:
                case StatusType.CAf:
                    return 1;
                default:
                    return 2;
            }
        }
    }
}
