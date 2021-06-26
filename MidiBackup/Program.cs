using Commons.Music.Midi;
using MidiBackup.Http;
using MidiBackup.Outgoing;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiBackup
{
    class Program
    {
        public string MidiDir = $"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}MidiFiles";
        public MidiDriver Driver;

        public HttpServer Server;

        public static Config Config { get; private set; }
        static void Main(string[] args)
        {
            Logger.Create();
            new Program().Start(args).GetAwaiter().GetResult();
        }

        public static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }
        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public async Task Start(string[] args)
        {
            try
            {
                var confFile = Environment.CurrentDirectory + $"{Path.DirectorySeparatorChar}conf.json";

                if (!File.Exists(confFile)) File.Create(confFile).Close();

                try
                {
                    var json = File.ReadAllText(confFile);
                    Config = JsonConvert.DeserializeObject<Config>(json);
                }
                catch (Newtonsoft.Json.JsonException x)
                {
                    Logger.Write($"Failed to read config file: {x}", Severity.Error);
                }
                catch (Exception x)
                {
                    Logger.Write($"Failed to load config: {x}", Severity.Error);
                }

                
                Driver = new MidiDriver(Config);
                Server = new HttpServer(Config.Port, Driver);
                await Driver.Start();

                if (args.Length == 2 && args[0] == "-p")
                {
                    DoPlayback(args[1]);
                    await Task.Delay(-1);
                }

                StopperTimer.Elapsed += HandleElapsed;

                Driver.OnMessage += Driver_OnMessage;

                async Task CheckRec(object arg)
                {
                    if (!Driver.Recorder.IsRecording)
                    {
                        StartRecording();

                        if (arg is MidiNoteEventArgs pArg)
                        {
                            await Driver.Recorder.NotePressed(pArg);
                        }
                        else if (arg is MidiSustainEventArg sus)
                        {
                            await Driver.Recorder.OnSustain(sus);
                        }
                    }
                    else
                    {
                        StopperTimer.Stop();
                        StopperTimer.Interval = 60000;
                        StopperTimer.Start();
                    }
                }

                Driver.OnNotePressed += CheckRec;
                Driver.OnSustain += CheckRec;

                Driver.DeviceDisconnected += async () =>
                {
                    Logger.Write("Device disconnected", Severity.Driver, Severity.Log);
                    if (Driver.Recorder.IsRecording)
                        SaveRecording();
                };

                await Task.Delay(-1);
            }
            catch (Exception x)
            {
                Logger.Write(x, Severity.Critical);
            }
        }

        private void DoPlayback(string file)
        {
            Driver.Playback.Start(file);
        }

        private void HandleElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Driver.Recorder.IsRecording)
                SaveRecording();
        }

        System.Timers.Timer StopperTimer = new System.Timers.Timer(60000); 
        public void StartRecording()
        {
            Logger.Write("Recording started", Severity.MIDI, Severity.Log);
            Driver.Recorder.Start();
        }

        public void SaveRecording()
        {
            if (!Directory.Exists(MidiDir))
                Directory.CreateDirectory(MidiDir);

            var fileName = $"{MidiDir}{Path.DirectorySeparatorChar}{ DateTime.UtcNow.ToString("O")}_{Driver.Recorder.Duration.TotalSeconds}.midi";

            Driver.Recorder.Stop();
            try
            {
                Driver.Recorder.Save(fileName);
            }
            catch(Exception x)
            {
                Logger.Write(x, Severity.MIDI, Severity.Error);
            }

            Logger.Write($"Saved recording to {fileName}", Severity.MIDI, Severity.Log);
        }

        private Task Driver_OnMessage(MidiEventArgs arg)
        {
            if(arg.Message.Status != StatusType.MidiClock && arg.Message.Status != StatusType.ActiveSense)
                Logger.Write($"{arg.Message}", Severity.Driver, Severity.Log);

            return Task.CompletedTask;
        }

        //public static string GetSelectedMidi(string midis) 
        //{
        //    Console.WriteLine("Multiple midi devices found, select a device: ");
        //}
    }
}
