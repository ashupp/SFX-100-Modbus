using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace sfx_100_modbus_lib
{

    /// <summary>
    /// Represents an Profile
    /// Can be XML- Serialized and Deserialized.
    /// </summary>
    public class ServoConfigurationProfile
    {
        /// <summary>
        /// DateTime when profile was saved
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Name of the Profile. Being Set as Attribute of ServoConfigurationProfile
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// Additional Information about the profile
        /// TODO: Currently not in programmatically use
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// Author of the profile
        /// TODO: Currently not in programmatically use
        /// </summary>
        public string Author { get; set; }
        
        /// <summary>
        /// Version of the profile
        /// TODO: Currently not in programmatically use
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Servo ID from which the profile was saved
        /// </summary>
        public byte OriginServoId { get; set; }

        /// <summary>
        /// List of PnXXX Parameters
        /// </summary>
        public List<Param> Parameters { get; set; }
        
        /// <summary>
        /// List of Valid Servos for this profile
        /// </summary>
        public List<Servo> ValidForServos { get; set; }
    }

    /// <summary>
    /// Class Servo Contains Servo IDs and names for which this profile is valid
    /// </summary>
    public class Servo
    {
        /// <summary>
        /// ID of the Servo
        /// </summary>
        [XmlAttribute]
        public int Id { get; set; }

        /// <summary>
        /// Name of the Servo
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }
    }

    /// <summary>
    /// Param contains a PnXXX parameter of the Servo
    /// </summary>
    public class Param
    {
        /// <summary>
        /// Key of the Parameter (i.e 65 for Pn0065)
        /// </summary>
        [XmlAttribute]
        public int Key { get; set; }

        /// <summary>
        /// Value of the Parameter
        /// </summary>
        [XmlAttribute]
        public int Value { get; set; }
    }
}
