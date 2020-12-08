﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiLagMod.settings.utilities
{
    public class PluginConfig
    {
        public static PluginConfig Instance;

        public bool modEnabled;
        public float frameThreshold = 5f;
        public bool frameDropDetectionEnabled;
        public float waitThenActive = 1f;
        public bool trackingErrorDetectionEnabled;
        public float driftThreshold = 10f;
        public float playerHeight = 1.2f;

    }
}
