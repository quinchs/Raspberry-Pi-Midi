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

        private MidiFile File { get; set; }

        private List<(TimeSpan time, MidiEvent evnt)> Events { get; set; } = new List<(TimeSpan time, MidiEvent evnt)>();

        private readonly Stopwatch _stopwatch = new Stopwatch();

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
            Events.Add((_stopwatch.Elapsed, sustain));
        }

        public async Task NoteReleased(MidiNoteEventArgs arg)
        {
            if (!IsRecording)
                return;

            var note = new NoteOffEvent((SevenBitNumber)arg.Note, (SevenBitNumber)arg.Velocity);
            Events.Add((_stopwatch.Elapsed, note));
        }

        public async Task NotePressed(MidiNoteEventArgs arg)
        {
            if (!IsRecording)
                return;

            var note = new NoteOnEvent((SevenBitNumber)arg.Note, (SevenBitNumber)arg.Velocity);
            Events.Add((_stopwatch.Elapsed, note));
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
                Console.WriteLine(x);
            }
        }
    }
}
