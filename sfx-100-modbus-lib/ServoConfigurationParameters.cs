using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace sfx_100_modbus_lib
{
    /// <summary>
    /// Represents possible ServoConfigurationParameters
    /// Can be XML- Serialized and Deserialized.
    /// </summary>
    public class ServoConfigurationParameters
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
        /// List of PnXXX Parameters
        /// </summary>
        public List<ServoPropertyParam> Parameters { get; set; }

        /// <summary>
        /// Searches and returns parameter set by key
        /// </summary>
        /// <param name="key">Key to search for (i.e 115)</param>
        /// <returns></returns>
        public ServoPropertyParam GetParamByKey(int key)
        {
            var retVal = new ServoPropertyParam();
            foreach (var parameter in Parameters)
            {
                if (parameter.Key == key)
                {
                    retVal = parameter;
                    break;
                }
            }
            return retVal;
        }
    }

    /// <summary>
    /// Contains a PnXXX representation with its properties
    /// </summary>
    [XmlType("Param")]
    public class ServoPropertyParam
    {
        /// <summary>
        /// Key of the parameter (i.e 65 for Pn0065)
        /// </summary>
        [XmlAttribute]
        public int Key { get; set; }

        /// <summary>
        /// Name of the parameter
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// Description of the parameter
        /// </summary>
        [XmlAttribute]
        public string Description { get; set; }

        /// <summary>
        /// Min value of the parameter
        /// </summary>
        [XmlAttribute]
        public int Min { get; set; }

        /// <summary>
        /// Max value of the parameter
        /// </summary>
        [XmlAttribute]
        public int Max { get; set; }

        /// <summary>
        /// Default value of the parameter
        /// </summary>
        [XmlAttribute]
        public int Default { get; set; }

        /// <summary>
        /// Value unit of the parameter
        /// </summary>
        [XmlAttribute]
        public string Unit { get; set; }

        /// <summary>
        /// Bool is re-enabling of the servo necessary after change
        /// </summary>
        [XmlAttribute]
        public bool ReEnable { get; set; }

        /// <summary>
        /// Bool is re-powering of the servo necessary after change
        /// </summary>
        [XmlAttribute]
        public bool RePower { get; set; }

        /// <summary>
        /// Settings appliance
        /// "All" is indicated for torque, speed, position control, T, for torque control, S for speed control, and P for position control.
        /// </summary>
        [XmlAttribute]
        public string Apply { get; set; }

        /// <summary>
        /// Risk level of the Setting
        /// 0 - 5
        /// </summary>
        [XmlAttribute]
        public int RiskLevel { get; set; }
    }
}
