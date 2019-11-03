namespace sfx_100_modbus_sfb_extension
{
    partial class ModBusExtensionControl
    {
        /// <summary> 
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.elementHost = new System.Windows.Forms.Integration.ElementHost();
            this.modBusExtensionControlGUI1 = new sfx_100_modbus_sfb_extension.ModBusExtensionControlGUI();
            this.SuspendLayout();
            // 
            // elementHost
            // 
            this.elementHost.AutoSize = true;
            this.elementHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost.Location = new System.Drawing.Point(0, 0);
            this.elementHost.Name = "elementHost";
            this.elementHost.Size = new System.Drawing.Size(1186, 814);
            this.elementHost.TabIndex = 1;
            this.elementHost.Text = "elementHost1";
            this.elementHost.ChildChanged += new System.EventHandler<System.Windows.Forms.Integration.ChildChangedEventArgs>(this.elementHost_ChildChanged);
            this.elementHost.Child = this.modBusExtensionControlGUI1;
            // 
            // ModBusExtensionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.elementHost);
            this.Name = "ModBusExtensionControl";
            this.Size = new System.Drawing.Size(1186, 814);
            this.Load += new System.EventHandler(this.ModBusExtensionControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Integration.ElementHost elementHost;
        private ModBusExtensionControlGUI modBusExtensionControlGUI1;
    }
}
