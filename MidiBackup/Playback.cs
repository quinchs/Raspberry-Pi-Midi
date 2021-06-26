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
    class Playback
    {
        public MidiFile CurrentFile { get; private set; }
        private Melanchall.DryWetMidi.Devices.Playback MidiPlayback { get; set; }
        private TempoMap TempoMap;
        private List<TimedEvent> Events { get; set; } = new List<TimedEvent>();
        public bool IsPlaying { get; private set; }
        private Stopwatch stopwatch { get; set; } = new Stopwatch();

        private MidiDriver Driver { get; }

        private TimeSpan Duration { get; set; }

        private List<TimedEvent> AlreadyPlayedNotes { get; } = new List<TimedEvent>();

        public Playback(MidiDriver driver)
        {
            this.Driver = driver;
            this.Driver.OnMidiClock += Driver_OnMidiClock;
        }

        private async Task Driver_OnMidiClock()
        {
            if (!IsPlaying)
                return;

            if (stopwatch.Elapsed.Ticks > Duration.Ticks)
                Stop();

            var newEvents = Events.Where(x => x.TimeAs<MetricTimeSpan>(TempoMap) <= (MetricTimeSpan)stopwatch.Elapsed && !AlreadyPlayedNotes.Contains(x));

            foreach (var item in newEvents)
            {
                Console.WriteLine($"{item.Time} - {item.Event.EventType}");
                Playback_EventPlayed(null, item);
            }

            AlreadyPlayedNotes.AddRange(newEvents);
        }

        public void Start(string file)
        {
            this.CurrentFile = MidiFile.Read(file);

            this.TempoMap = MidiPlayback?.TempoMap ?? TempoMap.Default;

            Console.WriteLine($"Midi file opened: {CurrentFile} - {TempoMap}");

            AlreadyPlayedNotes.Clear();
            Events.Clear();
            Events = CurrentFile.GetTimedEvents().ToList();

            Duration = CurrentFile
                .GetTimedEvents()
                .LastOrDefault(e => e.Event is NoteOffEvent)
                ?.TimeAs<MetricTimeSpan>(TempoMap) ?? new MetricTimeSpan();

            Console.WriteLine("Track length: " + Duration.TotalSeconds + "s");
            IsPlaying = true;
            stopwatch.Reset();
            stopwatch.Start();
            Console.WriteLine("Track started");
        }

        private async void Playback_EventPlayed(object sender, TimedEvent e)
        {
            if (!IsPlaying)
                return;

            try
            {
                switch (e.Event.EventType)
                {
                    case MidiEventType.NoteOn:
                        {
                            var message = (NoteOnEvent)e.Event;
                            await Driver.Writer.WritePacket(new NoteOn(message.Channel, message.NoteNumber, message.Velocity));
                        }
                        break;
                    case MidiEventType.NoteOff:
                        {
                            var message = (NoteOffEvent)e.Event;
                            await Driver.Writer.WritePacket(new NoteOff(message.Channel, message.NoteNumber));
                        }
                        break;
                    case MidiEventType.ControlChange:
                        {
                            var controlChange = (ControlChangeEvent)e.Event;
                            await Driver.Writer.WritePacket(new CCMessage(controlChange.Channel, (CCType)(byte)controlChange.ControlNumber, controlChange.ControlValue));
                        }
                        break;
                }
            }
            catch(Exception x)
            {
                Console.WriteLine(x);
            }
        }

        public void Stop()
        {
            Console.WriteLine("Track ended");
            IsPlaying = false;
            CurrentFile = null;
            stopwatch.Stop();
            MidiPlayback.Stop();
            MidiPlayback = null;
            Duration = TimeSpan.Zero;
        }
    }
}
