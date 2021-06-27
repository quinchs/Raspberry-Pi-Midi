using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket
{
    public class Message : IMessage
    {
        [JsonProperty("code")]
        public OpCode Code { get; set; }

        [JsonProperty("payload")]
        public object Payload { get; set; }

        [JsonProperty("id")]
        public string MessageId { get; set; }

        public Message(OpCode code, object paylaod, string messageId = null)
        {
            this.Code = code;
            this.Payload = paylaod;
            this.MessageId = messageId;
        }

        public static Message Create(OpCode code, object payload, string messageId = null)
            => new Message(code, payload, messageId);

        public T PayloadAs<T>()
            => (this.Payload as JToken).ToObject<T>();
    }
}
