using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket.MessageTypes
{
    public class PlayerCommandResult : WebsocketMessageResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        public PlayerCommandResult(SocketMessage message, bool success, string reason = null)
            : base(message)
        {
            this.Success = success;
            this.Reason = reason;
        }

        public static PlayerCommandResult FromResult(SocketMessage message, bool success, string reason = null)
            => new PlayerCommandResult(message, success, reason);
    }
}
