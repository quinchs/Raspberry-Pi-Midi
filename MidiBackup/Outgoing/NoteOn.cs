using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Outgoing
{
    public class NoteOn : BaseOutgoing
    {
        public byte Velocity { get; set; }
        public byte MidiNote { get; set; }
        public byte Channel { get; set; }

        public NoteOn(byte channel, byte note, byte velocity)
        {
            this.Channel = channel;
            this.MidiNote = note;
            this.Velocity = velocity;
        }

        public override byte[] Build()
        {
            return base.CompilePacket(this.Channel, StatusType.NoteOn, this.MidiNote, this.Velocity);
        }
    }
}
