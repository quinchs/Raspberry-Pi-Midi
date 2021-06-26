using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class SystemExclusiveMessage : MidiMessage
    {
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
            
        }

        public override string ToString()
        {
            return $"{this.Channel.ToString().PadRight(4)} -> {{{this.Size}}} |{this.StatusByte.ToString("X2")}:{this.MetaType.ToString("X2")}:{this.EventType.ToString("X2")}|  " +
                   $"{this.Status}:{(this.Meta.HasValue ? this.Meta.Value : "none")}:{(this.CC.HasValue ? this.CC : "none")} SysCall: {this.IdentByte} {(this.Data != null ? "\nSD: " + BitConverter.ToString(this.Data).Replace("-", "") : "")}";

        }
    }
}
