using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Websocket.MessageTypes
{
    public class StatusUpdate : SocketMessage
    {
        [JsonProperty("deviceRecording")]
        public bool DeviceRecording { get; set; }

        [JsonProperty("isConnected")]
        public bool IsConnected { get; set; }

        [JsonProperty("isPlayingBack")]
        public bool IsPlayingBack { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        public StatusUpdate(MidiDriver Driver)
        {
            this.DeviceName = Driver.DeviceName;
            this.IsConnected = Driver.IsConnected;
            this.IsPlayingBack = Driver.Playback.IsPlaying;
            this.DeviceRecording = Driver.Recorder.IsRecording;
        }

        public override OpCode Code => OpCode.StatusUpdate;
    }
}
