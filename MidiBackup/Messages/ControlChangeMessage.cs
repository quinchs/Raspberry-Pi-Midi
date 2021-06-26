using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Messages
{
    public class ControlChangeMessage : MidiMessage
    {

        public ControlChangeMessage(MidiPacket pack)
            : base(pack)
        {

        }
    }
}
