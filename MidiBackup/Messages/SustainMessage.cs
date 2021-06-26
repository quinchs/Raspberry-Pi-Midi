using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class SustainMessage : MidiMessage
    {
        public int SustainValue { get; }

        public SustainMessage(MidiPacket packet)
            : base(packet)
        {
            SustainValue = Convert.ToInt32(this.Data[2]);
        }

        public override string ToString()
        {
            return $"Sustain {(SustainValue > 0 ? "on" : "off")}";
        }
    }
}
