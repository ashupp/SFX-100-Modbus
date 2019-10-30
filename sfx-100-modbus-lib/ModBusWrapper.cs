using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyModbus;
using EasyModbus.Exceptions;

namespace sfx_100_modbus_lib
{

    public class ModBusConfiguration
    {
        public string PortName;
        public int DataBits;
        public Parity Parity;
        public StopBits StopBits;
        public int Speed;
    }

    public class ModBusWrapper
    {
        private ModbusClient _modbusClient = new ModbusClient();

        public bool Connect(ModBusConfiguration config)
        {
            _modbusClient.SerialPort = config.PortName;
            _modbusClient.Parity = config.Parity;
            _modbusClient.StopBits = config.StopBits;
            _modbusClient.Baudrate = config.Speed;
            _modbusClient.ConnectionTimeout = 500;
            try
            {
                _modbusClient.Connect();
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
                return false;
            }
        }

        public bool Disconnect()
        {
            if (_modbusClient.Connected)
            {
                try
                {
                    _modbusClient.Disconnect();
                    return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                    return false;
                }
            }
            return false;
        }

        public List<int> SearchServos()
        {
            List<int> foundServos = new List<int>();
            for (int i = 1; i <= 4; i++)
            {
                setServoId(i);
                try
                {
                    var speedResp = _modbusClient.ReadHoldingRegisters(65, 1);
                    Console.WriteLine("Unit: " + i + " register: " + speedResp[0].ToString("D3"));
                    foundServos.Add(i);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("No Servo found with identifier: " + i + " -- " + ex.Message);
                }
            }

            return foundServos;
        }

        public Dictionary<int, int> ReadData(byte servoID, int start, int end)
        {
            Dictionary<int,int> readValues = new Dictionary<int, int>();
            setServoId(servoID);

            for (int i = start; i <= end; i++)
            {
                var tmpRes = _modbusClient.ReadHoldingRegisters(i, 1);
                readValues.Add(i,tmpRes[0]);
            }

            return readValues;
        }

        private void setServoId(int servoId)
        {
            _modbusClient.UnitIdentifier = Convert.ToByte(servoId);
        }

        public void Test()
        {

            
            var speedResp = _modbusClient.ReadHoldingRegisters(51, 1);
            Console.WriteLine("register: " + speedResp[0].ToString("D3"));
        }

    }
}
