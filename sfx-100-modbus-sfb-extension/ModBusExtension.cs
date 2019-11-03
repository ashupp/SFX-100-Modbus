using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimFeedback.extension;
using System.Windows.Forms;

namespace sfx_100_modbus_sfb_extension
{
    public class ModBusExtension : AbstractSimFeedbackExtension
    {
        private ModBusExtensionControl extCtrl;
        public ModBusExtensionConfig extCfg;

        public ModBusExtension()
        {
            Name = "ModBus Extension";
            Info = "Directly control and query servo driver through ModBus/RS485";
            Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Author = "ashupp";
            NeedsOwnTab = false;
            HasControl = true;
        }

        public override void Init(SimFeedbackExtensionFacade facade, ExtensionConfig config)
        {
            base.Init(facade, config);
            Log("Initialize ModBus Extension");

            LogDebug("ModBusExtension: Reading Config");
            extCfg = (ModBusExtensionConfig)config.CustomConfig;
            if (extCfg == null)
            {
                LogDebug("FanatecExtension: No Config found, creating new config");
            }
            config.CustomConfig = extCfg;

            facade.SaveConfig();

            extCtrl = new ModBusExtensionControl(this, facade);
        }

        public void SetIsRunning(bool status)
        {
            IsRunning = status;
        }

        public override void Start()
        {
            if (IsRunning) return;
            SimFeedbackFacade.Log("Starting ModBus Extension");
            extCtrl.Start();
            IsRunning = true;
        }

        public override void Stop()
        {
            if (!IsRunning) return;
            Log("Stopping ModBus Extension");
            extCtrl.Stop();
            IsRunning = false;
        }

        public override Control ExtensionControl => extCtrl;
    }
}