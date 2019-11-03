using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using sfx_100_modbus_gui;
using sfx_100_modbus_lib;

namespace sfx_100_modbus_sfb_extension
{
    /// <summary>
    /// Interaktionslogik für ModBusExtensionControlGUI.xaml
    /// </summary>
    public partial class ModBusExtensionControlGUI : UserControl
    {
        private ModBusExtensionConfig Settings;

        public ModBusExtensionControlGUI()
        {
            InitializeComponent();

            modBusExtensionTitle.Content = modBusExtensionTitle.Content + " - " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            DataContext = this;
            Log("startup Extension GUI");
        }

        public void setConfig(ModBusExtensionConfig cfg)
        {
            Settings = cfg;
        }

        #region Members

        /// <summary>
        /// Dictionary containing available profiles in profile directory
        /// </summary>
        private Dictionary<string, string> _profileFilesAvailable;

        /// <summary>
        /// Holds the ModBusWrapper instance
        /// </summary>
        private sfx_100_modbus_lib.ModBusWrapper _modBusWrapper = new ModBusWrapper();

        /// <summary>
        /// Holds the available servo Ids
        /// </summary>
        public ObservableCollection<int> AvailableServoIds = new ObservableCollection<int>();

        /// <summary>
        /// Holds the available serial ports
        /// </summary>
        public ObservableCollection<string> AvailableSerialPorts;

        #endregion



        #region Private methods

        /// <summary>
        /// Loads available xml based servo profiles from applications subdirectory (servo-profiles)
        /// </summary>
        private void LoadAvailableProfiles()
        {
            Log("loadAvailableProfiles");

            var servoProfilesPath = Path.Combine(Directory.GetCurrentDirectory(), "servo-profiles");
            if (Directory.Exists(servoProfilesPath))
            {
                var foundServoProfiles = Directory.GetFiles(servoProfilesPath, "*.xml");
                listServoProfiles.Items.Clear();
                _profileFilesAvailable = new Dictionary<string, string>();
                // TODO: Maybe read an profile name set inside the xml file instead of file name. Checks also integrity of File through Deserialisation
                foreach (var availableProfile in foundServoProfiles)
                {
                    var shortProfileName = Path.GetFileNameWithoutExtension(availableProfile);
                    _profileFilesAvailable.Add(shortProfileName, availableProfile);
                    listServoProfiles.Items.Add(shortProfileName);
                }

                Log("servo-profile path and files loaded");
            }
            else
            {
                Log("servo-profile path not available - try to create path: " + servoProfilesPath);
                try
                {
                    Directory.CreateDirectory(servoProfilesPath);
                    Log("servo-profile path created");
                }
                catch (Exception ex)
                {
                    Log("Error during creation of Servo profiles directory. " + ex.Message);
                }
            }

        }

        /// <summary>
        /// Tries to connects to ModBus
        /// </summary>
        private void Connect()
        {
            Log("Connecting...");
            var portName = comOptionsPort.SelectedValue.ToString();
            ModBusConfiguration tmpConfig = new ModBusConfiguration
            {
                PortName = portName,
                DataBits = Settings.comOptionsDataBits,
                Parity = Settings.comOptionsParity,
                StopBits = Settings.comOptionsStopBits,
                Speed = Settings.comOptionsSpeed,
                ConnectionTimeout = Settings.servoQueryTimeout
            };
            if (_modBusWrapper.Connect(tmpConfig))
            {
                // Remember current Port by writing to Settings
                Settings.comOptionsLastConnectedPort = portName;

                btnConnect.IsEnabled = false;
                btnDisconnect.IsEnabled = true;
                Log("Connected");
                SearchServos();
            }
            else
            {
                Log("Error: Could not connect to port: " + portName);
            }
        }

        /// <summary>
        /// Disconnects from ModBus
        /// </summary>
        private void Disconnect()
        {
            Log("disconnect");
            _modBusWrapper.Disconnect();
            // Todo: Isnt it possible to observe the state and set the enabled state smarter?
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = false;
            grpBackup.IsEnabled = false;
            grpTransfer.IsEnabled = false;
        }

        /// <summary>
        /// Searches for servos and provides them in AvailableServoIds
        /// </summary>
        private async void SearchServos()
        {
            Log("searching servos - please wait");

            ModBusExtensionControlGuiElement.IsEnabled = false;
            AvailableServoIds = await Task.Run((() => _modBusWrapper.SearchServos(Settings.maxServoId)));
            ModBusExtensionControlGuiElement.IsEnabled = true;

            RePopulateLists();

            if (AvailableServoIds.Count > 0)
            {
                grpBackup.IsEnabled = true;
                grpTransfer.IsEnabled = true;

                Log("Servos found: ");
                foreach (var servo in AvailableServoIds)
                {
                    Log("Servo Id: " + servo);
                }
            }
            else
            {
                grpBackup.IsEnabled = false;
                grpTransfer.IsEnabled = false;

                Log("No Servos found");
            }
        }

