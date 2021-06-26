using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class Config
    {
        /// <summary>
        ///     Gets or sets the device name for reading midi from.
        /// </summary>
        public string DeviceName { get; set; }
        public byte DeviceNumber { get; set; } = 0x02;
        public bool Debug { get; set; }
    }
}
