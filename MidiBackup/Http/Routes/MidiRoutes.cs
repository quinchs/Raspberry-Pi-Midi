using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
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

            Dictionary<string, double> midiFiles = new Dictionary<string, double>();

            foreach(var file in files)
            {
                try
                {
                    var midiFile = MidiFile.Read(file);
                    if (midiFile != null)
                    {
                        var duration = midiFile.GetTimedEvents()
                            .LastOrDefault(e => e.Event is NoteOffEvent)
                            ?.TimeAs<MetricTimeSpan>(midiFile.GetTempoMap() ?? TempoMap.Default) ?? new MetricTimeSpan();

                        midiFiles.Add(file.Replace($"{MidiDir}/", ""), ((TimeSpan)duration).TotalSeconds);
                    }
                }
                catch(Exception x)
                {
                    Logger.Write($"Invalid midi file at {file} - {x}", Severity.Http, Severity.Warning);
                }
            }

            return RestResult.OK.WithData(midiFiles.Select(x => 
            {
                return new
                {
                    duration = x.Value,
                    name = x.Key
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
