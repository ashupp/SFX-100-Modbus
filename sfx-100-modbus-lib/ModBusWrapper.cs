using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EasyModbus;

namespace sfx_100_modbus_lib
{
    public class ModBusWrapper
    {
        /// <summary>
        /// Instance of easymodbus client
        /// </summary>
        private ModbusClient _modbusClient = new ModbusClient();

        /// <summary>
        /// Connects to the ModBus
        /// </summary>
        /// <param name="config">Connection Parameters</param>
        /// <returns>True/False</returns>
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
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Disconnects from ModBus
        /// </summary>
        /// <returns>True/False</returns>
        public bool Disconnect()
        {
            if (_modbusClient.Connected)
            {
                try
                {
                    _modbusClient.Disconnect();
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            return false;
        }

        /// <summary>
        /// Searches for max 8 attached servos
        /// Throws Error if counted ID does not match configured ID
        /// </summary>
        /// <returns>Observable int Collection of found servos</returns>
        public ObservableCollection<int> SearchServos()
        {
            ObservableCollection<int> foundServos = new ObservableCollection<int>();
            for (int i = 1; i <= 8; i++)
            {
                SetServoId(i);
                try
                {
                    var servoIdResponse= ReadValueFromServo(65);
                    if (servoIdResponse != i)
                    {
                        throw new Exception("Wrong ID configured on Servo ID :" + i + " Current value is: " + servoIdResponse);
                    }
                    else
                    {
                        foundServos.Add(i);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("No Servo found with identifier: " + i + " -- " + ex.Message);
                }
            }
            return foundServos;
        }

        /// <summary>
        /// Reads a single value from servo
        /// </summary>
        /// <param name="address">Address to read from i.e 65 for Pn0065</param>
        /// <returns></returns>
        private int ReadValueFromServo(int address)
        {
            return _modbusClient.ReadHoldingRegisters(address, 1)[0];
        }

        /// <summary>
        /// Writes a complete servo parameter profile to the given servo
        /// </summary>
        /// <param name="servoId"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        public bool WriteProfile(byte servoId, ServoConfigurationProfile profile)
        {
            try
            {
                foreach (var param in profile.Parameters)
                {
                    Console.WriteLine("writing: " + param.Key + " - " + param.Value);
                    _modbusClient.WriteSingleRegister(Convert.ToInt32(param.Key), Convert.ToInt32(param.Value));
                }

                //Todo: Implement & Error handling
                return true;
            }
            catch (Exception)
            {
                // Todo: Error handling
                return false;
            }
        }

        /// <summary>
        /// Reads a complete servo parameter profile from the given servo
        /// </summary>
        /// <param name="servoId"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public Dictionary<int, int> ReadData(byte servoId, int start, int end)
        {
            Dictionary<int,int> readValues = new Dictionary<int, int>();
            SetServoId(servoId);

            for (int i = start; i <= end; i++)
            {
                var tmpRes = _modbusClient.ReadHoldingRegisters(i, 1);
                readValues.Add(i, tmpRes[0]);
            }

            return readValues;
        }

        /// <summary>
        /// Changes Servo Id
        /// </summary>
        /// <param name="servoId"></param>
        public void SetServoId(int servoId)
        {
            _modbusClient.UnitIdentifier = Convert.ToByte(servoId);
        }
    }
}