        /// <summary>
        /// Reads current parameters from servo and saves them to file 
        /// </summary>
        /// <param name="servoId">Id of servo</param>
        private void BackupProfile(byte servoId)
        {
            Log("backupProfile - servo Id: " + servoId);
            var values = _modBusWrapper.ReadData(Convert.ToByte(servoId), 0, 280);
            SaveProfile(servoId, values, DateTime.Now.ToString("yyyyMMddHHmm") + "-" + servoId + "-backup");
        }

        /// <summary>
        /// Saves profile to profile folder
        /// </summary>
        /// <param name="servoId">Id of Servo from which the params are</param>
        /// <param name="values">Values from ModBusWrapper</param>
        /// <param name="profileName">Name of the profile to be saved</param>
        /// <param name="author">Author of the profile</param>
        /// <param name="version">Version of the profile</param>
        /// <param name="info">Additional information of the profile</param>
        private void SaveProfile(byte servoId, Dictionary<int, int> values, string profileName = "", string author = "", string version = "", string info = "")
        {
            ServoConfigurationProfile tmpSet = new ServoConfigurationProfile();

            tmpSet.Name = profileName;
            tmpSet.Created = DateTime.Now;
            tmpSet.Author = author;
            tmpSet.Version = version;
            tmpSet.Info = info;
            tmpSet.OriginServoId = servoId;
            tmpSet.Parameters = new List<Param>();

            foreach (var val in values)
            {
                var tmpParam = new Param() { Key = val.Key, Value = val.Value };
                tmpSet.Parameters.Add(tmpParam);
            }

            XmlSerializer xmlProfileSerializer = new XmlSerializer(typeof(ServoConfigurationProfile));

            using (var sww = new StringWriterWithEncoding(Encoding.UTF8))
            {
                using (XmlTextWriter writer = new XmlTextWriter(sww) { Formatting = Formatting.Indented })
                {
                    xmlProfileSerializer.Serialize(writer, tmpSet);
                    var xml = sww.ToString();

                    var profilePath = Path.Combine(Directory.GetCurrentDirectory(), "servo-profiles", profileName + ".xml");
                    File.WriteAllText(profilePath, xml);
                    Log("profile saved: " + profilePath);
                }
            }
        }

        /// <summary>
        /// Loads and returns a given profile 
        /// </summary>
        /// <param name="profilePath">path to profile to load</param>
        /// <returns>profile</returns>
        private ServoConfigurationProfile LoadProfile(string profilePath)
        {
            XmlSerializer xmlProfileSerializer = new XmlSerializer(typeof(ServoConfigurationProfile));
            ServoConfigurationProfile profile;
            using (var reader = new FileStream(profilePath, FileMode.Open))
            {
                profile = (ServoConfigurationProfile)xmlProfileSerializer.Deserialize(reader);
            }
            return profile;
        }

        /// <summary>
        /// Transfers the given profile to the given Servos
        /// </summary>
        /// <param name="profilePath"></param>
        /// <param name="servos">Array of Servos to which the profile will be transferred</param>
        private void TransferProfile(string profilePath, Array servos)
        {
            Log("Transferring profile:  " + profilePath);
            var profile = LoadProfile(profilePath);

            foreach (var servoId in servos)
            {
                Log("Profile transferring to servo Id: " + servoId);
                if (_modBusWrapper.WriteProfile(Convert.ToByte(servoId), profile))
                {
                    Log("Transfer to servo Id: " + servoId + " successful");
                }
                else
                {
                    Log("Error: transfer to servo Id: " + servoId + " failed");
                }
            }

            Log("Profile transfer finished");
            //throw new NotImplementedException();

        }

        #endregion

        #region Private helpers

