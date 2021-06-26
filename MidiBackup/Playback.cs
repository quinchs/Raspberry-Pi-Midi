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

namespace MidiBackup
{
    public class Playback
    {
        public MidiFile CurrentFile { get; private set; }
        private TempoMap TempoMap;
        private List<TimedEvent> Events { get; set; } = new List<TimedEvent>();
        public bool IsPlaying { get; private set; }
        private Stopwatch stopwatch { get; set; } = new Stopwatch();

        private MidiDriver Driver { get; }

        private TimeSpan Duration { get; set; }

        private MidiClock Clock { get; set; }

        private MetricTimeSpan LastRead { get; set; } = TimeSpan.Zero;

        public Playback(MidiDriver driver)
        {
            this.Driver = driver;
            this.Driver.OnMidiClock += Driver_OnMidiClock;
        }

        private async Task Driver_OnMidiClock()
        {
            if (!IsPlaying)
                return;

            try
            {
                if (stopwatch.Elapsed.Ticks > Duration.Ticks)
                    Stop();

                var ts = (MetricTimeSpan)stopwatch.Elapsed;
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

        public void Start(string file)
        {
            try
            {
                this.CurrentFile = MidiFile.Read(file);

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
                stopwatch.Reset();
                stopwatch.Start();
                Logger.Write("Track started", Severity.MIDI, Severity.Driver);
            }
            catch(Exception x)
            {
                Logger.Write(x, Severity.Error);
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
                Clock.Stop();
                CurrentFile = null;
                stopwatch.Stop();
                Duration = TimeSpan.Zero;
                LastRead = TimeSpan.Zero;
            }
        }
    }
}
