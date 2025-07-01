using System;
using System.Windows.Forms;
using System.Drawing;
using RMLib.Alarms;
using RM.Properties;
using System.Diagnostics;
using RMLib.Logger;
using RMLib.PLC;
using System.Threading.Tasks;
using RMLib.Versions;
using RMLib.Keyboards;
using RMLib.MessageBox;
using RMLib.Translations;
using RMLib.VATView;
using RMLib.View;
using RMLib.Security;
using System.Collections.Generic;
using RM.src.RM250311.Forms.ScreenSaver;

namespace RM.src.RM250311
{
    /// <summary>
    /// Definisce la struttura, il comportamento e la UI della form principale da cui si può arrivare a tutte le altre funzionalità. Non va mai chiusa
    /// piuttosto, per cambiare schermata bisogna cambiare il pannello (User control o UC). Per aprire altre pagine invece basta aprire una nuova
    /// form sopra di questa possibilmente come dialog.
    /// <br>Impostare _obj per usare poi la variabile di istanza così che gli UC possano accedervi come se fosse una variabile statica</br>
    /// </summary> 
    public partial class FormHomePage : Form
    {
        #region Variabili d'istanza
        static FormHomePage _obj;

        /// <summary>
        /// Definisce una istanza statica per la classe
        /// </summary>
        public static FormHomePage Instance
        {
            get
            {
                if (_obj == null) _obj = new FormHomePage();
                return _obj;
            }
        }

        /// <summary>
        /// Restituisce il riferimento alla form screen saver
        /// </summary>
        public ScreenSaverManager ScreenSaverManagerForm
        {
            get { return screenSaverManager; }
        }

        /// <summary>
        /// Definisce una variabile per settare e ottenere la pagina corrente della form
        /// </summary>
        public Panel PnlContainer
        {
            get { return pnl_pageContainer; }
            set { pnl_pageContainer = value; }
        }

        /// <summary>
        /// Definisce una variabile per settare ed ottenere il nome della pagina corrente della form
        /// </summary>
        public string LabelHeader
        {
            get { return lbl_pageTitle.Text; }
            set { lbl_pageTitle.Text = value; } // Lbl_title.Font = ProjectVariables.FontHeader;
        }

        /// <summary>
        /// Definisce una variabile per settare ed ottenere l'allarme del PLC
        /// </summary>
        public Panel PlcBlinkPanel
        {
            get { return Pnl_PLC_alarm; }
            set { Pnl_PLC_alarm = value; }
        }

        /// <summary>
        /// Definisce una variabile per settare ed ottenere l'allarme della funzionalità
        /// </summary>
        public Panel RobotBlinkPanel
        {
            get { return Pnl_ROBOT_alarm; }
            set { Pnl_ROBOT_alarm = value; }
        }

        /// <summary>
        /// Definisce una variabile per settare ed ottenere l'allarme del robot in movimento
        /// </summary>
        public Panel RobotMovPanel
        {
            get { return Pnl_MOVROBOT_alarm; }
            set { Pnl_MOVROBOT_alarm = value; }
        }

        /// <summary>
        /// Definisce una variabile per settare ed ottenere safeZone
        /// </summary>
        public Panel RobotSafeZone
        {
            get { return pnl_safeZone; }
            set { pnl_safeZone = value; }
        }
        #endregion

        #region Proprietà di FormHomePage

        /// <summary>
        /// Logger
        /// </summary>
        private static readonly log4net.ILog log = LogHelper.GetLogger();

        /// <summary>
        /// Serve per cambiare il colore di sfondo
        /// </summary>
        public Color ChangeBackColor
        {
            get { return BackColor; }
            set { BackColor = value; }
        }

        /// <summary>
        /// Istanza dell'oggetto BlinkManager
        /// </summary>
        readonly private BlinkManager blinkMgr;

        /// <summary>
        /// A true quando ausiliari connessi
        /// </summary>
        private bool emergencyOK = false;

        private ScreenSaverManager screenSaverManager;

        #endregion

        /// <summary>
        /// Costruisce la form di homepage
        /// </summary>
        public FormHomePage()
        {
            InitializeComponent();

            EnterFullScreenMode();
            CheckForIllegalCrossThreadCalls = false;

            // Avvio timer per la data
            timer_dateTime_clock.Tick += new EventHandler(Update_lbl_dateTime_clock);
            timer_dateTime_clock.Start();

            Translate();
            InitFont();

            blinkMgr = new BlinkManager(PlcBlinkPanel, RobotBlinkPanel, RobotMovPanel, Resources.plc_connection_ok,
                Resources.connection_error, Resources.robot_alarm_ok, Resources.robot_alarm_error, Resources.noMov, Resources.inMov);
            blinkMgr.StartBlinking();

            // Iscrizione al metodo OnAllarmeGenerato quando generato evento AllarmeGenerato
            RobotManager.AllarmeGenerato += OnAllarmeGenerato;

            // Iscrizione al metodo OnAllarmeResettato quando generato evento AllarmeResettato
            RobotManager.AllarmeResettato += OnAllarmeResettato;

            ScreenSaverManager.AutoAddClickEvents(this);
        }

