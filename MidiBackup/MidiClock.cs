using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MidiBackup
{
    public class MidiClock
    { 
        public int Interval { get; }
        private MidiDriver Driver { get; }
        private Timer timer;
        public MidiClock(int interval, MidiDriver driver)
        {
            this.Interval = interval;
            timer = new(interval);

            Driver = driver;

            timer.Elapsed += Timer_Elapsed;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Driver.Writer.WritePacket(0xF8);
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

    }
}
