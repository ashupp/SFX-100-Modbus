using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace sfx_100_modbus_lib
{
    public class ServoConfigurationProfile
    {
        public DateTime Created { get; set; }

        [XmlAttribute]
        public string Name { get; set; }
        public string Info { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public List<Param> Parameters { get; set; }
    }
    public class Param
    {
        [XmlAttribute]
        public string key { get; set; }

        [XmlAttribute]
        public string value { get; set; }
    }
}
