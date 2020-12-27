using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using sfx_100_modbus_gui.Properties;
using sfx_100_modbus_lib;
using sfx_100_modbus_sfb_extension;
using MessageBox = System.Windows.MessageBox;

namespace sfx_100_modbus_gui
{
    /// <summary>
    /// Interaktionslogik für ModBusExtensionControlGUI.xaml
    /// </summary>
    public partial class ModBusExtensionControlGui
    {

        #region Main Entry

        public ModBusExtensionControlGui()
        {
            InitializeComponent();

            _servoProfilesPath = GetServoProfilesPath();
            _servoParametersFile = getServoParametersPath();
            modBusExtensionTitle.Content = modBusExtensionTitle.Content + " - " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            DataContext = this;
            Log("startup Extension GUI");
        }

        #endregion

        #region Members

        /// <summary>
        /// Path containing servo profiles
        /// </summary>
        private string _servoProfilesPath;

        /// <summary>
        /// Path containing servo parameters file
        /// </summary>
        private string _servoParametersFile;

        private Dictionary<int, PropertySliderControl> _propertySliderControlsList;

        /// <summary>
        /// Contains the possible servo configuration parameters
        /// </summary>
        private ServoConfigurationParameters _servoConfigurationParameters;

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
        public ObservableCollection<int> AvailableServoIds = new ObservableCollection<int>();

        /// <summary>
        /// Holds the available serial ports
        /// </summary>
        public ObservableCollection<string> AvailableSerialPorts;
        #endregion

        #region Private methods

