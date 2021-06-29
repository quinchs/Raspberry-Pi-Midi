using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class MidiReader
    {
        private MidiPacketParser Parser;
        private MidiDriver Driver;
        private FileStream Stream
            => Driver.MidiStream;
        public MidiReader(MidiDriver driver)
        {
            this.Driver = driver;
            Parser = new MidiPacketParser();
        }

        public async Task ReaderAsync()
        {
            var cancelSource = new CancellationTokenSource();

            Logger.Write("Reading...", Severity.Driver, Severity.Reader);
            while (true)
            {
                try
                {
                    if (Driver.ReadCancel.Token.IsCancellationRequested)
                    {
                        Logger.Write("Read cancelled", Severity.Driver, Severity.Reader);
                        return;
                    }

                    byte[] buff = new byte[128];
                    var l = await Stream.ReadAsync(buff, cancelSource.Token);

                    var mEv = Parser.Parse(buff, 0, l);

                    foreach (var ev in mEv)
                    {
                        if (ev.Status != StatusType.MidiClock && ev.Status != StatusType.ActiveSense)
                            Logger.Debug($"{mEv.Count()} - {BitConverter.ToString(buff.Take(l).ToArray()).Replace("-", "")}\nRead {l}/{buff.Length} : {ev}\n", Severity.Driver, Severity.Reader);
                        Driver.DispatchMessageEvent(ev);
                    }
                }
                catch (OperationCanceledException) 
                { 
                    Logger.Write("Read cancelled", Severity.Driver, Severity.Reader);
                    Driver.HandleDeviceDisconnected();
                    return; 
                }
                catch (IOException x)
                {
                    if(x.Message == "No such device")
                    {
                        Logger.Write("Device disconnected", Severity.Driver, Severity.Reader);
                        Driver.HandleDeviceDisconnected();
                        return;
                    }
                    throw;
                }
                catch (Exception x)
                {
                    Logger.Write(x, Severity.Driver, Severity.Reader, Severity.Error);
                }
            }
        }
    }
}
