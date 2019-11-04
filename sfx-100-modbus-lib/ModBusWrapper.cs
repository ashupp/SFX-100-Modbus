using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using EasyModbus;

namespace sfx_100_modbus_lib
{
    public class ModBusWrapper
    {
        /// <summary>
        /// Current connection status
        /// </summary>
        public bool IsConnected {
            get
            {
                return _modbusClient.Connected;
            }}

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
            _modbusClient.ConnectionTimeout = config.ConnectionTimeout;
            try
            {
                _modbusClient.Connect();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
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
                }
            }
            return false;
        }

        /// <summary>
        /// Searches for given Servo count
        /// </summary>
        /// <returns>Observable int Collection of found servos</returns>
        public ObservableCollection<int> SearchServos(int maxId)
        {
            ObservableCollection<int> foundServos = new ObservableCollection<int>();
            for (int i = 1; i <= maxId; i++)
            {
                SetServoId(i);
                try
                {
                    var servoIdResponse= ReadValueFromServo(65);
                    foundServos.Add(servoIdResponse);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("No Servo found with identifier: " + i + " -- " + ex.Message);
                }
            }
            return foundServos;
        }

        /// <summary>
        /// Reads a single value from given servo
        /// </summary>
        /// <param name="servoId">ID of servo to read parameter from</param>
        /// <param name="address">Address to read from i.e 65 for Pn0065</param>
        /// <returns></returns>
        public int ReadValueFromServo(int servoId, int address)
        {
            SetServoId(servoId); 
            return ReadValueFromServo(address);
        }

        /// <summary>
        /// Reads a single value from current servo
        /// </summary>
        /// <param name="address">Address to read from i.e 65 for Pn0065</param>
        /// <returns>value</returns>
        public int ReadValueFromServo(int address)
        {
            int readValue;
            try
            {
                readValue = _modbusClient.ReadHoldingRegisters(address, 1)[0];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return readValue;
        }

        /// <summary>
        /// Writes a single value to a given servo
        /// </summary>
        /// <param name="servoId">Id of the servo</param>
        /// <param name="address">Adress to write</param>
        /// <param name="value">value to write</param>
        /// <returns>true/false</returns>
        public bool WriteValueToServo(int servoId, int address, int value)
        {
            SetServoId(servoId);
            return WriteValueToServo(address, value);
        }

        /// <summary>
        /// Writes a single value to servo.
        /// </summary>
        /// <param name="address">Adress to write</param>
        /// <param name="value">value to write</param>
        /// <returns>true/false</returns>
        public bool WriteValueToServo(int address, int value)
        {
            try
            {
                // Only write if value is different to current value. Try to write same value triggers no error tough.
                if (ReadValueFromServo(address) != value)
                {
                    _modbusClient.WriteSingleRegister(address, value);
                    Thread.Sleep(6);
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Writes a complete servo parameter profile to the given servo
        /// </summary>
        /// <param name="servoId"></param>
        /// <param name="profile"></param>
        /// <param name="overwriteId">Should the ID of the Servo be overwritten as well? true/false Defaults to false </param>
        /// <returns></returns>
        public bool WriteProfile(byte servoId, ServoConfigurationProfile profile, bool overwriteId = false)
        {
            try
            {
                SetServoId(servoId);
                foreach (var param in profile.Parameters)
                {
                    // Special check if overwriteId is set
                    if (param.Key == 65 && overwriteId == false)
                    {
                        Console.WriteLine("omitting: " + param.Key + " - " + param.Value);
                        continue;
                    }
                    Console.WriteLine("writing: " + param.Key + " - " + param.Value);
                    try
                    {
                        WriteValueToServo(param.Key, param.Value);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);

                    }
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

        /// <summary>
        /// Saves the whole Settings to EEPROM
        /// </summary>
        public bool PersistParametersToMemory(int servoId)
        {
            SetServoId(servoId);
            var currentPersistState = ReadValueFromServo(81);
            if (currentPersistState == 0)
            {
                WriteValueToServo(81, 1);
            }
            else
            {
                if (WriteValueToServo(81, 0))
                {

                }
                WriteValueToServo(81, 1);
            }
            // We have to wait until saving is complete
            Thread.Sleep(5000);

            return true;
        }
    }
}