        #region Metodi di FormHomePage

        /// <summary>
        /// Entra in modalità full screen
        /// </summary>
        private void EnterFullScreenMode()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.Bounds = Screen.PrimaryScreen.Bounds; // Imposta i confini della finestra sui confini dello schermo
        }

        /// <summary>
        /// Metodo che mette sfondo pannello allarme in rosso
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAllarmeGenerato(object sender, EventArgs e)
        {
            // Cambia lo sfondo del pannello
            pnl_ActiveAlarms.BackgroundImage = Resources.alarm_popup_red;
        }

        /// <summary>
        /// Metodo che mette sfondo pannello allarme in grigio
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAllarmeResettato(object sender, EventArgs e)
        {
            // Cambia lo sfondo del pannello
            pnl_ActiveAlarms.BackgroundImage = Resources.alarm_popup_grey;
        }

        /// <summary>
        /// (TODO) Traduzione della pagina 
        /// </summary>
        private void Translate()
        {

        }

        /// <summary>
        /// (TODO) Set del font della pagina
        /// </summary>
        private void InitFont()
        {

        }

        #endregion

        #region Eventi di FormHomePage

        /// <summary>
        /// Aggiorna e stampa orario attuale
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Update_lbl_dateTime_clock(object sender, EventArgs e)
        {
            lbl_dateTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        /// <summary>
        /// Caricamento della home page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormHomePage_Load(object sender, EventArgs e)
        {
            _obj = this;

            UC_HomePage HomePage = new UC_HomePage
            {
                Dock = DockStyle.Fill
            };

            Instance.PnlContainer.Controls.Add(HomePage);
        }

        /// <summary>
        /// Evento di visualizzazione della home page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormHomePage_Shown(object sender, EventArgs e)
        {
            // Notifica l'alarmManager che la form è stata caricata e quindi è possibile procedere con la gestione degli allarmi 
            AlarmManager.isFormReady = true;

            //Configurazione screen saver manager - 5m
            screenSaverManager = new ScreenSaverManager(300000, "screenSaver.mp4");
        }

        /// <summary>
        /// Apertura pagina allarmi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_alarms(object sender, EventArgs e)
        {
            AlarmManager.OpenAlarmFormPage(RobotManager.formAlarmPage);
        }

        /// <summary>
        /// Visualizzazione versione delle librerie utilizzate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoubleClickEvent_showSwVersion(object sender, EventArgs e)
        {
            Dictionary<string, string> versions = new Dictionary<string, string>
            {
                { "Project", "RM250311 - Robot fiera lamiera CEMSA" },
                //{ "Hmi", "2024/12/05 - V1.0" },
                { "Software", "2025/01/29 - V0.1 - DEMO" },
                { "Alarms", AlarmManager.Version },
                { "DataAccess", RMLib.DataAccess.SqlConnectionConfiguration.DataAccessManager.Version },
                { "Environment", RMLib.Environment.Environment.Version },
                { "Keyboards", VK_Manager.Version },
                { "Logger", LogHelper.Version },
                { "MessageBox", CustomMessageBox.Version },
                { "Plc", PLCConfig.Version },
                //{ "Recipes", RecipeConfig.Version },
                { "Security", SecurityManager.Version },
                { "Translations", TranslationManager.Version },
                { "Utils", RMLib.Utils.ProjectVariables.Version },
                { "VatView", VATViewManager.Version },
                { "Versions", VersionManager.Version },
                { "View", CustomViewManager.Version }
            };

            VersionManager.ShowVersions(versions);
        }

        /// <summary>
        /// Evento generato alla chiusura dell'app, termina tutti i thread in modo non-safe e distrugge tutti gli elementi subito.
        /// Metodo aggressivo per la chiusura che risolve il problema dei thread che rimangono in background impedendo la 
        /// riapertura del sw per via della doppia istanza.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClosingEvent_homePageClosing(object sender, FormClosingEventArgs e)
        {
            log.Info("Form Homepage: GUI chiusa, terminazione del programma e liberazione delle risorse");
            //Application.Exit(); // non basta
            //Environment.Exit(0); // metodo drastico, termina il processo e libera le risorse in questo momento
            if (!Global.shouldReset)
                Process.GetCurrentProcess().Kill(); //aspetta che i thread termino e libera le risorse 
        }

        /// <summary>
        /// Apertura pannello diagnostica
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pnl_diagnostics_Click(object sender, EventArgs e)
        {
            if (!RobotManager.formDiagnostics.Visible)
            {
                RobotManager.formDiagnostics.Visible = true;
            }
        }

        #endregion
    }
}
