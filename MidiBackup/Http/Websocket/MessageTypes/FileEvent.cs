using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket.MessageTypes
{
    public enum FileEventType
    {
        Created,
        Deleted,
        Updated
    }

    public class FileEvent : SocketMessage
    {
        [JsonProperty("eventType")]
        public FileEventType EventType { get; set; }

        [JsonProperty("meta")]
        public MidiFileMetadata Metadata { get; set; }

        [JsonProperty("files")]
        public IReadOnlyCollection<MidiFileMetadata> Files { get; }

        public FileEvent() { }

        public FileEvent(FileEventType type, MidiFileMetadata meta, IReadOnlyCollection<MidiFileMetadata> files)
        {
            this.EventType = type;
            this.Metadata = meta;
            this.Files = files;
        }

        public override OpCode Code => OpCode.FileEvent;
    }
}
