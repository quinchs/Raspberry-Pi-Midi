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
            return RestResult.OK.WithData(Driver.FileManager.Files);
        }

        [Route("/midi/download/{file}", "GET")]
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

        [Route("/midi/rename/{file}?new={newFile}", "PATCH")]
        public async Task<RestResult> RenameMidiFile(string file, string newFile) 
        {
            var result = FileManager.TryRenameFile(file, newFile, out var meta);

            return result ? RestResult.OK.WithData(meta) : RestResult.BadRequest;
        }
    }
}
