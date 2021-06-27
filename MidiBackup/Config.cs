﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup
{
    public class Config
    {
        public bool Debug { get; set; }
        public int Port { get; set; } = 3000;
        public bool LazySustain { get; set; } = false;
        public long PlaybackDispatchTime { get; set; } = 100;
    }
}