        /// <summary>
        /// Loads servo parameters file
        /// </summary>
        /// <returns></returns>
        private void LoadServoParameters()
        {
            Log("Loading servo parameters from Path: " + _servoParametersFile);
            XmlSerializer xmlProfileSerializer = new XmlSerializer(typeof(ServoConfigurationParameters));
            try
            {
                using (var reader = new FileStream(_servoParametersFile, FileMode.Open))
                {
                    _servoConfigurationParameters = (ServoConfigurationParameters) xmlProfileSerializer.Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Log("Error during loading of servo parameters file: " + e.Message);
            }
        }

        /// <summary>
        /// Loads available xml based servo profiles from applications subdirectory (servo-profiles)
        /// </summary>
        private void LoadAvailableProfiles()
        {
            Log("Loading available profiles...");

            if (Directory.Exists(_servoProfilesPath))
            {
                var foundServoProfiles = Directory.GetFiles(_servoProfilesPath, "*.xml");
                listServoProfiles.Items.Clear();
                _profileFilesAvailable = new Dictionary<string, string>();
                // TODO: Maybe read an profile name set inside the xml file instead of file name. Checks also integrity of File through Deserialisation
                foreach (var availableProfile in foundServoProfiles)
                {
                    var shortProfileName = Path.GetFileNameWithoutExtension(availableProfile);
                    _profileFilesAvailable.Add(shortProfileName, availableProfile);
                    listServoProfiles.Items.Add(shortProfileName);
                }

                Log("servo-profile path and files loaded. Path: " + _servoProfilesPath);
            }
            else
            {
                Log("Warning: Current Directory does not exist - no profiles loaded: " + _servoProfilesPath);
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
                DataBits = Settings.Default.comOptionsDataBits,
                Parity = Settings.Default.comOptionsParity,
                StopBits = Settings.Default.comOptionsStopBits,
                Speed = Settings.Default.comOptionsSpeed,
                ConnectionTimeout = Settings.Default.servoQueryTimeout
            };
            if (_modBusWrapper.Connect(tmpConfig))
            {
                // Remember current Port by writing to Settings
                Settings.Default.comOptionsLastConnectedPort = portName;
                Settings.Default.Save();
                btnConnect.IsEnabled = false;
                btnDisconnect.IsEnabled = true;
                Log("Connected");
                SearchServos();
                AddSliders();
            }
            else
            {
                Log("Error: Could not connect to port: " + portName);
            }
        }

        /// <summary>
        /// Generates Sliders Bases on the _propertySliderControlsList
        /// </summary>
        private void AddSliders()
        {

            _propertySliderControlsList = new Dictionary<int, PropertySliderControl>();
            propertySliderStackPanel.Children.Clear();

            foreach (var servoConfigurationParameter in _servoConfigurationParameters.Parameters)
            {
                var tmpSliderControl = new PropertySliderControl
                {
                    Key = servoConfigurationParameter.Key,
                    PName = servoConfigurationParameter.Name,
                    Description = servoConfigurationParameter.Description,
                    MinValue = servoConfigurationParameter.Min,
                    MaxValue = servoConfigurationParameter.Max,
                    DefaultValue = servoConfigurationParameter.Default,
                    IsEnabled = false
                };
                //tmpSliderControl.CurrentValue = servoConfigurationParameter;
                tmpSliderControl.SetValueClicked += ServoParameter_SetValueClicked;
                tmpSliderControl.DefaultValueClicked += ServoParameter_DefaultValueClicked;

                propertySliderStackPanel.Children.Add(tmpSliderControl);
                propertySliderStackPanel.Children.Add(new Separator());

                _propertySliderControlsList.Add(servoConfigurationParameter.Key, tmpSliderControl);
            }
            grpLiveDataManipulation.IsEnabled = true;
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
            btnSaveAsProfile.IsEnabled = false;
            grpLiveDataManipulation.IsEnabled = false;
            listBoxManipulationServos.ItemsSource = null;
            listBoxProfileServos.ItemsSource = null;
        }

        /// <summary>
        /// Searches for servos and provides them in AvailableServoIds
        /// </summary>
        private async void SearchServos()
        {
            Log("searching servos - please wait");

            ModBusExtensionControlGuiElement.IsEnabled = false;
            AvailableServoIds = await Task.Run((() => _modBusWrapper.SearchServos(Settings.Default.maxServoId)));
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
        /// <param name="filename"></param>
        private void BackupProfile(byte servoId, string filename)
        {
            Log("backupProfile - servo Id: " + servoId);
            var values = _modBusWrapper.ReadData(Convert.ToByte(servoId), 0, 280);
            SaveProfile(servoId, values, filename);
        }

        /// <summary>
        /// Transfers profile to one or more servos
        /// </summary>
        /// <param name="savePermanent">save settings on servo permanently</param>
        private async void TransferProfile(bool savePermanent)
        {
            Log("Transfer Profile: " + listServoProfiles.SelectedValue);

            Array servosArray = listBoxProfileServos.SelectedItems.Cast<int>().ToArray();
            var selectedProfile = _profileFilesAvailable[listServoProfiles.SelectedValue.ToString()];

            ModBusExtensionControlGuiElement.IsEnabled = false;
            await Task.Run(() => TransferProfile(selectedProfile, servosArray));
            if (savePermanent)
            {
                await Task.Run(() => SavePermanent(servosArray));
            }

            ReloadParameterDataOfSelectedServos();

            ModBusExtensionControlGuiElement.IsEnabled = true;
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
                var tmpParam = new Param() {Key = val.Key, Value = val.Value};
                tmpSet.Parameters.Add(tmpParam);
            }

            XmlSerializer xmlProfileSerializer = new XmlSerializer(typeof(ServoConfigurationProfile));

            using (var sww = new StringWriterWithEncoding(Encoding.UTF8))
            {
                using (XmlTextWriter writer = new XmlTextWriter(sww) {Formatting = Formatting.Indented})
                {
                    xmlProfileSerializer.Serialize(writer, tmpSet);
                    var xml = sww.ToString();

                    File.WriteAllText(profileName, xml);
                    Log("profile saved: " + profileName);
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
                profile = (ServoConfigurationProfile) xmlProfileSerializer.Deserialize(reader);
            }
            return profile;
        }

        /// <summary>
        /// Sets the default profiles folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSetFolder(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = _servoProfilesPath;
                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    _servoProfilesPath = dialog.SelectedPath;
                    Settings.Default.profilesPath = _servoProfilesPath;
                    Settings.Default.Save();
                    LoadAvailableProfiles();
                }

            }
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
        }

        /// <summary>
        /// Loads live Parameters from all selected servos and try to match them to the sliders
        /// If values are not equal, the according slider will be deactivated
        /// </summary>
        private void ReloadParameterDataOfSelectedServos()
        {
            Log("Live Data: Refreshing Parameters");

            // Leeres Dictionary erzeugen

            // Todo: Refactor Method from Hell :)

            Dictionary<int, List<int>> currentValues = new Dictionary<int, List<int>>();

            // Gewählte Servos identifizieren
            Array servosArray = listBoxManipulationServos.SelectedItems.Cast<int>().ToArray();

            bool disabledSlider = false;

            // Alle geladenen Parameter durchgehen
            foreach (var servoConfigurationParameter in _servoConfigurationParameters.Parameters)
            {
                if (servosArray.Length > 0)
                {
                    currentValues[servoConfigurationParameter.Key] = new List<int>();

                    // Alle gewählten Servos durchgehen
                    foreach (int servoId in servosArray)
                    {

                        // Auslesen und ablegen
                        var currentValue = _modBusWrapper.ReadValueFromServo(servoId, servoConfigurationParameter.Key);
                        currentValues[servoConfigurationParameter.Key].Add(currentValue);
                    }

                    // Direkt entsprechenden Slider Aktivieren/Deaktivieren wenn alle Values für gewählte Drives gleich sind.
                    if (currentValues[servoConfigurationParameter.Key]
                        .All(x => x == currentValues[servoConfigurationParameter.Key].First()))
                    {
                        // Enable Slider
                        try
                        {
                            _propertySliderControlsList[servoConfigurationParameter.Key].IsEnabled = true;
                            _propertySliderControlsList[servoConfigurationParameter.Key].Tag = "equal";
                            _propertySliderControlsList[servoConfigurationParameter.Key].CurrentValue =
                                currentValues[servoConfigurationParameter.Key].First();
                        }
                        catch (Exception e)
                        {
                            Log("Critical Error during setting of slider value: " + e.Message);
                            throw;
                        }

                    }
                    else
                    {
                        // Disable Slider
                        disabledSlider = true;
                        _propertySliderControlsList[servoConfigurationParameter.Key].IsEnabled = false;
                        _propertySliderControlsList[servoConfigurationParameter.Key].Tag = "non-equal";
                    }
                }
                else
                {
                    _propertySliderControlsList[servoConfigurationParameter.Key].IsEnabled = false;
                }

            }

            if (disabledSlider)
            {
                Log("Info: At least one value of selected servos is not equal. Disabled corresponding sliders.");
            }
        }

