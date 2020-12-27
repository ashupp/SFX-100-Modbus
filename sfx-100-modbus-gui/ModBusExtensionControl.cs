using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using sfx_100_modbus_lib;


namespace sfx_100_modbus_sfb_extension
{
    public partial class ModBusExtensionControl : UserControl
    {
        private bool isStarted = false;


        private ModBusWrapper _modBusWrapper = new ModBusWrapper();

        public ModBusExtensionControl()
        {

        }


        public void Start()
        {
            isStarted = true;
            //throw new NotImplementedException();
        }

        public void Stop()
        {
            if (!isStarted) return;
            isStarted = false;
            //throw new NotImplementedException();
        }

        private void ParentForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                StartStopToggle();
            }
        }

        public void StartStopToggle()
        {
            if (isStarted)
            {
                isStarted = false;
                Stop();
            }
            else
            {
                isStarted = true;
                Start();
            }
        }

        private void ModBusExtensionControl_Load(object sender, EventArgs e)
        {
            if (ParentForm != null)
            {
                ParentForm.FormClosing += ParentForm_FormClosing;
                this.ParentForm.KeyDown += ParentForm_KeyDown;
                ParentForm.KeyPreview = true;
            }
        }



        private void ParentForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }
    }
}
