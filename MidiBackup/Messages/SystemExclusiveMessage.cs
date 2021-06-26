using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public enum Identification
    {
        GMSystemOn = 0x7E,
        MIDIMasterVolume = 0x7F,
        InstrumentDetails = 0x43,
    }

    public enum InstrumentDetailType
    {
        ReverbType = 0x00,
        ChorusType = 0x20,
    }

    public class SystemExclusiveMessage : MidiMessage
    {
        public Identification Call { get; }
        public InstrumentDetailType InstrumentDetail { get; }

        public byte IdentByte
            => Data[0];

        public byte SubStatus
            => Data[1];

        public byte Parameter
            => Data[2];

        public byte[] SysExValue
            => Data.Skip(3).TakeWhile(x => x != 0xF7).ToArray();

        public SystemExclusiveMessage(MidiPacket packet)
            : base (packet)
        {
            // 43 10 4C 08 01 0D 42 F7
            // 43 10 4C 08 01 0D 40 F7
            Call = (Identification)packet.Data[0];

            if(Call == Identification.InstrumentDetails)
            {
                switch (Data[2])
                {
                    case 0x4C:
                        // reverb or chorus
                        switch ((InstrumentDetailType)Data[5])
                        {
                            case InstrumentDetailType.ChorusType:
                                break;
                            case InstrumentDetailType.ReverbType:
                                break;
                        }
                        break;
                }
            }
        }

        public override string ToString()
        {
            return $"{this.Channel.ToString().PadRight(4)} -> {{{this.Size}}} |{this.StatusByte.ToString("X2")}:{this.MetaType.ToString("X2")}:{this.EventType.ToString("X2")}|  " +
                   $"{this.Status}:{(this.Meta.HasValue ? this.Meta.Value : "none")}:{(this.CC.HasValue ? this.CC : "none")} SysCall: {this.Call} {(this.Data != null ? "\nSD: " + BitConverter.ToString(this.Data).Replace("-", "") : "")}";

        }
    }
}