        /// <summary>
        /// Saves current Parameters permanent to EEPROM
        /// </summary>
        /// <param name="servosArray">Servos to persist parameters on</param>
        /// <returns></returns>
        private bool SavePermanent(Array servosArray)
        {
            Log("******** WRITING PARAMETERS TO EEPROM - PLEASE WAIT - TAKES 5 Seconds per SERVO ********");
                try
                {
                    if (servosArray.Length > 0)
                    {
                        foreach (int servoId in servosArray)
                        {
                            Log("Write Parameters to EEPROM of servo: " + servoId);
                            if (_modBusWrapper.PersistParametersToMemory(servoId))
                            {
                                Log("Parameters written to EEPROM of servo:" + servoId);
                            }
                            else
                            {
                                Log("Error during write of parameters to EEPROM of servo: " + servoId);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log("Exception during write of parameters to EEPROM" + exception.Message);
                }
                return true;
        }

        /// <summary>
        /// Writes all Slider parameters to selected Servos
        /// </summary>
        private void TransferAllParametersToSelectedServos()
        {
            Log("Live Data: Transfer all parameters at once");
            Dispatcher?.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    
                    foreach (var propertySlider in _propertySliderControlsList)
                    {
                        if ((string) propertySlider.Value.Tag != "non-equal")
                        {
                            Log("Live Data: Transferring Key: " + propertySlider.Value.Key + " - Value: " +
                                propertySlider.Value.CurrentValue);
                            var args = new KeyValueEventArgs()
                            {
                                Key = propertySlider.Value.Key,
                                Value = propertySlider.Value.CurrentValue
                            };
                            WriteSliderValueToSelectedServos(propertySlider.Value, args);
                        }
                        else
                        {
                            Log("Live Data: Omitting non-equal Key: " + propertySlider.Value.Key + " - Value: " +
                                propertySlider.Value.CurrentValue);
                        }
                    }
                }));
        }

        #endregion

        #region Private helpers

