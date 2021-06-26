using MidiBackup.Outgoing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class Writer
    {
        private MidiDriver Driver;
        private Stream Stream
            => Driver.MidiStream;
        public Writer(MidiDriver driver)
        {
            this.Driver = driver;
        }

        public async Task WritePacket(OutgoingMidiMessage message)
        {
            var buff = message.Build();

            Console.WriteLine($"Sending {buff.Length} bytes to midi device: {BitConverter.ToString(buff).Replace("-", "")}");

            await Stream.WriteAsync(buff);
            await Stream.FlushAsync();
        }
    }
}
