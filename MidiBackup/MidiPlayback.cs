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
    public class MidiPlayback
    {
        public event Func<string, Task> PlaybackStarted;
        public event Func<Task> PlaybackStopped;
        public event Func<long, long, Task> MidiTimeUpdated;

        public MidiFile CurrentFile { get; private set; }
        public string CurrentFileName { get; private set; }
        public MetricTimeSpan Duration { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; } = false;

        private TempoMap TempoMap;
        private List<TimedEvent> Events { get; set; } = new List<TimedEvent>();
        private MidiStopwatch stopwatch { get; set; } = new MidiStopwatch();
        private MetricTimeSpan StartOffset { get; set; } = TimeSpan.Zero;
        private MidiDriver Driver { get; }
        private MidiClock Clock { get; set; }
        private MetricTimeSpan LastRead { get; set; } = TimeSpan.Zero;
        private Timer PlaybackEventTimer { get;}

        private MidiEventToBytesConverter Converter { get; } = new MidiEventToBytesConverter();

        public MidiPlayback(MidiDriver driver)
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

            var currentSecond = (long)(stopwatch.Elapsed + StartOffset).Seconds;

            if (lastSecond == currentSecond)
                return;

            lastSecond = currentSecond;

            Driver.DispatchEvent(MidiTimeUpdated, currentSecond, (long)((TimeSpan)Duration).TotalSeconds);
        }

        private async Task Driver_OnMidiClock()
        {
            if (!IsPlaying || IsPaused)
                return;

            try
            {
                var ts = stopwatch.Elapsed + (MetricTimeSpan)StartOffset;
                if (ts.TotalMicroseconds > Duration.TotalMicroseconds)
                    Stop();

                var last = ts.Milliseconds - LastRead.Milliseconds;

                if (last < 0)
                    last *= -1;

                if (last > 60)
                {
                    if (ts.TotalMicroseconds <= 60000)
                        LastRead = new MetricTimeSpan(0);
                    else
                        LastRead = ts - new MetricTimeSpan(60000);
                }

                var newEvents = Events.Where(x =>
                {
                    var pTS = x.TimeAs<MetricTimeSpan>(TempoMap);
                    if (pTS.TotalMicroseconds <= ts.TotalMicroseconds && pTS.TotalMicroseconds > LastRead.TotalMicroseconds)
                        Logger.Debug($"Got event {x.Event.EventType} -- {pTS.TotalMicroseconds} <= {ts.TotalMicroseconds} && {pTS.TotalMicroseconds} > {LastRead.TotalMicroseconds}", Severity.MIDI, Severity.Log);
                    return pTS.TotalMicroseconds <= ts.TotalMicroseconds && pTS.TotalMicroseconds > LastRead.TotalMicroseconds;
                }).ToArray();

                var tmpLast = LastRead;
                LastRead = ts;

                if (newEvents.Length == 0)
                    return;

                var buff = BuildPacket(newEvents.Select(x => GetMessage(x)).ToArray());

                Logger.Debug($"{tmpLast.TotalMicroseconds} - {ts.TotalMicroseconds} : {newEvents.Count()}", Severity.MIDI);

                if (buff != null && buff.Length > 0)
                {
                    if (!IsPlaying || IsPaused)
                        return;
                    await Driver.Writer.WritePacket(buff);
                    Logger.Write($"[{tmpLast.TotalMicroseconds} - {ts.TotalMicroseconds}] Sending 0x{string.Join("", buff.Select(x => x.ToString("X2")))}", Severity.MIDI);
                }
            }
            catch(Exception x)
            {
                Logger.Write(x, Severity.MIDI, Severity.Warning);
            }
        }

        public bool Seek(long miliseconds)
        {
            MetricTimeSpan offset = TimeSpan.FromMilliseconds(miliseconds);

            if (offset.TotalMicroseconds >= Duration.TotalMicroseconds)
                return false;
            this.Driver.OnMidiClock -= Driver_OnMidiClock;
            IsPlaying = false;
            stopwatch = MidiStopwatch.StartNew();
            LastRead = offset;
            StartOffset = offset;
            IsPlaying = true;
            lastSecond = (long)((TimeSpan)StartOffset).TotalSeconds - 1;
            this.Driver.OnMidiClock += Driver_OnMidiClock;
            Logger.Write($"Seeked to {miliseconds}", Severity.MIDI);
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

                Logger.Debug($"{string.Join("\n", Events.Select(x => $"{x.Time} - {x.Event.EventType}"))}", Severity.MIDI);

                LastRead = TimeSpan.Zero;

                Clock = new MidiClock(20, Driver);

                Duration = CurrentFile
                    .GetTimedEvents()
                    .LastOrDefault(e => e.Event is NoteOffEvent)
                    ?.TimeAs<MetricTimeSpan>(TempoMap) ?? new MetricTimeSpan();

                Logger.Write("Track length: " + ((TimeSpan)Duration).TotalSeconds + "s", Severity.MIDI, Severity.Driver);

                Clock.Start();
                StartOffset = TimeSpan.Zero;
                stopwatch.Reset();
                stopwatch.Start();
                lastSecond = 0;
                IsPaused = false;
                IsPlaying = true;
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
            if (msg.Length == 0)
                return null;

            List<byte> buff = new List<byte>();

            foreach (var item in msg.Where(x => x != null))
                buff.AddRange(item.Build());

            return buff.ToArray();
        }

        private OutgoingMidiMessage GetMessage(TimedEvent e)
        {
            if (!IsPlaying)
                return null;

            if (e == null)
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
                    default:
                        {
                            var buff = Converter.Convert(e.Event);

                            Logger.Write($"[{LastRead.TotalMicroseconds} - {e.TimeAs<MetricTimeSpan>(TempoMap).TotalMicroseconds}] Unknown handler: {e.Event.EventType} Sending 0x{string.Join("", buff.Select(x => x.ToString("X2")))}", Severity.MIDI);

                            
                            return new DefaultOutgoing(buff);
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
                IsPlaying = false;
                Logger.Write("Track ended", Severity.MIDI, Severity.Driver);
                this.CurrentFileName = null;
                Clock.Stop();
                CurrentFile = null;
                stopwatch.Stop();
                lastSecond = 0;
                Duration = TimeSpan.Zero;
                LastRead = TimeSpan.Zero;
                IsPaused = false;

                _ = Task.Run(async () =>
                {
                    await Driver.Writer.WritePacket((byte)StatusType.CC, (byte)CCType.AllNotesOff, 0x00);
                    await Driver.Writer.WritePacket((byte)StatusType.CC, (byte)CCType.AllSoundOff, 0x00);
                });

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
