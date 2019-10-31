using sfx_100_modbus_lib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using sfx_100_modbus_gui.Properties;
using Path = System.IO.Path;

namespace sfx_100_modbus_gui
{

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Dictionary containing available profiles in profile directory
        /// </summary>
        private Dictionary<string, string> _profileFilesAvailable;

        /// <summary>
        /// Holds the ModBusWrapper instance
        /// </summary>
        private ModBusWrapper _modBusWrapper = new ModBusWrapper();

        /// <summary>
        /// Holds the available servo Ids
        /// </summary>
        public ObservableCollection<int> AvailableServoIDs = new ObservableCollection<int>();

        /// <summary>
        /// Main Entry
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Log("startup");
            LoadAvailableProfiles();
        }

        #region private methods

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
        /// Reads current parameters from servo and saves them to file 
        /// </summary>
        /// <param name="servoId">ID of servo</param>
        private void BackupProfile(byte servoId)
        {
            Log("backupProfile - servo ID: " + servoId);
            var values = _modBusWrapper.ReadData(Convert.ToByte(servoId), 0, 280);
            SaveProfile(servoId, values, DateTime.Now.ToString("yyyyMMddHHmm") + "-" + servoId + "-backup");
        }

        /// <summary>
        /// Saves profile to profile folder
        /// </summary>
        /// <param name="servoId">ID of Servo from which the params are</param>
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
                using (XmlTextWriter writer = new XmlTextWriter(sww) { Formatting = Formatting.Indented})
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
                Log("Profile transferring to servo: " + servoId);
                if (_modBusWrapper.WriteProfile(Convert.ToByte(servoId), profile))
                {
                    Log("Transfer to servo: " + servoId + " successful");
                }
                else
                {
                    Log("Error: transfer to servo: " + servoId + " failed");
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
            Application.Current.Dispatcher?.BeginInvoke(
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
        /// Helper method. Repopulates the Servo IDs to the lists and boxes
        /// </summary>
        private void RePopulateLists()
        {
            // Todo: WPF Binding instead of this
            listBoxProfileServos.ItemsSource = AvailableServoIDs;
            cmbBoxBackupServo.ItemsSource = AvailableServoIDs;
        }


        #endregion

        #region ButtonClicks

        /// <summary>
        /// Eventhandler for click on connect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnConnect(object sender, RoutedEventArgs e)
        {
            Log("connect");
            ModBusConfiguration tmpConfig = new ModBusConfiguration
            {
                // Todo: Maybe pass plain values here and take care of conversion in wrapper
                PortName = comOptionsPort.SelectedValue.ToString(),
                DataBits = Convert.ToInt32(comOptionsDataBits.Text),
                Parity = (Parity) Enum.Parse(typeof(Parity), comOptionsParity.Text),
                StopBits = (StopBits) Enum.Parse(typeof(StopBits), comOptionsStopBit.Text),
                Speed = Convert.ToInt32(comOptionsSpeed.Text),
                ConnectionTimeout = Settings.Default.servoQueryTimeout
            };
            if (_modBusWrapper.Connect(tmpConfig))
            {
                btnConnect.IsEnabled = false;
                btnDisconnect.IsEnabled = true;
                grpSearchForServos.IsEnabled = true;

            }

        }

        /// <summary>
        /// Eventhandler for click on disconnect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDisconnect(object sender, RoutedEventArgs e)
        {
            Log("disconnect");
            if (_modBusWrapper.Disconnect())
            { 
                // Todo: Isnt it possible to observe the state and set the enabled state smarter?
                btnConnect.IsEnabled = true;
                btnDisconnect.IsEnabled = false;
                grpSearchForServos.IsEnabled = false;
                grpBackup.IsEnabled = false;
                grpTransfer.IsEnabled = false;

            }
        }

        /// <summary>
        /// Eventhandler for click on search servos
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnSearchServos(object sender, RoutedEventArgs e)
        {
            Log("searching servos - please wait");

            MainWindowElement.IsEnabled = false;
            AvailableServoIDs = await Task.Run((() => _modBusWrapper.SearchServos(Settings.Default.maxServoId)));
            MainWindowElement.IsEnabled = true;

            RePopulateLists();

            if (AvailableServoIDs.Count > 0)
            {
                grpBackup.IsEnabled = true;
                grpTransfer.IsEnabled = true;

                Log("Servos found: ");
                foreach (var servo in AvailableServoIDs)
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
        /// Eventhandler for click on check for double Ids
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCheckDoubleIds(object sender, RoutedEventArgs e)
        {
            Log("check for double servo IDs");
        }

        /// <summary>
        /// Eventhandler for click on backup profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnBackupAsProfile(object sender, RoutedEventArgs e)
        {
            var selectedServo = Convert.ToByte(cmbBoxBackupServo.SelectedValue);
            MainWindowElement.IsEnabled = false;
            await Task.Run(() => BackupProfile(selectedServo));
            LoadAvailableProfiles();
            MainWindowElement.IsEnabled = true;
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
                Log("transfer Profile: " + listServoProfiles.SelectedValue);

                Array servosArray = listBoxProfileServos.SelectedItems.Cast<int>().ToArray();
                var selectedProfile = _profileFilesAvailable[listServoProfiles.SelectedValue.ToString()];

                MainWindowElement.IsEnabled = false;
                await Task.Run((() => TransferProfile(selectedProfile, servosArray)));
                MainWindowElement.IsEnabled = true;
            }
            else
            {
                Log("Error: no profile selected");
            }
        }

        #endregion
    }
}
