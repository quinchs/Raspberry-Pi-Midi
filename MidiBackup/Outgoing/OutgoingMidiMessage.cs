using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Outgoing
{
    public interface OutgoingMidiMessage
    {
        public byte[] Build();
    }
}