        /// <summary>
        /// Returns the servo profiles path
        /// </summary>
        /// <returns>Path of profiles</returns>
        private string GetServoProfilesPath()
        {
            string profileDirectory;

            if (Settings.Default.profilesPath != "")
            {
                profileDirectory = Settings.Default.profilesPath;
            }
            else
            {

                profileDirectory = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ??
                    throw new ArgumentNullException(
                        $"Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)"),
                    "servo-profiles");
                


                if (!Directory.Exists(profileDirectory)) { 
                    Log("servo-profile path not available - try to create path: " + _servoProfilesPath);
                    try
                    {
                        Directory.CreateDirectory(_servoProfilesPath);
                        Log("servo-profile path created");
                    }
                    catch (Exception ex)
                    {
                        Log("Error during creation of Servo profiles directory. " + ex.Message);
                    }
                }
                Settings.Default.profilesPath = profileDirectory;
                Settings.Default.Save();

            }
            return profileDirectory;
        }

        /// <summary>
        /// Returns the path to servo properties file
        /// </summary>
        /// <returns></returns>
        private string getServoParametersPath()
        {
            var currentDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException(), "servo-parameters", "sfx-100-parameters.xml");
            return currentDirectory;
        }

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
                    if (debugBox.Items.Count >= 250)
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
        /// Eventhandler called when controlis loaded.
        /// Loads profiles, serial ports and tries to connect automatically - if set.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModBusExtensionControlGUI_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadAvailableProfiles();
            LoadServoParameters();

            try
            {
                LoadAvailableSerialPorts();
                comOptionsPort.SelectedValue = Settings.Default.comOptionsLastConnectedPort;

                if (Settings.Default.comOptionsAutoConnectOnStartup && comOptionsPort.SelectedValue != null)
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

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = DateTime.Now.ToString("yyyyMMddHHmm") + "-" + selectedServo + "-backup",
                DefaultExt = ".xml",
                Filter = "SFX-100 servo profiles (.xml)|*.xml",
                InitialDirectory = _servoProfilesPath,
                RestoreDirectory = false
                
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                // Save document
                string filename = dlg.FileName;
                ModBusExtensionControlGuiElement.IsEnabled = false;
                await Task.Run(() => BackupProfile(selectedServo, filename));
                LoadAvailableProfiles();
                ModBusExtensionControlGuiElement.IsEnabled = true;
            }


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
        /// Eventhandler for click on parameters refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnParametersRefresh(object sender, RoutedEventArgs e)
        {
            ReloadParameterDataOfSelectedServos();
        }

        /// <summary>
        /// Eventhandler for click on transfer profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnTransferProfile(object sender, RoutedEventArgs e)
        {
            TransferProfile(false);
        }

        /// <summary>
        /// Eventhandler for click on transfer and save profile permanently
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnTransferProfilePermanently(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Do you really want to save the profile to selected servos?", "Permanent save", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                TransferProfile(true);
            }
        }

        /// <summary>
        /// Eventhandler called when manipulation servos listbox changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBoxManipulationServos_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ReadCurrentRpmOfSelectedServos();
            ReloadParameterDataOfSelectedServos();

        }

        /// <summary>
        /// Writes a slider value to selected servos
        /// </summary>
        /// <param name="currentSlider"></param>
        /// <param name="e"></param>
        private void WriteSliderValueToSelectedServos(PropertySliderControl currentSlider, KeyValueEventArgs e)
        {
            // Todo: No KeyValueEvents args needed here, since currentValue holds e.Value and Key is available too
            if (e.Value >= currentSlider.MinValue && e.Value <= currentSlider.MaxValue)
            {
                foreach (int servoId in listBoxManipulationServos.SelectedItems)
                {
                    if (_modBusWrapper.WriteValueToServo(servoId, e.Key, e.Value))
                    {
                        Log("Slider Key: " + e.Key + " with Value: " + e.Value + " written to servo: " + servoId);
                    }
                    else
                    {
                        Log("Error: Slider Key: " + e.Key + " with Value: " + e.Value + " not written to servo: " + servoId);
                    }
                    
                }
            }
            else
            {
                Log("Invalid Value: " + e.Value + " - Allowed Min/Max: " + currentSlider.MinValue + " - " + currentSlider.MaxValue);
            }
        }

        /// <summary>
        /// Eventhandler launched when click set on an property slider control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServoParameter_SetValueClicked(object sender, KeyValueEventArgs e) {
            var sliderControl = sender as PropertySliderControl;
            if (sliderControl != null)
            {
                Log("Clicked set value: " + sliderControl.PName + "(" + sliderControl.Key + ") -- " + e.Value);
                WriteSliderValueToSelectedServos(sliderControl, e);
            }
        }

        /// <summary>
        /// Eventhandler launched when click reset on an property slider control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServoParameter_DefaultValueClicked(object sender, KeyValueEventArgs e)
        {
            var sliderControl = sender as PropertySliderControl;
            if (sliderControl != null)
            {
                Log("Clicked reset value: " + sliderControl.PName + "(" + sliderControl.Key + ") -- " + e.Value);
                WriteSliderValueToSelectedServos(sliderControl, e);
            }
        }


        /// <summary>
        /// Disconnect when UI is unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModBusExtensionControlGUI_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Disconnect();
            Settings.Default.Save();
        }

        /// <summary>
        /// Eventhandler launched when Project URI is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        /// <summary>
        /// Eventhandler launched when clicking on save permanent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnSavePermanent_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Do you really want to save current values to selected servos?", "Permanent save", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Array servosArray = listBoxManipulationServos.SelectedItems.Cast<int>().ToArray();
                ModBusExtensionControlGuiElement.IsEnabled = false;
                await Task.Run((() => SavePermanent(servosArray)));
                ReloadParameterDataOfSelectedServos();
                ModBusExtensionControlGuiElement.IsEnabled = true;
            }
        }

        /// <summary>
        /// Enable save as profile box when servo is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbBoxBackupServo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSaveAsProfile.IsEnabled = Convert.ToInt32(cmbBoxBackupServo.SelectedValue) >= 1;
        }

        /// <summary>
        /// Eventhandler called when clicking "Transfer all at once"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnTransferAllAtOnce_OnClick(object sender, RoutedEventArgs e)
        {
            ModBusExtensionControlGuiElement.IsEnabled = false;
            await Task.Run(() => TransferAllParametersToSelectedServos());
            ReloadParameterDataOfSelectedServos();
            ModBusExtensionControlGuiElement.IsEnabled = true;
        }

        #endregion

    }
}