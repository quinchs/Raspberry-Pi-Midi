using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Outgoing
{
    public class SetInstrument<TInstrument> : BaseOutgoing where TInstrument : Enum
    {
        public byte Channel { get; set; }
        public TInstrument Instrument { get; }

        public SetInstrument(byte channel, TInstrument instrument)
        {
            this.Channel = channel;
            this.Instrument = instrument;
        }

        public override byte[] Build()
            => base.CompilePacket(this.Channel, StatusType.Program, Convert.ToByte(this.Instrument));
    }
}
