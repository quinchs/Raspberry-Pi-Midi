using MidiBackup.Http.RestService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.Routes
{
    public class MidiRoutes : RestModuleBase
    {
        private string MidiDir { get; } = $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}MidiFiles";

        [Route("/midi", "GET")]
        public async Task<RestResult> ListMidiFiles()
        {
            var files = Directory.GetFiles(MidiDir);

            return RestResult.OK.WithData(files.Select(x => 
            {
                var spl = x.Split("_");
                return new
                {
                    duration = spl[1].Replace(".midi", ""),
                    name = x.Split("/").Last()
                };
            }));
        }

        [Route("/midi/get/{file}", "GET")]
        public async Task<RestResult> GetMidiFile(string file)
        {
            var path = MidiDir + $"{Path.DirectorySeparatorChar}{file}";
            if (!File.Exists(path))
                return RestResult.NotFound;

            var bytes = File.ReadAllBytes(path);

            await Response.OutputStream.WriteAsync(bytes);
            Response.Headers.Add("Content-Type", "audio/midi");

            return RestResult.OK;
        }
    }
}
