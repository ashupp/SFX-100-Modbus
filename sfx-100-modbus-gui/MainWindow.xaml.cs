using sfx_100_modbus_lib;
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

        #region Main entry
        /// <summary>
        /// Main Entry
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Title = Title + " - " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            DataContext = this;
        }

        #endregion



        #region Private helpers

        #endregion

        #region Eventhandlers

        /// <summary>
        /// Eventhandler called when main window is loaded.
        /// Loads profiles, serial ports and tries to connect automatically - if set.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        { }




        /// <summary>
        /// Eventhandle called when Main window is closing.
        /// Disconnects from ModBus if connected and saves profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();
        }

        #endregion
    }
}
