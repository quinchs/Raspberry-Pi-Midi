using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class MidiStopwatch
    {
        public MetricTimeSpan Elapsed
            => IsRunning ? DateTime.UtcNow - StartTime : TimeSpan.Zero;

        public bool IsRunning { get; private set; }

        private DateTime StartTime { get; set; }

        
        public void Start()
        {
            Reset();
            IsRunning = true;
        }

        public void Stop()
        {
            Reset();
            IsRunning = true;
        }

        public void Reset()
        {
            StartTime = DateTime.UtcNow;
        }

        public static MidiStopwatch StartNew()
        {
            var sw = new MidiStopwatch();
            sw.Start();
            return sw;
        }
    }
}
