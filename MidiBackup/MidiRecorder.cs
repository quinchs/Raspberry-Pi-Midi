using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class MidiRecorder
    {
        public MidiDriver Driver { get; }

        public bool IsRecording { get; private set; }

        public TimeSpan Duration
            => Events.LastOrDefault().time;

        private MidiFile File { get; set; }

        private List<(TimeSpan time, MidiEvent evnt)> Events { get; set; } = new List<(TimeSpan time, MidiEvent evnt)>();

        private List<MidiEvent> LazySustainedEvents = new List<MidiEvent>();

        private readonly Stopwatch _stopwatch = new Stopwatch();

        public bool SustainedOn { get; set; } = false;

        public MidiRecorder(MidiDriver driver)
        {
            this.Driver = driver;

            this.Driver.OnNotePressed += NotePressed;
            this.Driver.OnNoteReleased += NoteReleased;
            this.Driver.OnSustain += OnSustain;
        }

        public async Task OnSustain(MidiSustainEventArg arg)
        {
            if (!IsRecording)
                return;

            var sustain = new ControlChangeEvent((SevenBitNumber)0x40, (SevenBitNumber)arg.Value);
            SustainedOn = arg.Value > 0;

            Events.Add((_stopwatch.Elapsed, sustain));

            if (LazySustainedEvents.Any())
            {
                foreach(var item in LazySustainedEvents)
                    Events.Add((_stopwatch.Elapsed, item));

                LazySustainedEvents.Clear();
            }
        }

        public async Task NoteReleased(MidiNoteEventArgs arg)
        {
            if (!IsRecording)
                return;

            var note = new NoteOffEvent((SevenBitNumber)arg.Note, (SevenBitNumber)arg.Velocity);

            if (!Driver.Config.LazySustain)
            {
                Events.Add((_stopwatch.Elapsed, note));
            }
            else
            {
                if (!SustainedOn)
                    Events.Add((_stopwatch.Elapsed, note));
                else LazySustainedEvents.Add(note);
            }
        }

        public async Task NotePressed(MidiNoteEventArgs arg)
        {
            if (!IsRecording)
                return;

            var note = new NoteOnEvent((SevenBitNumber)arg.Note, (SevenBitNumber)arg.Velocity);

            var val = (_stopwatch.Elapsed, note);
            Events.Add(val);

            //if (SustainedOn && Driver.Config.LazySustain)
            //    LazySustainedEvents.Add(val.note);
        }

        public void Start()
        {
            Reset();
            IsRecording = true;
            _stopwatch.Start();
        }

        public void Reset()
        {
            if (IsRecording)
                Stop();
            _stopwatch.Reset();
        }

        public void Stop()
        {
            IsRecording = false;
        }

        public void Save(string path)
        {
            try
            {
                var chunk = new TrackChunk(Events.Select(x => x.evnt));

                var tempoMap = TempoMap.Default;
                var manager = new TimedEventsManager(chunk.Events);
                TimedEventsCollection timedEvents = manager.Events;
                timedEvents.Clear();

                timedEvents.Add(Events.Select(x => new TimedEvent(x.evnt, TimeConverter.ConvertFrom((MetricTimeSpan)x.time.Add(TimeSpan.FromSeconds(2)), tempoMap))));

                manager.SaveChanges();

                File = new MidiFile(chunk);
                File.Write(path);
            }
            catch(Exception x)
            {
                Logger.Write(x, Severity.Error);
            }
        }
    }
}