        /// <summary>
        /// Simple logging to window. Holds only limited entries to prevent scrolling
        /// </summary>
        /// <param name="logEntry">Object to log</param>
        private void Log(object logEntry)
        {
            Dispatcher?.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    if (debugBox.Items.Count >= 36)
                    {
                        debugBox.Items.RemoveAt(0);
                    }

                    debugBox.Items.Add(DateTime.Now + ": " + logEntry);
                    debugBox.SelectedIndex = debugBox.Items.Count - 1;
                    debugBox.ScrollIntoView(debugBox.SelectedItem);
                }));
        }

        /// <summary>
        /// Helper method. Repopulates the Servo Ids to the lists and boxes
        /// </summary>
        private void RePopulateLists()
        {
            // Todo: Maybe WPF Binding instead of this
            listBoxProfileServos.ItemsSource = AvailableServoIds;
            cmbBoxBackupServo.ItemsSource = AvailableServoIds;
            listBoxManipulationServos.ItemsSource = AvailableServoIds;
        }

        /// <summary>
        /// Loads available serial ports and populates in listbox
        /// </summary>
        private void LoadAvailableSerialPorts()
        {
            try
            {
                comOptionsPort.ItemsSource = SerialPort.GetPortNames();
            }
            catch (Exception e)
            {
                Log("Could not get serial port names: " + e.Message);
            }
        }

        #endregion

        #region Eventhandlers

        /// <summary>
        /// Eventhandler called when main window is loaded.
        /// Loads profiles, serial ports and tries to connect automatically - if set.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadAvailableProfiles();

            try
            {
                LoadAvailableSerialPorts();
                comOptionsPort.SelectedValue = Settings.comOptionsLastConnectedPort;

                if (Settings.comOptionsAutoConnectOnStartup && comOptionsPort.SelectedValue != null)
                {
                    Log("Try to connect automatically to: " + comOptionsPort.SelectedValue);
                    Connect();
                }
            }
            catch (Exception)
            {
                Log("Warning: Could not set last connected Port (not found)");
            }
        }

        /// <summary>
        /// Eventhandler is called when refresh serial ports is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComPortsRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            LoadAvailableSerialPorts();
        }

        /// <summary>
        /// Eventhandler for click on connect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnConnect(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        /// <summary>
        /// Eventhandler for click on disconnect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDisconnect(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }

        /// <summary>
        /// Eventhandler for click on backup profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnBackupAsProfile(object sender, RoutedEventArgs e)
        {
            var selectedServo = Convert.ToByte(cmbBoxBackupServo.SelectedValue);
            ModBusExtensionControlGuiElement.IsEnabled = false;
            await Task.Run(() => BackupProfile(selectedServo));
            LoadAvailableProfiles();
            ModBusExtensionControlGuiElement.IsEnabled = true;
        }

        /// <summary>
        /// Eventhandler for click on profiles refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnProfilesRefresh(object sender, RoutedEventArgs e)
        {
            LoadAvailableProfiles();
        }

        /// <summary>
        /// Eventhandler for click on transfer profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnTransferProfile(object sender, RoutedEventArgs e)
        {
            if (listServoProfiles.SelectedValue != null)
            {
                Log("Transfer Profile: " + listServoProfiles.SelectedValue);

                Array servosArray = listBoxProfileServos.SelectedItems.Cast<int>().ToArray();
                var selectedProfile = _profileFilesAvailable[listServoProfiles.SelectedValue.ToString()];

                ModBusExtensionControlGuiElement.IsEnabled = false;
                await Task.Run((() => TransferProfile(selectedProfile, servosArray)));
                ModBusExtensionControlGuiElement.IsEnabled = true;
            }
            else
            {
                Log("Error: no profile selected");
            }
        }

        /// <summary>
        /// Eventhandle called when Main window is closing.
        /// Disconnects from ModBus if connected and saves profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_modBusWrapper.IsConnected)
            {
                Disconnect();
            }
            //TODO Save Settings
            //Settings.Save();
        }



        #endregion

        /// <summary>
        /// Eventhandler triggered wehen slider value has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rpmSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var valueToSet = Convert.ToInt32(e.NewValue);
            Console.WriteLine();

            if (valueToSet >= 0 && valueToSet <= 3000)
            {
                foreach (int servoId in listBoxProfileServos.SelectedItems)
                {
                    _modBusWrapper.WriteValueToServo(servoId, 52, valueToSet);
                    Log("RPM: Value: " + valueToSet + " written to servo: " + servoId);
                    var checkValue =_modBusWrapper.ReadValueFromServo(servoId, 52);
                    Log("RPM: Value Check - Got: " + checkValue + " from servo: " + servoId);
                }
            }
            else
            {
                rpmSlider.Value = e.OldValue;
            }
        }

        private void listBoxManipulationServos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Try to get value from servo drives. 

            List<int> currentValues = new List<int>();
            var valueToSet = 3000;

            foreach (int servoId in listBoxProfileServos.SelectedItems)
            {
                currentValues[servoId] = _modBusWrapper.ReadValueFromServo(servoId, 51);
            }

            if (currentValues.Any(o => o != currentValues[0]))
            {
                Log("RPM change: Values of all selected servos are not the same. Writing 3000 for all selected");
                rpmSlider.Value = 3000;
            }
            else
            {
                rpmSlider.Value = currentValues[0];
            }
        }
    }
}
