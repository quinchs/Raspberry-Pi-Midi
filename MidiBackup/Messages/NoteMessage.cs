using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class NoteMessage : MidiMessage
    {
        public int MidiNoteNumber { get; }
        public int Velocity { get; }
        public bool State { get; }

        public NoteMessage(MidiPacket packet)
            : base (packet)
        {
            this.MidiNoteNumber = Convert.ToInt32(Data[1]);
            this.Velocity = Convert.ToInt32(Data[2]);

            if (Status == StatusType.NoteOff || Velocity == 0)
                this.State = false;
            else this.State = true;
        }

        public override string ToString()
        {
            return $"F:{this.DataOffset} Note: {MidiNoteNumber} was {(this.State ? $"Pressed with a velocity of {this.Velocity}" : "Released")}";
        }
    }
}
