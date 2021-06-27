using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket
{
    public interface IMessage
    {
        OpCode Code { get; }
        object Payload { get; }
        string MessageId { get; }
    }
}
