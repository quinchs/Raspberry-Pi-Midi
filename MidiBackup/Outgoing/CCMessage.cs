using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Outgoing
{
    public class CCMessage : BaseOutgoing
    {
        public byte Channel { get; set; }
        public CCType Type { get; set; }
        public byte Value { get; set; }

        public CCMessage(byte channel, CCType type, byte value)
        {
            this.Channel = channel;
            this.Type = type;
            this.Value = value;
        }

        public override byte[] Build()
        {
            return base.CompilePacket(this.Channel, StatusType.CC, (byte)this.Type, this.Value);
        }
    }
}
