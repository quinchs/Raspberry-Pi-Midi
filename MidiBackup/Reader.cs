using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class Reader
    {
        private Parser Parser;
        private MidiDriver Driver;
        private FileStream Stream
            => Driver.MidiStream;
        public Reader(MidiDriver driver)
        {
            this.Driver = driver;
            Parser = new Parser();
        }

        public async Task ReaderAsync()
        {
            Console.WriteLine("Reading...");
            while (true)
            {
                try
                {
                    if (Driver.ReadCancel.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Read cancelled");
                        return;
                    }

                    byte[] buff = new byte[128];
                    var l = await Stream.ReadAsync(buff);

                    var mEv = Parser.Parse(buff, 0, l);

                    foreach (var ev in mEv)
                    {
                        if (Driver.Config.Debug)
                            Console.WriteLine($"{mEv.Count()} - {BitConverter.ToString(buff.Take(l).ToArray()).Replace("-", "")}\nRead {l}/{buff.Length} : {ev}\n");
                        Driver.DispatchMessageEvent(ev);
                    }
                }
                catch (OperationCanceledException) { Console.WriteLine("Read cancelled"); return; }
                catch (IOException x)
                {
                    if(x.Message == "No such device")
                    {
                        Driver.HandleDeviceDisconnected();
                        return;
                    }
                    throw;
                }
                catch (Exception x)
                {
                    Console.WriteLine(x);
                }
            }
        }
    }
}
