using MidiBackup.Outgoing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class MidiWriter
    {
        private MidiDriver Driver;
        private Stream Stream
            => Driver.MidiStream;
        public MidiWriter(MidiDriver driver)
        {
            this.Driver = driver;
        }

        public async Task WritePacket(params byte[] buff)
        {
            if (buff.Length == 0)
                return;

            Logger.Debug($"Sending {buff.Length} bytes to midi device: {BitConverter.ToString(buff).Replace("-", "")}", Severity.MIDI, Severity.Writer);

            await Stream.WriteAsync(buff);
            await Stream.FlushAsync();
        }

        public Task WritePacket(OutgoingMidiMessage message)
            => WritePacket(message.Build());
    }
}
