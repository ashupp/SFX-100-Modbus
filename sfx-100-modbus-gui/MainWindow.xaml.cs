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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;
using Path = System.IO.Path;

namespace sfx_100_modbus_gui
{
    public class ViewModel
    {
        public ObservableCollection<int> AvailableServoIDs { get; } = new ObservableCollection<int>
        {
            1,2,3,4
        };
    }

    class Item
    {
        public string Name { get; set; }
        public bool Selected { get; set; }
    }

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, string> _profileFilesAvailable;
        private ModBusWrapper _modBusWrapper = new ModBusWrapper();
        public List<int> AvailableServoIDs = new List<int>();

        public MainWindow()
        {
            InitializeComponent();
            log("startup");
            InitializeApp();
        }

        private void InitializeApp()
        {
            loadAvailablePorts();
            loadAvailableProfiles();

            /*listBoxPrepareServos.Items.Clear();
            listBoxPrepareServos.ItemsSource = AvailableServoIDs;*/
        }

        /// <summary>
        /// Loads available xml based servo profiles from applications subdirectory (servo-profiles)
        /// </summary>
        private void loadAvailableProfiles()
        {
            log("loadAvailableProfiles");

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

                log("servo-profile path and files loaded");
            }
            else
            {
                log("servo-profile path not available - try to create path: " + servoProfilesPath);
                try
                {
                    Directory.CreateDirectory(servoProfilesPath);
                    log("servo-profile path created");
                }
                catch (Exception ex)
                {
                    log("Error during creation of Servo profiles directory. " + ex.Message);
                }
            }

        }

        private void backupAsProfile(string servoID)
        {
            log("backupAsProfile - servo ID: " + servoID);
            var values = _modBusWrapper.ReadData(Convert.ToByte(servoID), 0, 280);
            saveAsProfile(servoID, values, DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + servoID + "-backup");
        }

        private void saveAsProfile(string servoID, Dictionary<int,int> values, string ProfileName = "", string Author = "", string Info = "")
        {
            ServoConfigurationProfile tmpSet = new ServoConfigurationProfile();

            tmpSet.Created = DateTime.Now;
            tmpSet.Author = Author;
            tmpSet.Name = ProfileName;
            tmpSet.Info = Info;
            tmpSet.Parameters = new List<Param>();

            foreach (var val in values)
            {
                var tmpParam = new Param() {key = val.Key.ToString(), value = val.Value.ToString()};
                tmpSet.Parameters.Add(tmpParam);
            }

            XmlSerializer xsSubmit = new XmlSerializer(typeof(ServoConfigurationProfile));
            var xml = "";

            using (var sww = new StringWriter())
            {
                using (XmlTextWriter writer = new XmlTextWriter(sww) { Formatting = Formatting.Indented })
                {
                    xsSubmit.Serialize(writer, tmpSet);
                    xml = sww.ToString(); // Your XML

                    var profilePath = Path.Combine(Directory.GetCurrentDirectory(), "servo-profiles", ProfileName + ".xml");
                    File.WriteAllText(profilePath, xml);
                    log("profile saved: " + profilePath);
                }
            }
            
        }

        private void loadAvailablePorts()
        {
            // Handled in WPF
        }

        private void log(object logEntry)
        {
            if (debugBox.Items.Count >= 36)
            {
                debugBox.Items.RemoveAt(0);
            }

            debugBox.Items.Add(DateTime.Now + ": " + logEntry);
            debugBox.SelectedIndex = debugBox.Items.Count - 1;
            debugBox.ScrollIntoView(debugBox.SelectedItem);
        }

        private void btnConnect(object sender, RoutedEventArgs e)
        {
            log("connect");
            ModBusConfiguration tmpConfig = new ModBusConfiguration
            {
                // Todo: Maybe pass plain values here and take care of conversion in wrapper
                PortName = comOptionsPort.SelectedValue.ToString(),
                DataBits = Convert.ToInt32(comOptionsDataBits.Text),
                Parity = (Parity) Enum.Parse(typeof(Parity), comOptionsParity.Text),
                StopBits = (StopBits) Enum.Parse(typeof(StopBits), comOptionsStopBit.Text),
                Speed = Convert.ToInt32(comOptionsSpeed.Text)
            };
            _modBusWrapper.Connect(tmpConfig);

        }

        private void btnDisconnect(object sender, RoutedEventArgs e)
        {
            log("disconnect");
            _modBusWrapper.Disconnect();
        }

        private void btnSearchServos(object sender, RoutedEventArgs e)
        {
            log("search servos");
            AvailableServoIDs = _modBusWrapper.SearchServos();
        }

        private void btnCheckDoubleIds(object sender, RoutedEventArgs e)
        {
            log("check for double servo IDs");
        }

        private void btnTransferProfile(object sender, RoutedEventArgs e)
        {
            if (listServoProfiles.SelectedValue != null)
            {
                log("transfer Profile: " + listServoProfiles.SelectedValue);
                transferProfile(this._profileFilesAvailable[listServoProfiles.SelectedValue.ToString()]);
            }
            else
            {
                log("error: no profile selected");
            }
        }

        private void transferProfile(string profile)
        {
            //throw new NotImplementedException();

        }

        private void btnPrepareServos(object sender, RoutedEventArgs e)
        {
            log("prepareServos");
        }

        private void btnProfilesRefresh(object sender, RoutedEventArgs e)
        {
            loadAvailableProfiles();
        }

        private void btnBackupAsProfile(object sender, RoutedEventArgs e)
        {
            backupAsProfile(cmbBackUpServo.SelectedValue.ToString());
        }
    }
}
