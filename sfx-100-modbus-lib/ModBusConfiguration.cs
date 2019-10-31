using System.IO.Ports;

namespace sfx_100_modbus_lib
{
    /// <summary>
    /// Configuration Class for Modbus
    /// </summary>
    public class ModBusConfiguration
    {
        public string PortName;
        public int DataBits;
        public Parity Parity;
        public StopBits StopBits;
        public int Speed;
    }
}