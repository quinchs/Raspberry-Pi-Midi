using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket
{
    public enum OpCode
    {
        StatusUpdate = 0,
        FileUpdate = 1,
        RemotePlayerCommand = 2,
        PlaybackEvent = 3,

        CommandResult = 69,
    }
}
