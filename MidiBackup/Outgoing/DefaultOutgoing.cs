using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Outgoing
{
    public class DefaultOutgoing : BaseOutgoing
    {
        public byte[] Packet { get; set; }

        public DefaultOutgoing(byte[] packet)
        {
            this.Packet = packet;
        }

        public override byte[] Build()
        {
            return Packet;
        }
    }
}
