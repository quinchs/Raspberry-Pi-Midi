using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using MidiBackup.Outgoing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MidiBackup
{
    public class Playback
    {
        public event Func<string, Task> PlaybackStarted;
        public event Func<Task> PlaybackStopped;
        public event Func<long, long, Task> MidiTimeUpdated;

        public MidiFile CurrentFile { get; private set; }
        public string CurrentFileName { get; private set; }

        private TempoMap TempoMap;
        private List<TimedEvent> Events { get; set; } = new List<TimedEvent>();
        public bool IsPlaying { get; private set; }
        private Stopwatch stopwatch { get; set; } = new Stopwatch();
        private TimeSpan StartOffset { get; set; } = TimeSpan.Zero;

        private MidiDriver Driver { get; }

        public TimeSpan Duration { get; private set; }

        private MidiClock Clock { get; set; }

        private MetricTimeSpan LastRead { get; set; } = TimeSpan.Zero;

        private Timer PlaybackEventTimer { get;}
        public bool IsPaused { get; private set; } = false;

        public Playback(MidiDriver driver)
        {
            this.Driver = driver;
            this.Driver.OnMidiClock += Driver_OnMidiClock;
            PlaybackEventTimer = new Timer(driver.Config.PlaybackDispatchTime);
            PlaybackEventTimer.Elapsed += PlaybackEventTimer_Elapsed;
            PlaybackEventTimer.Start();
        }

        private long lastSecond = 0;

        private void PlaybackEventTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsPlaying)
                return;

            var currentSecond = (long)(stopwatch.Elapsed + StartOffset).TotalSeconds;

            if (lastSecond == currentSecond)
                return;

            lastSecond = currentSecond;

            Driver.DispatchEvent(MidiTimeUpdated, currentSecond, (long)Duration.TotalSeconds);
        }

        private async Task Driver_OnMidiClock()
        {
            if (!IsPlaying)
                return;

            try
            {
                var tickTime = StartOffset + stopwatch.Elapsed;
                if (tickTime.Ticks > Duration.Ticks)
                    Stop();

                var ts = (MetricTimeSpan)tickTime;
                var newEvents = Events.Where(x =>
                {
                    var pTS = x.TimeAs<MetricTimeSpan>(TempoMap);
                    return pTS.TotalMicroseconds <= ts.TotalMicroseconds && pTS.TotalMicroseconds > LastRead.TotalMicroseconds;
                });

                

                var buff = BuildPacket(newEvents.Select(x => GetMessage(x)).ToArray());

                if (Driver.Config.Debug)
                {
                    Logger.Write($"{LastRead} - {ts} : {newEvents.Count()}", Severity.MIDI, Severity.Log);
                }

                LastRead = ts;

                if (buff.Length > 0)
                {
                    await Driver.Writer.WritePacket(buff);
                    Logger.Write($"Sending 0x{string.Join("", buff.Select(x => x.ToString("X2")))}", Severity.MIDI);
                }
            }
            catch(Exception x)
            {
                Logger.Write(x, Severity.MIDI, Severity.Error);
            }
        }

        public bool Seek(long miliseconds)
        {
            var offset = TimeSpan.FromMilliseconds(miliseconds);

            if (offset.TotalMilliseconds >= Duration.TotalMilliseconds)
                return false;

            StartOffset = offset;
            stopwatch.Restart();
            return true;
        }

        public void Start(string file)
        {
            try
            {
                this.CurrentFile = MidiFile.Read(file);
                this.CurrentFileName = file;

                this.TempoMap = CurrentFile.GetTempoMap() ?? TempoMap.Default;

                Logger.Write($"Midi file opened: {CurrentFile} - {TempoMap}", Severity.MIDI, Severity.Driver);

                Events.Clear();
                Events = CurrentFile.GetTimedEvents().ToList();

                LastRead = TimeSpan.Zero;

                Clock = new MidiClock(20, Driver);

                Duration = CurrentFile
                    .GetTimedEvents()
                    .LastOrDefault(e => e.Event is NoteOffEvent)
                    ?.TimeAs<MetricTimeSpan>(TempoMap) ?? new MetricTimeSpan();

                Logger.Write("Track length: " + Duration.TotalSeconds + "s", Severity.MIDI, Severity.Driver);

                Clock.Start();
                IsPlaying = true;
                StartOffset = TimeSpan.Zero;
                stopwatch.Reset();
                stopwatch.Start();
                lastSecond = 0;
                IsPaused = true;
                Logger.Write("Track started", Severity.MIDI, Severity.Driver);

                Driver.DispatchEvent(PlaybackStarted, file);
            }
            catch(Exception x)
            {
                Logger.Write(x, Severity.Error);
                Stop();
            }
        }

        private byte[] BuildPacket(params OutgoingMidiMessage[] msg)
        {
            List<byte> buff = new List<byte>();

            foreach (var item in msg)
                buff.AddRange(item.Build());

            return buff.ToArray();
        }

        private OutgoingMidiMessage GetMessage(TimedEvent e)
        {
            if (!IsPlaying)
                return null;

            try
            {
                switch (e.Event.EventType)
                {
                    case MidiEventType.NoteOn:
                        {
                            var message = (NoteOnEvent)e.Event;
                            return new NoteOn(message.Channel, message.NoteNumber, message.Velocity);
                        }
                    case MidiEventType.NoteOff:
                        {
                            var message = (NoteOffEvent)e.Event;
                            return new NoteOff(message.Channel, message.NoteNumber);
                        }
                    case MidiEventType.ControlChange:
                        {
                            var controlChange = (ControlChangeEvent)e.Event;
                            return new CCMessage(controlChange.Channel, (CCType)(byte)controlChange.ControlNumber, controlChange.ControlValue);
                        }
                }
            }
            catch(Exception x)
            {
                Logger.Write(Logger.BuildColoredString($"{x}", ConsoleColor.Red), Severity.Error);
            }

            return null;
        }

        public void Stop()
        {
            if (IsPlaying)
            {
                Logger.Write("Track ended", Severity.MIDI, Severity.Driver);
                IsPlaying = false;
                this.CurrentFileName = null;
                Clock.Stop();
                CurrentFile = null;
                stopwatch.Stop();
                lastSecond = 0;
                Duration = TimeSpan.Zero;
                LastRead = TimeSpan.Zero;
                IsPaused = false;

                Driver.DispatchEvent(PlaybackStopped);
            }
        }

        public void Pause()
        {
            stopwatch.Stop();
            IsPaused = true;
        }

        public void Resume()
        {
            stopwatch.Start();
            IsPaused = false;
        }
    }
}
