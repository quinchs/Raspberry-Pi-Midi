using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket.MessageTypes
{
    public enum Command
    {
        Play = 0,
        Stop = 1,
        Seek = 2,
        Pause = 3,
        Resume = 4,
    }
    public class RemotePlayerCommand : SocketMessage
    {
        [JsonProperty("command")]
        public Command Command { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        public override OpCode Code => OpCode.RemotePlayerCommand;

        public static implicit operator RemotePlayerCommand(Message m) => m.PayloadAs<RemotePlayerCommand>();
    }
}
