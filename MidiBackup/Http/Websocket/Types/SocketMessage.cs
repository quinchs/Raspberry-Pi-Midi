using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket
{
    public abstract class SocketMessage
    {
        [JsonIgnore]
        public abstract OpCode Code { get; }

        [JsonProperty("id")]
        public string MessageId { get; set; }

        public IMessage BuildMessage()
        {
            return new Message(this.Code, this);
        }

    }
}
