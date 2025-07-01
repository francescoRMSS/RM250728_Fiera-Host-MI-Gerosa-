namespace RM.src.RM250619
{
    partial class FormHomePage
    {
        /// <summary>
        /// Variabile di progettazione necessaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Pulire le risorse in uso.
        /// </summary>
        /// <param name="disposing">ha valore true se le risorse gestite devono essere eliminate, false in caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Codice generato da Progettazione Windows Form

        /// <summary>
        /// Metodo necessario per il supporto della finestra di progettazione. Non modificare
        /// il contenuto del metodo con l'editor di codice.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormHomePage));
            this.pnl_pageContainer = new System.Windows.Forms.Panel();
            this.timer_dateTime_clock = new System.Windows.Forms.Timer(this.components);
            this.pnl_header = new RMLib.View.CustomPanel();
            this.pnl_safeZone = new System.Windows.Forms.Panel();
            this.Pnl_ROBOT_alarm = new System.Windows.Forms.Panel();
            this.Pnl_PLC_alarm = new System.Windows.Forms.Panel();
            this.Pnl_diagnostics = new System.Windows.Forms.Panel();
            this.pnl_ActiveAlarms = new System.Windows.Forms.Panel();
            this.PnlLogoContainer = new RMLib.View.CustomPanel();
            this.Pnl_logo_rm = new RMLib.View.CustomPanel();
            this.Pnl_MOVROBOT_alarm = new System.Windows.Forms.Panel();
            this.lbl_pageTitle = new System.Windows.Forms.Label();
            this.lbl_dateTime = new System.Windows.Forms.Label();
            this.pnl_header.SuspendLayout();
            this.PnlLogoContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnl_pageContainer
            // 
            this.pnl_pageContainer.BackColor = System.Drawing.Color.Transparent;
            this.pnl_pageContainer.Location = new System.Drawing.Point(0, 110);
            this.pnl_pageContainer.Name = "pnl_pageContainer";
            this.pnl_pageContainer.Size = new System.Drawing.Size(1024, 658);
            this.pnl_pageContainer.TabIndex = 1;
            // 
            // pnl_header
            // 
            this.pnl_header.BackColor = System.Drawing.Color.RoyalBlue;
            this.pnl_header.BackgroundColor = System.Drawing.Color.RoyalBlue;
            this.pnl_header.BorderColor = System.Drawing.Color.Black;
            this.pnl_header.BorderRadius = 0;
            this.pnl_header.BorderSize = 0;
            this.pnl_header.Controls.Add(this.pnl_safeZone);
            this.pnl_header.Controls.Add(this.Pnl_ROBOT_alarm);
            this.pnl_header.Controls.Add(this.Pnl_PLC_alarm);
            this.pnl_header.Controls.Add(this.Pnl_diagnostics);
            this.pnl_header.Controls.Add(this.pnl_ActiveAlarms);
            this.pnl_header.Controls.Add(this.PnlLogoContainer);
            this.pnl_header.Controls.Add(this.Pnl_MOVROBOT_alarm);
            this.pnl_header.Controls.Add(this.lbl_pageTitle);
            this.pnl_header.Controls.Add(this.lbl_dateTime);
            this.pnl_header.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.pnl_header.ForeColor = System.Drawing.Color.White;
            this.pnl_header.Location = new System.Drawing.Point(-4, -20);
            this.pnl_header.Margin = new System.Windows.Forms.Padding(0);
            this.pnl_header.Name = "pnl_header";
            this.pnl_header.Size = new System.Drawing.Size(1030, 120);
            this.pnl_header.TabIndex = 0;
            this.pnl_header.TextColor = System.Drawing.Color.White;
            // 
            // pnl_safeZone
            // 
            this.pnl_safeZone.BackColor = System.Drawing.Color.Transparent;
            this.pnl_safeZone.BackgroundImage = global::RM.Properties.Resources.safeZone_green32;
            this.pnl_safeZone.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pnl_safeZone.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnl_safeZone.Location = new System.Drawing.Point(731, 34);
            this.pnl_safeZone.Name = "pnl_safeZone";
            this.pnl_safeZone.Size = new System.Drawing.Size(40, 40);
            this.pnl_safeZone.TabIndex = 28;
            this.pnl_safeZone.Visible = false;
            // 
            // Pnl_ROBOT_alarm
            // 
            this.Pnl_ROBOT_alarm.BackColor = System.Drawing.Color.Transparent;
            this.Pnl_ROBOT_alarm.BackgroundImage = global::RM.Properties.Resources.robot_arm_connection;
            this.Pnl_ROBOT_alarm.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Pnl_ROBOT_alarm.Location = new System.Drawing.Point(959, 34);
            this.Pnl_ROBOT_alarm.Name = "Pnl_ROBOT_alarm";
            this.Pnl_ROBOT_alarm.Size = new System.Drawing.Size(40, 40);
            this.Pnl_ROBOT_alarm.TabIndex = 25;
            // 
            // Pnl_PLC_alarm
            // 
            this.Pnl_PLC_alarm.BackColor = System.Drawing.Color.Transparent;
            this.Pnl_PLC_alarm.BackgroundImage = global::RM.Properties.Resources.connection_icon;
            this.Pnl_PLC_alarm.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Pnl_PLC_alarm.Location = new System.Drawing.Point(914, 34);
            this.Pnl_PLC_alarm.Name = "Pnl_PLC_alarm";
            this.Pnl_PLC_alarm.Size = new System.Drawing.Size(40, 40);
            this.Pnl_PLC_alarm.TabIndex = 24;
            // 
            // Pnl_diagnostics
            // 
            this.Pnl_diagnostics.BackColor = System.Drawing.Color.Transparent;
            this.Pnl_diagnostics.BackgroundImage = global::RM.Properties.Resources.diagnostic;
            this.Pnl_diagnostics.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Pnl_diagnostics.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Pnl_diagnostics.Location = new System.Drawing.Point(778, 34);
            this.Pnl_diagnostics.Name = "Pnl_diagnostics";
            this.Pnl_diagnostics.Size = new System.Drawing.Size(40, 40);
            this.Pnl_diagnostics.TabIndex = 26;
            this.Pnl_diagnostics.Click += new System.EventHandler(this.Pnl_diagnostics_Click);
            // 
            // pnl_ActiveAlarms
            // 
            this.pnl_ActiveAlarms.BackColor = System.Drawing.Color.Transparent;
            this.pnl_ActiveAlarms.BackgroundImage = global::RM.Properties.Resources.alarm_popup_grey;
            this.pnl_ActiveAlarms.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pnl_ActiveAlarms.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pnl_ActiveAlarms.Location = new System.Drawing.Point(869, 34);
            this.pnl_ActiveAlarms.Name = "pnl_ActiveAlarms";
            this.pnl_ActiveAlarms.Size = new System.Drawing.Size(40, 40);
            this.pnl_ActiveAlarms.TabIndex = 25;
            this.pnl_ActiveAlarms.Click += new System.EventHandler(this.ClickEvent_alarms);
            // 
            // PnlLogoContainer
            // 
            this.PnlLogoContainer.BackColor = System.Drawing.Color.White;
            this.PnlLogoContainer.BackgroundColor = System.Drawing.Color.White;
            this.PnlLogoContainer.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.PnlLogoContainer.BorderColor = System.Drawing.Color.Black;
            this.PnlLogoContainer.BorderRadius = 15;
            this.PnlLogoContainer.BorderSize = 0;
            this.PnlLogoContainer.Controls.Add(this.Pnl_logo_rm);
            this.PnlLogoContainer.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PnlLogoContainer.ForeColor = System.Drawing.Color.White;
            this.PnlLogoContainer.Location = new System.Drawing.Point(20, 42);
            this.PnlLogoContainer.Margin = new System.Windows.Forms.Padding(0);
            this.PnlLogoContainer.Name = "PnlLogoContainer";
            this.PnlLogoContainer.Size = new System.Drawing.Size(208, 54);
            this.PnlLogoContainer.TabIndex = 28;
            this.PnlLogoContainer.TextColor = System.Drawing.Color.White;
            // 
            // Pnl_logo_rm
            // 
            this.Pnl_logo_rm.BackColor = System.Drawing.Color.White;
            this.Pnl_logo_rm.BackgroundColor = System.Drawing.Color.White;
            this.Pnl_logo_rm.BackgroundImage = global::RM.Properties.Resources.RM_logo;
            this.Pnl_logo_rm.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Pnl_logo_rm.BorderColor = System.Drawing.Color.Black;
            this.Pnl_logo_rm.BorderRadius = 0;
            this.Pnl_logo_rm.BorderSize = 0;
            this.Pnl_logo_rm.ForeColor = System.Drawing.Color.White;
            this.Pnl_logo_rm.Location = new System.Drawing.Point(14, 3);
            this.Pnl_logo_rm.Name = "Pnl_logo_rm";
            this.Pnl_logo_rm.Size = new System.Drawing.Size(180, 49);
            this.Pnl_logo_rm.TabIndex = 29;
            this.Pnl_logo_rm.TextColor = System.Drawing.Color.White;
            this.Pnl_logo_rm.DoubleClick += new System.EventHandler(this.DoubleClickEvent_showSwVersion);
            // 
            // Pnl_MOVROBOT_alarm
            // 
            this.Pnl_MOVROBOT_alarm.BackColor = System.Drawing.Color.Transparent;
            this.Pnl_MOVROBOT_alarm.BackgroundImage = global::RM.Properties.Resources.noMov;
            this.Pnl_MOVROBOT_alarm.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Pnl_MOVROBOT_alarm.Location = new System.Drawing.Point(824, 34);
            this.Pnl_MOVROBOT_alarm.Name = "Pnl_MOVROBOT_alarm";
            this.Pnl_MOVROBOT_alarm.Size = new System.Drawing.Size(40, 40);
            this.Pnl_MOVROBOT_alarm.TabIndex = 26;
            // 
            // lbl_pageTitle
            // 
            this.lbl_pageTitle.BackColor = System.Drawing.Color.Transparent;
            this.lbl_pageTitle.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_pageTitle.Location = new System.Drawing.Point(-2, 74);
            this.lbl_pageTitle.Name = "lbl_pageTitle";
            this.lbl_pageTitle.Size = new System.Drawing.Size(1030, 34);
            this.lbl_pageTitle.TabIndex = 25;
            this.lbl_pageTitle.Text = "HOMEPAGE";
            this.lbl_pageTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbl_dateTime
            // 
            this.lbl_dateTime.BackColor = System.Drawing.Color.Transparent;
            this.lbl_dateTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_dateTime.Location = new System.Drawing.Point(298, 29);
            this.lbl_dateTime.Name = "lbl_dateTime";
            this.lbl_dateTime.Size = new System.Drawing.Size(427, 25);
            this.lbl_dateTime.TabIndex = 4;
            this.lbl_dateTime.Text = "12:34:56";
            this.lbl_dateTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FormHomePage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.CornflowerBlue;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(1024, 767);
            this.Controls.Add(this.pnl_pageContainer);
            this.Controls.Add(this.pnl_header);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormHomePage";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ClosingEvent_homePageClosing);
            this.Load += new System.EventHandler(this.FormHomePage_Load);
            this.Shown += new System.EventHandler(this.FormHomePage_Shown);
            this.pnl_header.ResumeLayout(false);
            this.PnlLogoContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private global::RMLib.View.CustomPanel pnl_header;
        private System.Windows.Forms.Panel pnl_pageContainer;
        private System.Windows.Forms.Label lbl_dateTime;
        private System.Windows.Forms.Timer timer_dateTime_clock;
        private System.Windows.Forms.Panel Pnl_ROBOT_alarm;
        private System.Windows.Forms.Panel Pnl_PLC_alarm;
        private System.Windows.Forms.Label lbl_pageTitle;
        private System.Windows.Forms.Panel Pnl_MOVROBOT_alarm;
        private System.Windows.Forms.Panel pnl_ActiveAlarms;
        private RMLib.View.CustomPanel Pnl_logo_rm;
        private RMLib.View.CustomPanel PnlLogoContainer;
        private System.Windows.Forms.Panel Pnl_diagnostics;
        private System.Windows.Forms.Panel pnl_safeZone;
    }
}

