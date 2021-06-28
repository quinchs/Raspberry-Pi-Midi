using MidiBackup.Http.RestService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Routes
{
    public class PlaybackRoutes : RestModuleBase
    {
        [Route("/midi/play/{file}", "GET")]
        public async Task<RestResult> PlayFile(string file)
        {
            if (!File.Exists($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}MidiFiles{Path.DirectorySeparatorChar}{file}"))
                return RestResult.NotFound;

            if (!Driver.IsConnected)
                return RestResult.Forbidden;

            Playback.Start($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}MidiFiles{Path.DirectorySeparatorChar}{file}");

            return RestResult.OK;
        }

        [Route("/midi/stop", "GET")]
        public async Task<RestResult> StopPlayback()
        {
            if (!Playback.IsPlaying)
                return RestResult.BadRequest;

            Playback.Stop();

            return RestResult.OK;
        }
    }
}
