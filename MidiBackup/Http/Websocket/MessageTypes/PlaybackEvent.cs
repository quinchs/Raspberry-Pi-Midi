using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket.MessageTypes
{
    class PlaybackEvent : SocketMessage
    {
        [JsonProperty("isPlaying")]
        public bool IsPlaying { get; set; }

        [JsonProperty("isPaused")]
        public bool IsPaused { get; set; }

        [JsonProperty("trackDuration")]
        public long TrackDuration { get; set; }
        
        [JsonProperty("currentSeek")]
        public long CurrentSeek { get; set; }

        [JsonProperty("currentFile")]
        public string CurrentFile { get; set; }

        public PlaybackEvent(MidiPlayback playback)
        {
            this.IsPlaying = playback.IsPlaying;
            this.TrackDuration = (long)((TimeSpan)playback.Duration).TotalSeconds;
            this.CurrentFile = playback.CurrentFileName;
            this.IsPaused = playback.IsPaused;
        }


        public PlaybackEvent(MidiPlayback playback, long seek, long duration)
            : this(playback)
        {
            this.CurrentSeek = seek;
            this.TrackDuration = duration;
        }
        
        public override OpCode Code => OpCode.PlaybackEvent;


    }
}
