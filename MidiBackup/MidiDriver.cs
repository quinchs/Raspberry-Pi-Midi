using MidiBackup.Outgoing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiBackup
{
    public partial class MidiDriver
    {
        public Reader Reader { get; private set; }
        public Writer Writer { get; private set; }

        private FileSystemWatcher Watcher { get; }

        internal FileStream MidiStream { get; private set; }

        public CancellationTokenSource ReadCancel { get; set; }

        public bool IsConnected
            => MidiStream != null;

        public Config Config;

        public MidiDriver(Config conf)
        {
            ReadCancel = new CancellationTokenSource();
            this.Config = conf;
            Watcher = new FileSystemWatcher(@"/dev/snd");

            Watcher.Created += Watcher_Created;
            Watcher.Deleted += Watcher_Deleted;

            Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;

            Watcher.Filter = "*midi*";

            Watcher.EnableRaisingEvents = true;
            Watcher.IncludeSubdirectories = true;
        }

        public async Task Start()
        {
            var files = Directory.GetFiles("/dev/snd");
            var MidiFile = files.FirstOrDefault(x => x.Contains("midi"));

            if (MidiFile != null)
            {
                Console.WriteLine($"Found connected midi device: {MidiFile}");

                OpenMidiStream(MidiFile);
                if (Reader == null)
                    Reader = new Reader(this);
                if (Writer == null)
                    Writer = new Writer(this);
                ResumeRead();

                //await Writer.WritePacket(new SetInstrument<Instrument>(0, Instrument.AcousticGrandPiano));
            }
            else
            {
                Console.WriteLine("Waiting for midi device...");
            }
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                Console.WriteLine($"D {e.FullPath}");
                if (e.Name.StartsWith("midi"))
                {
                    if (MidiStream != null)
                        HandleDeviceDisconnected();
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x);
            }
        }

        public void HandleDeviceDisconnected()
        {
            Console.WriteLine("Midi device disconnected, pausing read");
            if (MidiStream != null)
            {
                MidiStream.Dispose();
                MidiStream.Close();
                MidiStream = null;
            }
            PauseRead();

            DispatchEvent(DeviceDisconnected);
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                Console.WriteLine($"C {e.FullPath}");
                if (e.Name.StartsWith("midi"))
                {
                    Console.WriteLine("Midi stream was created, opening...");
                    Thread.Sleep(500);
                    OpenMidiStream(e.FullPath);
                    if (Reader == null)
                        Reader = new Reader(this);
                    if (Writer == null)
                        Writer = new Writer(this);

                    ResumeRead();
                    DispatchEvent(DeviceConnected);
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x);
            }
        }

        public void OpenMidiStream(string path)
        {
            MidiStream = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
        }

        public void PauseRead()
            => ReadCancel.Cancel();

        public void ResumeRead()
        {
            ReadCancel = new CancellationTokenSource();
            _ = Task.Run(async () => await Reader.ReaderAsync());
        }
    }
}
