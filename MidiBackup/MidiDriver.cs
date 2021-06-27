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
        public MidiRecorder Recorder { get; private set; }
        public Playback Playback { get; private set; }
        public Reader Reader { get; private set; }
        public Writer Writer { get; private set; }

        public string DeviceName { get; private set; }

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

            Playback = new Playback(this);
            Recorder = new MidiRecorder(this);
        }

        public async Task Start()
        {
            var files = Directory.GetFiles("/dev/snd");
            var MidiFile = files.FirstOrDefault(x => x.Contains("midi"));

            if (MidiFile != null)
            {
                Logger.Write($"Found connected midi device: {MidiFile}", Severity.Driver, Severity.Log);

                OpenMidiStream(MidiFile);
                if (Reader == null)
                    Reader = new Reader(this);
                if (Writer == null)
                    Writer = new Writer(this);
                DeviceName = $"{MidiFile.Split("/").Last()}";
                ResumeRead();

                DispatchEvent(DeviceConnected);
            }
            else
            {
                Logger.Write("Waiting for midi device...", Severity.Driver, Severity.Log);
            }
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (e.Name.StartsWith("midi"))
                {
                    if (MidiStream != null)
                        HandleDeviceDisconnected();
                }
            }
            catch (Exception x)
            {
                Logger.Write(x, Severity.Driver, Severity.Log);
            }
        }

        public void HandleDeviceDisconnected()
        {
            if (MidiStream != null)
            {
                Logger.Write("Midi device disconnected, pausing read", Severity.Driver, Severity.Log);
                MidiStream.Dispose();
                MidiStream.Close();
                MidiStream = null;
                DeviceName = null;

                PauseRead();

                DispatchEvent(DeviceDisconnected);
            }

        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (e.Name.StartsWith("midi"))
                {
                    Logger.Write("Midi stream was created, opening...", Severity.Driver, Severity.Log);
                    Thread.Sleep(500);
                    OpenMidiStream(e.FullPath);
                    if (Reader == null)
                        Reader = new Reader(this);
                    if (Writer == null)
                        Writer = new Writer(this);

                    DeviceName = $"{e.Name}";
                    ResumeRead();
                    DispatchEvent(DeviceConnected);
                }
            }
            catch (Exception x)
            {
                Logger.Write(x, Severity.Driver, Severity.Error);
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
