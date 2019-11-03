﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimFeedback.conf;

namespace sfx_100_modbus_sfb_extension
{

    [Serializable]
    public class ModBusExtensionConfig : ICustomConfig
    {
        public int comOptionsDataBits = 8;
        public Parity comOptionsParity = Parity.Odd;
        public StopBits comOptionsStopBits = StopBits.One;
        public int comOptionsSpeed = 115200;
        public int servoQueryTimeout = 100;
        public string comOptionsLastConnectedPort = "";
        public bool comOptionsAutoConnectOnStartup = false;
        public int maxServoId = 8;
    }
}