using HttpMultipartParser;
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

        [Route("/midi/upload", "POST")]
        public async Task<RestResult> UploadMidiFile()
        {
            if (!Request.HasEntityBody)
                return RestResult.BadRequest;

            var formData = await MultipartFormDataParser.ParseAsync(Request.InputStream, Encoding.UTF8);

            var midiFileData = formData.Files.FirstOrDefault(x => x != null && x.Name == "midi");

            if (midiFileData == null)
                return RestResult.BadRequest;

            MidiFile midiFile = null;

            try
            {
                midiFile = MidiFile.Read(midiFileData.Data);
            }
            catch(Exception x)
            {
                Logger.Debug($"Invalid midi file {x}", Severity.Http, Severity.Warning);
                return RestResult.BadRequest;
            }

            FileManager.AddMidiFile(midiFile, midiFileData.FileName);
            return RestResult.OK;
        }
    }
}
