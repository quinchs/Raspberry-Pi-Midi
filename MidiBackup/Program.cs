using Commons.Music.Midi;
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
        public MidiRecorder Recorder;
        public Playback Playback;
        public static Config Config { get; private set; }
        static void Main(string[] args)
        {
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
            var confFile = Environment.CurrentDirectory + $"{Path.DirectorySeparatorChar}conf.json";

            if (!File.Exists(confFile)) File.Create(confFile).Close();

            try
            {
                var json = File.ReadAllText(confFile);
                Config = JsonConvert.DeserializeObject<Config>(json);
            }
            catch (Newtonsoft.Json.JsonException x)
            {
                Console.Error.WriteLine($"Failed to read config file: {x}");
            }
            catch (Exception x)
            {
                Console.Error.WriteLine($"Failed to load config: {x}");
            }

            Driver = new MidiDriver(Config);
            Playback = new Playback(Driver);
            await Driver.Start();

            if (args.Length == 2 && args[0] == "-p")
            {
                DoPlayback(args[1]);
                await Task.Delay(-1);
            }

            Recorder = new MidiRecorder(Driver);

            StopperTimer.Elapsed += HandleElapsed;

            Driver.OnMessage += Driver_OnMessage;

            async Task CheckRec(object arg)
            {
                if (!Recorder.IsRecording)
                {
                    StartRecording();

                    if(arg is MidiNoteEventArgs pArg)
                    {
                        await Recorder.NotePressed(pArg);
                    }
                    else if (arg is MidiSustainEventArg sus)
                    {
                        await Recorder.OnSustain(sus);
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
                Console.WriteLine("Device disconnected");
                if (Recorder.IsRecording)
                    SaveRecording();
            };

            await Task.Delay(-1);
        }

        private void DoPlayback(string file)
        {
            Playback.Start(file);
        }

        private void HandleElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Recorder.IsRecording)
                SaveRecording();
        }

        System.Timers.Timer StopperTimer = new System.Timers.Timer(60000); 
        public void StartRecording()
        {
            Console.WriteLine("Recording started");
            Recorder.Start();
        }

        public void SaveRecording()
        {
            if (!Directory.Exists(MidiDir))
                Directory.CreateDirectory(MidiDir);

            var fileName = $"{MidiDir}{Path.DirectorySeparatorChar}{ DateTime.UtcNow.ToString("O")}.midi";

            Recorder.Stop();
            try
            {
                Recorder.Save(fileName);
            }
            catch(Exception x)
            {
                Console.WriteLine(x);
            }

            Console.WriteLine($"Saved recording to {fileName}");
        }

        private Task Driver_OnMessage(MidiEventArgs arg)
        {
            if(arg.Message.Status != StatusType.MidiClock && arg.Message.Status != StatusType.ActiveSense)
                Console.WriteLine($"{arg.Message}");

            return Task.CompletedTask;
        }

        //public static string GetSelectedMidi(string midis) 
        //{
        //    Console.WriteLine("Multiple midi devices found, select a device: ");
        //}
    }
}
