using MidiBackup.Http.Websocket.MessageTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket
{
    public abstract class WebsocketMessageResult
    {
        [JsonIgnore]
        public string MessageId { get; }
        
        [JsonIgnore]
        public SocketMessage SocketMessage { get; }

        [JsonProperty("exception")]
        public string Exception { get; }

        public WebsocketMessageResult(SocketMessage message)
        {
            this.SocketMessage = message;
            this.MessageId = message.MessageId;
        }

        public WebsocketMessageResult(Exception x)
        {
            this.Exception = x.ToString();
        }

        public IMessage BuildMessage()
        {
            return Message.Create(OpCode.CommandResult, this, MessageId);
        }

    }
}
