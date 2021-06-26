using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Outgoing
{
    public class NoteOff : BaseOutgoing
    {
        public byte Channel { get; set; }
        public byte Note { get; set; }

        public NoteOff(byte channel, byte note)
        {
            this.Channel = channel;
            this.Note = note;
        }

        public override byte[] Build()
        {
            return base.CompilePacket(this.Channel, StatusType.NoteOff, Note, 0x00);
        }
    }
}
