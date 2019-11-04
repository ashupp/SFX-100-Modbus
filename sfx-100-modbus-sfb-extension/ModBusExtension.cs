using SimFeedback.extension;
using System.Windows.Forms;

namespace sfx_100_modbus_sfb_extension
{
    public class ModBusExtension : AbstractSimFeedbackExtension
    {
        private ModBusExtensionControl _extCtrl;

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
            _extCtrl = new ModBusExtensionControl(this, facade);
        }

        public void SetIsRunning(bool status)
        {
            IsRunning = status;
        }

        public override void Start()
        {
            if (IsRunning) return;
            SimFeedbackFacade.Log("Starting ModBus Extension");
            _extCtrl.Start();
            IsRunning = true;
        }

        public override void Stop()
        {
            if (!IsRunning) return;
            Log("Stopping ModBus Extension");
            _extCtrl.Stop();
            IsRunning = false;
        }

        public override Control ExtensionControl => _extCtrl;
    }
}