using System;
using System.Windows.Forms;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using RMLib.DataAccess;
using RMLib.Logger;
using RMLib.Alarms;
using RMLib.Keyboards;
using RMLib.MessageBox;
using RMLib.PLC;
using RMLib.Security;
using RM.src.RM250619.Forms.Plant;
using RM.Properties;
using System.Drawing;
using RMLib.VATView;
using RM.src.RM250619.Forms.DragMode;
using fairino;
using RM.src.RM250619.Classes.FR20.Jog;
using System.Data;
using RM.src.RM250619.Classes.PLC;
using RM.src.RM250619.Classes.FR20.Applications.Application;
using System.Drawing.Drawing2D;
using RM.src.RM250619.Forms.Plant.DragMode;
using RM.src.RM250619.Forms.ScreenSaver;

namespace RM.src.RM250619
{
    /// <summary>
    /// Definisce la struttura, il comportamento e la UI della home page del software, da usare come pannello preimpostato.
    /// </summary>
    public partial class UC_HomePage : UserControl
    {
        #region Variabili per connessione database
        private static readonly RobotDAOSqlite robotDAO = new RobotDAOSqlite();
        private static readonly SqliteConnectionConfiguration DatabaseConnection = new SqliteConnectionConfiguration();
        private static readonly string ConnectionString = DatabaseConnection.GetConnectionString();
        #endregion

        #region Proprietà di UC_HomePage

        #region Colori

        /// <summary>
        /// Colore associato alle righe lw per gli spostamenti verso un punto
        /// </summary>
        private readonly Color MoveToPointColor = Color.Orange;
        /// <summary>
        /// Colore associato alle righe lw per i punti presi e salvati
        /// </summary>
        private readonly Color GenericPointColor = Color.White;
        /// <summary>
        /// Colore associato alle righe lw per il punto corrente in cui ci si trova
        /// </summary>
        private readonly Color CurrentPointColor = Color.Silver;

        #endregion

        /// <summary>
        /// Logger
        /// </summary>
        private static readonly log4net.ILog log = LogHelper.GetLogger();

        /// <summary>
        /// Variabile d'appoggio velocità corrente Robot
        /// </summary>
        private int currentVelocity;

        /// <summary>
        /// Timer per salvare velocità Robot
        /// </summary>
        private System.Timers.Timer VelocitySaverTimer;
       
        /// <summary>
        /// Specifica se il robot è in modalità automatica, altrimenti sarà in manuale
        /// </summary>
        private bool isAutomaticMode = false;

        // Variabili per aumentare o decrementare la velocità del Robot
        private System.Windows.Forms.Timer continuousAddSpeedTimer;
        private System.Windows.Forms.Timer continuousRemoveSpeedTimer;
        private bool isMouseDown = false;

        /// <summary>
        /// Indice della posizione da selezionare su lw_positions
        /// </summary>
        public static int index = 0;

        /// <summary>
        /// Indice del punto da aggiungere a lw_positions
        /// </summary>
        static int pointIndex = 0;

        #endregion

        #region Variabili d'istanza
        static UC_HomePage _obj;

        /// <summary>
        /// Definisce una variabile statica da utilizzare come istanza per accedere al contenuto di questa pagina da codice esterno
        /// </summary>
        public static UC_HomePage Instance
        {
            get 
            {
                if (_obj == null)
                {
                    _obj = new UC_HomePage();
                }
                return _obj;
            }
        }

        /// <summary>
        /// Serve per aggiornate la textBox della velocità
        /// </summary>
      public string UpdateSpeed
        {
            get { return lbl_velocity.Text; }
            set { lbl_velocity.Text = value; }
        }
        #endregion

        #region Metodi di UC_HomePage
        /// <summary>
        /// Inizializza lo user control della homepage
        /// </summary>
        public UC_HomePage()
        {
            InitializeComponent();
            _obj = this;

            // Imposta l'intestazione della homepage
            FormHomePage.Instance.LabelHeader = "HOME PAGE";

            // Avvia il caricamento asincrono dei parametri
            /*
            if (AlarmManager.isPlcConnected)
            {
                Task.Run(() => InitParameters());
            }*/

            // Collegamento evento ValueChanged del dizionario al metodo HandleDictionaryChange
            //PLCConfig.appVariables.ValueChanged += RefreshVariables;

            // Collegamento evento Login e Logout per mostrare/nascondere la VAT
            SecurityManager.SecMgrLoginRM += OnRMLogin;
            SecurityManager.SecMgrLogout += OnRMLogout;

            // Collegamento evento cambio velocità Robot al metodo che cambia valore della label velocità Robot
            RobotManager.RobotVelocityChanged += ChangeLblVelocity;
            RobotManager.RobotModeChanged += SelectRobotModeButton;

            RobotManager.EnableButtonCycleEvent += RobotManager_EnableButtonCycleEvent;

            RobotManager.RobotInHomePosition += RobotInHomePositionEvent;
            RobotManager.RobotNotInHomePosition += RobotNotInHomePositionEvent;

            RobotManager.GripperStatusOFF += GripperStatusOFFEvent;
            RobotManager.GripperStatusON += GripperStatusONEvent;

            // Traduce e inizializza i font
            Translate();
            InitFont();

            //if (AlarmManager.isPlcConnected)
            {
                InitLbl_velocity();
            }

            InitVelocitySaverTimer();

            // Inizializza il timer
            continuousAddSpeedTimer = new System.Windows.Forms.Timer();
            continuousAddSpeedTimer.Interval = 250; // intervallo in millisecondi
            continuousAddSpeedTimer.Tick += ContinuousAddVelocityTimer_Tick;

            continuousRemoveSpeedTimer = new System.Windows.Forms.Timer();
            continuousRemoveSpeedTimer.Interval = 250; // intervallo in millisecondi
            continuousRemoveSpeedTimer.Tick += ContinuousRemoveVelocityTimer_Tick;

            //Controllo per vedere in che modalità è il robot in base a una nuova variabile nel robot manager
            // SynchronizeMode();

            // Avvia il thread per l'applicazione della trasparenza
            Task.Run(() => ApplyPanelTransparency());

            InitRobotModeButtons();

            ScreenSaverManager.AutoAddClickEvents(this);
        }

        private void GripperStatusOFFEvent(object sender, EventArgs e)
        {
            pnl_pinzeStatus.BackColor = Color.SeaGreen;
        }

        private void GripperStatusONEvent(object sender, EventArgs e) 
        {
            pnl_pinzeStatus.BackColor = Color.Firebrick;
        }

        private void RobotInHomePositionEvent(object sender, EventArgs e)
        {
            pnl_homeStatus.BackColor = Color.SeaGreen;
        }

        private void RobotNotInHomePositionEvent(object sender, EventArgs e)
        {
            pnl_homeStatus.BackColor = Color.Firebrick;
        }

        /// <summary>
        /// Imposta i button relativi alla modalità del Robot quando viene avviata l'applicazione
        /// </summary>
        private void InitRobotModeButtons()
        {
            int mode = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.Operating_Mode));

            switch (mode)
            {
                case 2:
                    #region Manuale

                    btn_autoMode.BackColor = SystemColors.ControlDark;
                    btn_manualMode.BackColor = Color.LimeGreen;

                    btn_homePosition.Enabled = true;
                    btn_homePosition.BackColor = SystemColors.Control;
                    btn_homePosition.BackgroundImage = Resources.home;

                    btn_openGripper.Enabled = true;
                    btn_openGripper.BackColor = SystemColors.Control;
                    btn_openGripper.BackgroundImage = Resources.gripper;

                    btn_startApp.Enabled = false;
                    btn_startApp.BackColor = SystemColors.ControlDark;
                    btn_startApp.BackgroundImage = null;

                    btn_stopApp.Enabled = false;
                    btn_stopApp.BackColor = SystemColors.ControlDark;
                    btn_stopApp.BackgroundImage = null;

                    btn_pauseApp.Enabled = false;
                    btn_pauseApp.BackColor = SystemColors.ControlDark;
                    btn_pauseApp.BackgroundImage = null;

                    break;

                #endregion

                case 1:
                    #region Automatico

                    btn_autoMode.BackColor = Color.DodgerBlue;
                    btn_manualMode.BackColor = SystemColors.ControlDark;

                    btn_homePosition.Enabled = false;
                    btn_homePosition.BackColor = SystemColors.ControlDark;
                    btn_homePosition.BackgroundImage = null;

                    btn_openGripper.Enabled = false;
                    btn_openGripper.BackColor = SystemColors.ControlDark;
                    btn_openGripper.BackgroundImage = null;

                    btn_startApp.Enabled = true;
                    btn_startApp.BackColor = SystemColors.Control;
                    btn_startApp.BackgroundImage = Resources.play32;

                    btn_stopApp.Enabled = false;
                    btn_stopApp.BackColor = SystemColors.ControlDark;
                    btn_stopApp.BackgroundImage = null;

                    btn_pauseApp.Enabled = false;
                    btn_pauseApp.BackColor = SystemColors.ControlDark;
                    btn_pauseApp.BackgroundImage = null;

                    break;

                #endregion

                case 0:
                    #region Off

                    btn_autoMode.BackColor = SystemColors.ControlDark;
                    btn_manualMode.BackColor = SystemColors.ControlDark;

                    btn_homePosition.Enabled = false;
                    btn_homePosition.BackColor = SystemColors.ControlDark;
                    btn_homePosition.BackgroundImage = null;

                    btn_openGripper.Enabled = false;
                    btn_openGripper.BackColor = SystemColors.ControlDark;
                    btn_openGripper.BackgroundImage = null;

                    btn_startApp.Enabled = false;
                    btn_startApp.BackColor = SystemColors.ControlDark;
                    btn_startApp.BackgroundImage = null;

                    btn_stopApp.Enabled = false;
                    btn_stopApp.BackColor = SystemColors.ControlDark;
                    btn_stopApp.BackgroundImage = null;

                    btn_pauseApp.Enabled = false;
                    btn_pauseApp.BackColor = SystemColors.ControlDark;
                    btn_pauseApp.BackgroundImage = null;

                    break;

                    #endregion
            }
        }

        /// <summary>
        /// Gestisce attivazione e disattivazione dei tasti start, stop e pausa quando
        /// viene cambiata la modalità di lavoro del Robot
        /// </summary>
        /// <param name="sender">1 --> Auto / 0 --> Man</param>
        /// <param name="e"></param>
        private void SelectRobotModeButton(object sender, EventArgs e)
        {
            int mode = Convert.ToInt16(sender.ToString());

            switch (mode)
            {
                case 0:
                    #region Manuale

                    btn_autoMode.BackColor = SystemColors.ControlDark;
                    btn_manualMode.BackColor = Color.LimeGreen;

                    btn_homePosition.Enabled = true;
                    btn_homePosition.BackColor = SystemColors.Control;
                    btn_homePosition.BackgroundImage = Resources.home;

                    btn_openGripper.Enabled = true;
                    btn_openGripper.BackColor = SystemColors.Control;
                    btn_openGripper.BackgroundImage = Resources.gripper;

                    btn_startApp.Enabled = false;
                    btn_startApp.BackColor = SystemColors.ControlDark;
                    btn_startApp.BackgroundImage = null;

                    btn_stopApp.Enabled = false;
                    btn_stopApp.BackColor = SystemColors.ControlDark;
                    btn_stopApp.BackgroundImage = null;

                    btn_pauseApp.Enabled = false;
                    btn_pauseApp.BackColor = SystemColors.ControlDark;
                    btn_pauseApp.BackgroundImage = null;

                    break;

                #endregion

                case 1:
                    #region Automatico

                    btn_autoMode.BackColor = Color.DodgerBlue;
                    btn_manualMode.BackColor = SystemColors.ControlDark;

                    btn_homePosition.Enabled = false;
                    btn_homePosition.BackColor = SystemColors.ControlDark;
                    btn_homePosition.BackgroundImage = null;

                    btn_openGripper.Enabled = false;
                    btn_openGripper.BackColor = SystemColors.ControlDark;
                    btn_openGripper.BackgroundImage = null;

                    btn_startApp.Enabled = true;
                    btn_startApp.BackColor = SystemColors.Control;
                    btn_startApp.BackgroundImage = Resources.play32;

                    btn_stopApp.Enabled = false;
                    btn_stopApp.BackColor = SystemColors.ControlDark;
                    btn_stopApp.BackgroundImage = null;

                    btn_pauseApp.Enabled = false;
                    btn_pauseApp.BackColor = SystemColors.ControlDark;
                    btn_pauseApp.BackgroundImage = null;

                    break;

                #endregion

                case 3:
                    #region Off

                    btn_autoMode.BackColor = SystemColors.ControlDark;
                    btn_manualMode.BackColor = SystemColors.ControlDark;

                    btn_homePosition.Enabled = false;
                    btn_homePosition.BackColor = SystemColors.ControlDark;
                    btn_homePosition.BackgroundImage = null;

                    btn_startApp.Enabled = false;
                    btn_startApp.BackColor = SystemColors.ControlDark;
                    btn_startApp.BackgroundImage = null;

                    btn_openGripper.Enabled = false;
                    btn_openGripper.BackColor = SystemColors.ControlDark;
                    btn_openGripper.BackgroundImage = null;

                    btn_stopApp.Enabled = false;
                    btn_stopApp.BackColor = SystemColors.ControlDark;
                    btn_stopApp.BackgroundImage = null;

                    btn_pauseApp.Enabled = false;
                    btn_pauseApp.BackColor = SystemColors.ControlDark;
                    btn_pauseApp.BackgroundImage = null;

                    break;

                    #endregion
            }

        }

        private void ChangeLblVelocity(object sender, EventArgs e)
        {
            lbl_velocity.Text = sender.ToString();
        }

        private void RobotManager_RobotVelocityChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gestisce l'attivazione e disattivazione dei tasti start, stop e pausa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RobotManager_EnableButtonCycleEvent(object sender, EventArgs e)
        {
            if (Convert.ToInt16(sender) == 1) // Attiva start, disattiva pausa e stop
            {
                btn_stopApp.Enabled = false;
                btn_stopApp.BackColor = SystemColors.ControlDark;
                btn_stopApp.BackgroundImage = null;

                btn_pauseApp.Enabled = false;
                btn_pauseApp.BackColor = SystemColors.ControlDark;
                btn_pauseApp.BackgroundImage = null;

                btn_startApp.Enabled = true;
                btn_startApp.BackColor = SystemColors.Control;
                btn_startApp.BackgroundImage = Resources.play32;
            }
            else
            if (Convert.ToInt16(sender) == 0) // Disattiva start, attiva pausa e stop
            {
                btn_stopApp.Enabled = true;
                btn_stopApp.BackColor = SystemColors.Control;
                btn_stopApp.BackgroundImage = Resources.stop;

                btn_pauseApp.Enabled = true;
                btn_pauseApp.BackColor = SystemColors.Control;
                btn_pauseApp.BackgroundImage = Resources.pausemonitoringRed_32;

                btn_startApp.Enabled = false;
                btn_startApp.BackColor = SystemColors.ControlDark;
                btn_startApp.BackgroundImage = null;
            }
        }

        /// <summary>
        /// Abilita e disabilita i tasti di start, stop e pausa
        /// </summary>
        /// <param name="status">//0: Disattiva stop, disattiva pause, attiva start - 1: Attiva stop, attiva pause, disattiva start</param>
        public void EnableCycleButton(int status)
        {
            if (status == 0) // Disattiva stop, disattiva pause, attiva start
            {
                btn_stopApp.Enabled = false;
                btn_stopApp.BackColor = SystemColors.ControlDark;
                btn_stopApp.BackgroundImage = null;

                btn_pauseApp.Enabled = false;
                btn_pauseApp.BackColor = SystemColors.ControlDark;
                btn_pauseApp.BackgroundImage = null;

                btn_startApp.Enabled = true;
                btn_startApp.BackColor = SystemColors.Control;
                btn_startApp.BackgroundImage = Resources.play32;
            }
            else if (status == 1) // Attiva stop, attiva pause, disattiva start
            {
                btn_startApp.Enabled = false;
                btn_startApp.BackColor = SystemColors.ControlDark;
                btn_startApp.BackgroundImage = null;

                btn_stopApp.Enabled = true;
                btn_stopApp.BackColor = SystemColors.Control;
                btn_stopApp.BackgroundImage = Resources.stop;

                btn_pauseApp.Enabled = true;
                btn_pauseApp.BackColor = SystemColors.Control;
                btn_pauseApp.BackgroundImage = Resources.pausemonitoringRed_32;
            }

        }

        /// <summary>
        /// Applica la trasparenza ai pannelli.
        /// </summary>
        private void ApplyPanelTransparency()
        {
          /*  Color opacity200 = Color.FromArgb(200, pnl_navigation.BackColor.R, pnl_navigation.BackColor.G, pnl_navigation.BackColor.B);
            Color opacity170 = Color.FromArgb(170, pnl_navigation.BackColor.R, pnl_navigation.BackColor.G, pnl_navigation.BackColor.B);
            Color opacity0 = Color.FromArgb(0, pnl_navigation.BackColor.R, pnl_navigation.BackColor.G, pnl_navigation.BackColor.B);

            // Sospendi temporaneamente il layout dei controlli
            this.Invoke(new Action(() => this.SuspendLayout()));

            ApplyTransparency(pnl_navigation, opacity200);
            ApplyTransparency(pnl_buttonVAT, opacity170);
            ApplyTransparency(pnl_modeContainer, opacity170);
            ApplyTransparency(pnl_robotContainer, opacity170);
            ApplyTransparency(pnl_appContainer, opacity170);
            ApplyTransparency(pnl_program, opacity170);
            ApplyTransparency(pnl_monitoring, opacity170);
            ApplyTransparency(pnl_buttonVAT, opacity0);*/

            // Riprendi il layout dei controlli
            Invoke(new Action(() => ResumeLayout()));
        }

        /// <summary>
        /// Applica la trasparenza a un pannello specificato.
        /// </summary>
        /// <param name="panel">Il pannello a cui applicare la trasparenza.</param>
        /// <param name="color">Il colore con il livello di opacità da applicare.</param>
        private void ApplyTransparency(Panel panel, Color color)
        {
            if (panel.InvokeRequired)
            {
                panel.Invoke(new Action(() => panel.BackColor = color));
            }
            else
            {
                panel.BackColor = color;
            }
        }

        /// <summary>
        /// Inizializza il timer che salva la velocità del Robot
        /// </summary>
        private void InitVelocitySaverTimer()
        {
            VelocitySaverTimer = new System.Timers.Timer(1000); // intervallo di 1 secondo (1000 millisecondi)
            VelocitySaverTimer.Elapsed += OnSaveTimerElapsed;
            VelocitySaverTimer.AutoReset = false; // il timer non si resetta automaticamente
        }

        /// <summary>
        /// Inizializza i valori dei controlli presenti nell'interfaccia
        /// </summary>
        private async Task InitParameters()
        {
            object operatingMode;

            operatingMode = PLCConfig.appVariables.getValue(PLCTagName.Operating_Mode);

            await Task.Delay(100);

            // Aggiorna la UI nel thread della UI
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    UpdateUI(operatingMode);
                }));
            }
            else
            {
                UpdateUI(operatingMode);
            }
        }

        private void UpdateUI(object operatingMode)
        {
            if (Convert.ToInt16(operatingMode) == 1)
            {
                // Manuale

                btn_autoMode.BackColor = SystemColors.ControlDark;
                btn_manualMode.BackColor = Color.LimeGreen;
                btn_homePosition.Enabled = true;
                btn_homePosition.BackColor = SystemColors.Control;
                btn_homePosition.BackgroundImage = Resources.home;
            }

            if (Convert.ToInt16(operatingMode) == 0)
            {
                // Automatico

                btn_autoMode.BackColor = Color.DodgerBlue;
                btn_manualMode.BackColor = SystemColors.ControlDark;
                btn_homePosition.Enabled = false;
                btn_homePosition.BackColor = SystemColors.ControlDark;
                btn_homePosition.BackgroundImage = null;
            }

        }


        /// <summary>
        /// TODO
        /// </summary>
        private void Translate()
        {

        }

        /// <summary>
        /// TODO
        /// </summary>
        private void InitFont()
        {

        }

        /// <summary> 
        /// Metodo chiamato quando il timer della velocità del Robot scade 
        /// </summary> 
        private void OnSaveTimerElapsed(object sender, ElapsedEventArgs e)
        {
            robotDAO.SetRobotVelocity(ConnectionString, currentVelocity);
            robotDAO.SetRobotAcceleration(ConnectionString, currentVelocity);
        }

        /// <summary>
        /// Inizializza label relativa alla velocità del Robot
        /// </summary>
        private void InitLbl_velocity()
        {
            try
            {
                if (RobotManager.robotProperties != null)
                    lbl_velocity.Text = RobotManager.robotProperties.Velocity.ToString();
                else
                    lbl_velocity.Text = "0";
            }
            catch
            {
                log.Error("Configurazione robot fallita, inserimento velocità : 0");
                lbl_velocity.Text = "0";
            }
        }

        /// <summary>
        /// Metodo richiamato dall'evento ValueChanged del dizionario delle variabili PLC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RefreshVariables(object sender, DictionaryChangedEventArgs e)
        {
            if (AlarmManager.isPlcConnected)
            {
                switch (e.Key)
                {
                    case PLCTagName.Operating_Mode:

                        if (Convert.ToInt16(e.NewValue) == 2)
                        {
                            // Manuale

                            btn_autoMode.BackColor = SystemColors.ControlDark;
                            btn_manualMode.BackColor = Color.LimeGreen;
                            btn_homePosition.Enabled = true;
                            btn_homePosition.BackColor = SystemColors.Control;
                            btn_homePosition.BackgroundImage = Resources.home;
                        }

                        if (Convert.ToInt16(e.NewValue) == 1)
                        {
                            // Automatico

                            btn_autoMode.BackColor = Color.DodgerBlue;
                            btn_manualMode.BackColor = SystemColors.ControlDark;
                            btn_homePosition.Enabled = false;
                            btn_homePosition.BackColor = SystemColors.ControlDark;
                            btn_homePosition.BackgroundImage = null;
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Evento generato quando ci si logga con un account di livello 3 o superiore
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRMLogin(object sender, EventArgs e)
        {
            //pnl_buttonVAT.Visible = true;
            btn_VAT.Visible = true;
            lbl_buttonVAT.Visible = true;
        }

        /// <summary>
        /// Evento generato quando si fa il logout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRMLogout(object sender, EventArgs e)
        {
            //pnl_buttonVAT.Visible = false;
            btn_VAT.Visible = false;
            lbl_buttonVAT.Visible = false;
            VATViewManager.CloseAll();
        }

        /// <summary>
        /// Imposta il valore della label dell'applicazione da eseguire
        /// </summary>
        /// <param name="application"></param>
        public void SetApplicationToExecute(string application)
        {
            lb_applicationToExecute.Text = application;
        }

        #endregion

        #region Eventi di UC_HomePage
        /// <summary>
        /// Aumenta la velocità tenendo premuto il tasto +
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContinuousAddVelocityTimer_Tick(object sender, EventArgs e)
        {
            if (isMouseDown)
            {
                currentVelocity = Convert.ToInt32(lbl_velocity.Text);

                if (currentVelocity < 100)
                    currentVelocity++;

                lbl_velocity.Text = currentVelocity.ToString();

                // Reset del timer
                VelocitySaverTimer.Stop();
                VelocitySaverTimer.Start();
            }
        }

        /// <summary>
        /// Diminuisce la velocità tenendo premuto il tasto -
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContinuousRemoveVelocityTimer_Tick(object sender, EventArgs e)
        {
            if (isMouseDown)
            {
                currentVelocity = Convert.ToInt32(lbl_velocity.Text);

                if (currentVelocity > 0)
                    currentVelocity--;

                lbl_velocity.Text = currentVelocity.ToString();

                // Reset del timer
                VelocitySaverTimer.Stop();
                VelocitySaverTimer.Start();
            }
        }

        /// <summary>
        /// Caricamento dell HomePage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UC_HomePage_Load(object sender, EventArgs e)
        {
            User actualUser = SecurityManager.GetActualUser();
            if (actualUser != null && actualUser.SecurityLevel >= 3)
            {
                btn_VAT.Visible = true;
                lbl_buttonVAT.Visible = true;
            }
            if(!ScreenSaverManager.useScreenSaver)
            {
                btn_restoreSCreenSaverManager.Visible = false;
                lbl_restoreScreenSaverManager.Visible = false;
            }
        }

        /// <summary>
        /// Apre la form VAT view per la gestione delle variabili PLC. Accessibile solo agli utenti RM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_openVAT(object sender, EventArgs e)
        {
            VATViewManager.ShowVAT();
        }

        /// <summary>
        /// Permette all'utente di uscire e chiudere l'applicativo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_exit(object sender, EventArgs e)
        {
            if (!SecurityManager.ActionRequestCheck("exit")) return;

            if(CustomMessageBox.ShowTranslated(MessageBoxTypeEnum.WARNING, "MSG_CLOSING_APP", Resources.exit_filled) == DialogResult.OK)
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Apre la form per la gestione della sicurezza e degli utenti.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_openSecurityManager(object sender, EventArgs e)
        {
            FormSecurityView securityView = new FormSecurityView();
            securityView.ShowDialog();
        }

        /// <summary>
        /// Apre la form degli allarmi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_openAlarmPage(object sender, EventArgs e)
        {
            AlarmManager.OpenAlarmFormPage(RobotManager.formAlarmPage);
        }

        /// <summary>
        /// Apre la form dei permessi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_openPermissionsPage(object sender, EventArgs e)
        {
            return; 

            UC_permissions uc;
            if (!FormHomePage.Instance.PnlContainer.Controls.ContainsKey("UC_permissions"))
            {
                uc = new UC_permissions()
                {
                    Dock = DockStyle.Fill
                };
                FormHomePage.Instance.PnlContainer.Controls.Add(uc);
            }
            FormHomePage.Instance.PnlContainer.Controls["UC_permissions"].BringToFront();
        }

        /// <summary>
        /// Apre la form delle configurazioni
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_openConfigurationPage(object sender, EventArgs e)
        {
            return;

            UC_configuration uc;
            if (!FormHomePage.Instance.PnlContainer.Controls.ContainsKey("UC_configuration"))
            {
                uc = new UC_configuration()
                {
                    Dock = DockStyle.Fill
                };
                FormHomePage.Instance.PnlContainer.Controls.Add(uc);
            }
            FormHomePage.Instance.PnlContainer.Controls["UC_configuration"].BringToFront();
        }

        /// <summary>
        /// Evento attivato quando si vuole aggiungere speed da tasto con incremento di 1
        /// </summary>
        private void ClickEvent_addVelocity(object sender, EventArgs e)
        {
            currentVelocity = Convert.ToInt32(lbl_velocity.Text);

            if(currentVelocity < 100) 
                currentVelocity++;

            lbl_velocity.Text = currentVelocity.ToString();

            // Reset del timer
            VelocitySaverTimer.Stop(); 
            VelocitySaverTimer.Start();
        }
   
        /// <summary>
        /// Evento attivato quando si vuole rimuovere speed da tasto con decremento di 1
        /// </summary>
        private void ClickEvent_removeVelocity(object sender, EventArgs e)
        {
            currentVelocity = Convert.ToInt32(lbl_velocity.Text);

            if (currentVelocity > 0)
                currentVelocity--;

            lbl_velocity.Text = currentVelocity.ToString();

            // Reset del timer
            VelocitySaverTimer.Stop();
            VelocitySaverTimer.Start();
        }

        /// <summary>
        /// Apre la pagina delle posizioni
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_openPositions(object sender, EventArgs e)
        {
            if (!SecurityManager.ActionRequestCheck("positions")) return;

            UC_positions uc;
            if (!FormHomePage.Instance.PnlContainer.Controls.ContainsKey("UC_positions"))
            {
                uc = new UC_positions()
                {
                    Dock = DockStyle.Fill
                };
                FormHomePage.Instance.PnlContainer.Controls.Add(uc);
            }
            FormHomePage.Instance.PnlContainer.Controls["UC_positions"].BringToFront();

            uc = (UC_positions)FormHomePage.Instance.PnlContainer.Controls["UC_positions"];
            //uc.ShowLoadingScreen();
        }

        /// <summary>
        /// Apre pagina parametri Robot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_openParameters(object sender, EventArgs e)
        {
            if (!SecurityManager.ActionRequestCheck("parameters")) return;

            UC_parameters uc;
            if (!FormHomePage.Instance.PnlContainer.Controls.ContainsKey("UC_parameters"))
            {
                uc = new UC_parameters()
                {
                    Dock = DockStyle.Fill
                };
                FormHomePage.Instance.PnlContainer.Controls.Add(uc);
            }
            FormHomePage.Instance.PnlContainer.Controls["UC_parameters"].BringToFront();

            uc = (UC_parameters)FormHomePage.Instance.PnlContainer.Controls["UC_parameters"];
            //uc.ShowLoadingScreen();
        }

        /// <summary>
        /// Invio richiesta di modifica velocità robot al PLC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_setVelocity(object sender, EventArgs e)
        {
            if (!SecurityManager.ActionRequestCheck("modifyRobotSpeed")) return;

            string newVelocity = VK_Manager.OpenIntVK("0",1,100);

            if (newVelocity.Equals(VK_Manager.CANCEL_STRING)) return;
            if (Convert.ToInt16(newVelocity) < 1 || Convert.ToInt16(newVelocity) > 100)
            {
                CustomMessageBox.Show(MessageBoxTypeEnum.ERROR, "Selezionare un valore da 1 a 100");
                return;
            }
            else
            {
                robotDAO.SetRobotVelocity(ConnectionString, Convert.ToInt16(newVelocity));
                robotDAO.SetRobotAcceleration(ConnectionString, Convert.ToInt16(newVelocity));
                lbl_velocity.Text = newVelocity;
            }
        }   

        /// <summary>
        /// Aumento della velocità
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseDownEvent_addVelocity(object sender, MouseEventArgs e)
        {
            if (!SecurityManager.ActionRequestCheck("modifyRobotSpeed")) return;
            isMouseDown = true;
            continuousAddSpeedTimer.Start();
        }

        /// <summary>
        /// Aumento della velocità
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseUpEvent_addVelocity(object sender, MouseEventArgs e)
        {
            if (!SecurityManager.ActionRequestCheck("modifyRobotSpeed")) return;
            isMouseDown = false;
            continuousAddSpeedTimer.Stop();
        }

        /// <summary>
        /// Diminuzione della velocità
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseDownEvent_removeVelocity(object sender, MouseEventArgs e)
        {
            if (!SecurityManager.ActionRequestCheck("modifyRobotSpeed")) return;
            isMouseDown = true;
            continuousRemoveSpeedTimer.Start();
        }

        /// <summary>
        /// Diminuzione della velocità
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MouseUpEvent_removeVelocity(object sender, MouseEventArgs e)
        {
            if (!SecurityManager.ActionRequestCheck("modifyRobotSpeed")) return;
            isMouseDown = false;
            continuousRemoveSpeedTimer.Stop();
        }

        /// <summary>
        /// Imposta la modalità manuale al robot (mode 1)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_manualMode(object sender, EventArgs e)
        {
            RobotManager.ClearRobotAlarm();
            RobotManager.ClearRobotQueue();

            btn_autoMode.BackColor = SystemColors.ControlDark;
            btn_manualMode.BackColor = Color.LimeGreen;
            btn_homePosition.Enabled = true;
            btn_homePosition.BackColor = SystemColors.Control;
            btn_homePosition.BackgroundImage = Resources.home;

            if (RobotManager.robotInPause)
            {
                RobotManager.robot.ResumeMotion();

            }

            RobotManager.SetRobotMode(1);
            JogMovement.StartJogRobotThread();
        }

        /// <summary>
        /// Imposta la modalità automatica al robot (mode 0)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_autoMode(object sender, EventArgs e)
        {
            //auto mode
            btn_autoMode.BackColor = Color.DodgerBlue;
            btn_manualMode.BackColor = SystemColors.ControlDark;
            btn_homePosition.Enabled = false;
            btn_homePosition.BackColor = SystemColors.ControlDark;
            btn_homePosition.BackgroundImage = null;

            RobotManager.robotCycleStopRequested = false;
            RobotManager.SetRobotMode(0);
            JogMovement.StopJogRobotThread();
        }

        /// <summary>
        /// Apre la pagine di DragMode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_openDragMode(object sender, EventArgs e)
        {
            if (!SecurityManager.ActionRequestCheck("dragMode")) return;

            if (homeRoutineStarted)
            {
                CustomMessageBox.Show(MessageBoxTypeEnum.WARNING_OK, "Attendi il termine della Home routine");
                log.Error("Tentativo di accesso a Drag Mode durante home routine");
                return;
            }

            if (string.IsNullOrEmpty(RobotManager.applicationName))
            {
                CustomMessageBox.Show(MessageBoxTypeEnum.ERROR, "Selezionare prima un'applicazione da usare");
                log.Error("Tentativo di accesso a Drag Mode senza nessun'applicazione selezionata");
                return;
            }

            /*
            if (RobotManager.isAutomaticMode)
            {
                CustomMessageBox.Show(MessageBoxTypeEnum.ERROR, "Impostare il robot in modalità manuale");
                log.Error("Tentativo di accesso a Drag Mode con robot in modalità automatic");
                return;
            }
            */

            UC_FullDragModePage uc;
            if (!FormHomePage.Instance.PnlContainer.Controls.ContainsKey("UC_FullDragModePage"))
            {
                uc = new UC_FullDragModePage()
                {
                    Dock = DockStyle.Fill
                };
                FormHomePage.Instance.PnlContainer.Controls.Add(uc);
            }
            FormHomePage.Instance.PnlContainer.Controls["UC_FullDragModePage"].BringToFront();

            uc = (UC_FullDragModePage)FormHomePage.Instance.PnlContainer.Controls["UC_FullDragModePage"];
            uc.ShowLoadingScreen();
        }

        /// <summary>
        /// Apre la pagina delle applicazioni
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_openApplications(object sender, EventArgs e)
        {
            UC_ApplicationPage uc;
            if (!FormHomePage.Instance.PnlContainer.Controls.ContainsKey("UC_ApplicationPage"))
            {
                uc = new UC_ApplicationPage()
                {
                    Dock = DockStyle.Fill
                };
                FormHomePage.Instance.PnlContainer.Controls.Add(uc);
            }
            FormHomePage.Instance.PnlContainer.Controls["UC_ApplicationPage"].BringToFront();

            uc = (UC_ApplicationPage)FormHomePage.Instance.PnlContainer.Controls["UC_ApplicationPage"];
            uc.ShowLoadingScreen();
        }

        /// <summary>
        /// Avvio dell'applicazione scelta
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_startApp(object sender, EventArgs e)
        {
            log.Info("Richiesta di avvio ciclo");
            string application = RobotManager.applicationName; // Get nome applicazione

            // Se il Robot non si trova in home position e non stava riproducendo nessun punto
            // stampo un messaggio di errore per richiedere di portare il Robot in posizione di Home
            if (!RobotManager.isInHomePosition && RobotManager.currentIndex < 0)
            {
                CustomMessageBox.Show(MessageBoxTypeEnum.ERROR, "Spostare il Robot nella posizione di Home", Resources.safeZone_yellow32);
                log.Error("Tentativo di avvio applicazione con Robot fuori posizione di Home");
                return;
            }

            /*
            int gripperStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.GripperStatusIn));
        
            if (gripperStatus != 1)
            {
                CustomMessageBox.Show(MessageBoxTypeEnum.ERROR, "Aprire la pinza prima di avviare il ciclo", Resources.safeZone_yellow32);
                log.Error("Tentativo di avvio applicazione con pinza chiusa");
                return;
            }
            */

            if (!string.IsNullOrEmpty(application)) // Se l'applicazione è stata selezionata
            {
                log.Info($"Applicazione scelta: {application}");
                // Setto della velocità del Robot dalle sue proprietà memorizzate sul database
                if (RobotManager.robotProperties.Speed > 1)
                {
                    int speed = RobotManager.robotProperties.Speed;
                    RobotManager.robot.SetSpeed(speed);
                    log.Info($"Velocità Robot: {speed}");
                }

                if (!AlarmManager.isRobotMoving) // Se il Robot non è in movimento 
                {
                    /*
                    // Get da database delle posizioni dell'applicazione selezionata
                    DataTable positions = robotDAO.GetPointsPosition(ConnectionString, application);

                    // Se non ci sono posizioni presenti nell'applicazione da riprodurre
                    if (positions.Rows.Count < 1)
                    {
                        CustomMessageBox.Show(MessageBoxTypeEnum.ERROR, "Nessun punto presente nell'applicazione selezionata");
                        log.Error("Tentativo di riprodurre un'applicazione senza posizioni");
                        return;
                    }*/

                    // Se l'avvio dell'applicazione parte dal primo punto
                    if (RobotManager.currentIndex < 0)
                    {
                        if (CustomMessageBox.Show(MessageBoxTypeEnum.WARNING, "Procedere con l'avvio dell'applicazione?") != DialogResult.OK)
                            return;
                    }
                    else // Se l'applicazione riprende da un punto precedente
                    {
                        if (CustomMessageBox.Show(MessageBoxTypeEnum.WARNING, "Riprendere la riproduzione dell'applicazione?") != DialogResult.OK)
                            return;
                    }

                    // Quindi, se il ciclo era stato messo precedentemente in pausa, metto a true il booleano riprendiCiclo
                    // che fa uscire dallo step di attesa il metodo che riproduce i punti dentro StartApplication
                    if (RobotManager.pauseCycleRequested)
                    {
                        RobotManager.riprendiCiclo = true;
                    }
                    else // Se invece il ciclo deve iniziare dall'inizio, avvio normalmente
                    {
                       // RobotManager.StartApplication(application);
                        RobotManager.PickAndPlaceFocaccia();
                        EnableCycleButton(1); // Disattiva stop, disattiva pause, attiva start
                    }
                }
                else // Se il Robot è in movimento
                {
                    log.Error("Impossibile inviare nuovi punti al Robot. Robot in movimento");
                    CustomMessageBox.Show(MessageBoxTypeEnum.ERROR, "Impossibile inviare nuovi punti al Robot");
                }
            }
            else // Se l'applicazione non è stata selezionata
            {
                CustomMessageBox.Show(MessageBoxTypeEnum.ERROR, "Nessuna applicazione caricata da riprodurre");
                log.Error("Nessuna applicazione caricata da riprodurre");
            }
        }
        /// <summary>
        /// A true quando routine di home avviata
        /// </summary>
        public static bool homeRoutineStarted = false;

        /// <summary>
        /// Controlla che il robot si trovi nella home zone
        /// </summary>
        /// <returns></returns>
        private bool CheckRobotIsInHomeZone()
        {
            // Oggetto che rileva home zone
            PositionChecker checker_homeZone;
            double delta_homeZone = 500.0; // Dichiarazione delta
            checker_homeZone = new PositionChecker(delta_homeZone); // Creazione oggetto

            // Get del punto di home
            var restPose = ApplicationConfig.applicationsManager.GetPosition("pHome", "RM");
            DescPose pHome = new DescPose(restPose.x, restPose.y, restPose.z, restPose.rx, restPose.ry, restPose.rz);

            // Se si trova nella zona
            if (checker_homeZone.IsInObstruction(pHome, RobotManager.TCPCurrentPosition))
                return true;
            else return false; // Se non si trova nella zona

        }

        /// <summary>
        /// Ritorno a casa del Robot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ClickEvent_GoToHomePosition(object sender, EventArgs e)
        {
            // Se la home routine è già in esecuzione non eseguo il metodo
            if (homeRoutineStarted)
                return;

            // Chiedo conferma per avvio della HomeRoutine
            if (CustomMessageBox.Show(MessageBoxTypeEnum.WARNING, "Posizionare il Robot in Home position?") != DialogResult.OK)
            {
                return;
            }

            // Get del punto di home
            var restPose = ApplicationConfig.applicationsManager.GetPosition("pHome", "RM");
            DescPose pHome = new DescPose(restPose.x, restPose.y, restPose.z, restPose.rx, restPose.ry, restPose.rz);

            bool stopHomeRoutine = false; // Reset della richiesta di stop HomeRoutine
            int stepHomeRoutine = 0; // Reset degli step della HomeRoutine

            RobotManager.ClearRobotQueue();

            RobotManager.robot.RobotEnable(1); // Abilito il Robot
            Thread.Sleep(2000); // Attesa di 2s per stabilizzare il Robot
            homeRoutineStarted = true; // Segnalo che la home routine è partita

            // Rendo il tasto per andare in HomePosition non cliccabile
            btn_homePosition.Enabled = false;
            btn_homePosition.BackColor = SystemColors.ControlDark;
            btn_homePosition.BackgroundImage = null;

            // Avvia un task separato per il ciclo while
            await Task.Run(async () =>
            {
                while (!stopHomeRoutine) // Fino a quando non termino la home routine
                {
                    // Se viene tolta la modalità manuale durante la home routine, questa viene bloccata immediatamente
                    if (RobotManager.mode != 2)
                    {
                        RobotManager.robot.PauseMotion(); // Pausa Robot
                        Thread.Sleep(200); // Leggero delay per stabilizzare il Robot
                        RobotManager.robot.StopMotion(); // Stop del Robot
                        homeRoutineStarted = false; // Reset variabile che indica l'avvio della home routine
                        stopHomeRoutine = true; // Alzo richiesta per terminare metodo che riproduce home routine
                    }
                    else // Altrimenti eseguo la home routine normalmente
                    {
                        switch (stepHomeRoutine)
                        {
                            case 0:
                                #region Cancellazione coda Robot e disattivazione tasti applicazione

                                btn_startApp.Enabled = false;
                                btn_startApp.BackColor = SystemColors.ControlDark;
                                btn_startApp.BackgroundImage = null;

                                btn_stopApp.Enabled = false;
                                btn_stopApp.BackColor = SystemColors.ControlDark;
                                btn_stopApp.BackgroundImage = null;

                                RobotManager.ClearRobotQueue();
                                RobotManager.SetHomeRoutineSpeed();
                                await Task.Delay(1000);

                                stepHomeRoutine = 10;
                                break;

                            #endregion

                            case 10:
                                #region Movimento a punto di home

                                RobotManager.MoveRobotToSafePosition();
                                RobotManager.GoToHomePosition();
                                RobotManager.endingPoint = pHome;

                                stepHomeRoutine = 20;
                                break;

                            #endregion

                            case 20:
                                #region Attesa inPosition home

                                if (RobotManager.inPosition)
                                    stepHomeRoutine = 30;
                                break;

                            #endregion

                            case 30:
                                #region Termine ciclo e riattivazione tasti applicazione e tasto home 
                                RobotManager.ResetHomeRoutineSpeed();

                                homeRoutineStarted = false;

                                btn_homePosition.Enabled = true;
                                btn_homePosition.BackColor = SystemColors.Control;
                                btn_homePosition.BackgroundImage = Resources.home;

                                stopHomeRoutine = true;

                                break;

                                #endregion
                        }
                    }

                    await Task.Delay(100); // Delay routine
                }
            });

            Thread.Sleep(2000); // Attesa di 2s per stabilizzare il Robot

            RobotManager.robot.RobotEnable(0); // Disattivo il Robot
        }


        /// <summary>
        /// Stop applicazione
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_stopApp(object sender, EventArgs e)
        {/*
            RobotManager.stopCycleRoutine = true; // Alzo richiesta per fermare thread di riproduzione ciclo
                                                  // Imposto a 0 (false) Automatic_Start che resetta anche il contatore dello spostamento della catena
            RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");

            // Imposto a 1 (true) Auto_Cycle_End che segnala che il ciclo automatico è terminato
            RefresherTask.AddUpdate(PLCTagName.Auto_Cycle_End, 1, "INT16");

            EnableCycleButton(0);

            RobotManager.currentIndex = -1; // Reset dell'indice corrente della posizione che riproduce il Robot

            RobotManager.robot.PauseMotion(); // Invio comando di pausa al robot
            Thread.Sleep(200); // Leggero ritardo per stabilizzare il robot
            RobotManager.robot.StopMotion(); // Stop Robot con conseguente cancellazione di coda di punti
            */
            RobotManager.stopCycleRequested = true;
        }


        /// <summary>
        ///  Alza richiesta per mettere in pausa il ciclo del Robot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_pauseApp(object sender, EventArgs e)
        {
            RobotManager.pauseCycleRequested = true; // Alzo richiesta di pausa ciclo
            RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16"); // Reset della variabile che fa partire contatore catena
        }
        #endregion

        private void btn_emergencyStop_Click(object sender, EventArgs e)
        {
            RobotManager.ClearRobotAlarm();
            RobotManager.ClearRobotQueue();
            RobotManager.ResetRobotSteps();

            CustomMessageBox.Show(MessageBoxTypeEnum.WARNING_OK, "Riavviare il robot");
        }

        #region Demo

        /// <summary>
        /// A true quando il thread demo è stato avviato
        /// </summary>
        private static bool demoThreadStarted = false;

        /// <summary>
        /// Thread che esegue metodo DemoRoutine
        /// </summary>
        private static Thread demoThread;

        /// <summary>
        /// Avvia il thread di demo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_startDemo_Click(object sender, EventArgs e)
        {
            // Controllo che il thread demo non stia già girando
            if (!demoThreadStarted)
            {
                // Thread a priorità normale
                demoThread = new Thread(new ThreadStart(DemoRoutine));
                demoThread.IsBackground = true;
                demoThread.Priority = ThreadPriority.Normal;
                demoThread.Start();

                demoThreadStarted = true;

                log.Info("Demo avviata");
            }
        }

        /// <summary>
        /// Metodo che esegue la routine di demo
        /// </summary>
        private static async void DemoRoutine()
        {
            #region Variabili necessarie alla routine

            // Parametri ciclo
            int stepDemo = 0;
            int refreshDemoDelay = 50;

            //Riferimenti movimento robot
            ExaxisPos ePos = new ExaxisPos(0,0,0,0);
            DescPose pose = new DescPose(0,0,0,0,0,0);
            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0);

            // Parametri movimento robot
            int tool = 0;
            int user = 0;
            float vel = 5.0f;
            float acc = 100.0f;
            float ovl = 100.0f;
            float blendT = 500.0f;
            byte flag = 0;

            #endregion

            #region Dichiarazione punti necessari alla routine

            var pullPose = ApplicationConfig.applicationsManager.GetPosition("pPull","RM");
            var pickPose = ApplicationConfig.applicationsManager.GetPosition("pPick", "RM");
            var placePose = ApplicationConfig.applicationsManager.GetPosition("pPlace", "RM");
            var placeHome = ApplicationConfig.applicationsManager.GetPosition("pHomeDemo", "RM");

            #region Pull
            
            DescPose pPreApproachPull = new DescPose(pullPose.x - 200.0, pullPose.y, pullPose.z, pullPose.rx, pullPose.ry, pullPose.rz);
            JointPos jPreApproachPull = new JointPos(0, 0, 0, 0, 0, 0);
           
            DescPose pApproachPull = new DescPose(pullPose.x, pullPose.y, pullPose.z, pullPose.rx, pullPose.ry, pullPose.rz);
            DescPose pTakeTray = new DescPose(pApproachPull.tran.x, pApproachPull.tran.y, pApproachPull.tran.z - 30.0, pApproachPull.rpy.rx, pApproachPull.rpy.ry, pApproachPull.rpy.rz);
            DescPose pPull = new DescPose(pTakeTray.tran.x - 320.0, pTakeTray.tran.y, pTakeTray.tran.z, pTakeTray.rpy.rx, pTakeTray.rpy.ry, pTakeTray.rpy.rz);

            #endregion

            #region Pick

            DescPose pRetract1 = new DescPose(pPull.tran.x, pPull.tran.y, pPull.tran.z + 30, pPull.rpy.rx, pPull.rpy.ry, pPull.rpy.rz);
            DescPose pRetract2 = new DescPose(pRetract1.tran.x - 50, pRetract1.tran.y, pRetract1.tran.z, pRetract1.rpy.rx, pRetract1.rpy.ry, pRetract1.rpy.rz);
            DescPose pRetract3 = new DescPose(pRetract2.tran.x, pRetract2.tran.y, pRetract2.tran.z + 100, pRetract2.rpy.rx, pRetract2.rpy.ry, pRetract2.rpy.rz);
            
            DescPose pApproachPick = new DescPose(pickPose.x, pickPose.y - 50, pickPose.z + 100, pickPose.rx, pickPose.ry, pickPose.rz);
            JointPos jApproachPick = new JointPos(0, 0, 0, 0, 0, 0);

            DescPose pPrePick = new DescPose(pApproachPick.tran.x, pApproachPick.tran.y, pApproachPick.tran.z - 100, pApproachPick.rpy.rx, pApproachPick.rpy.ry, pApproachPick.rpy.rz);
            DescPose pPick = new DescPose(pickPose.x, pickPose.y, pickPose.z, pickPose.rx, pickPose.ry, pickPose.rz);
            DescPose pMovePick = new DescPose(pickPose.x - 800, pickPose.y, pickPose.z, pickPose.rx, pickPose.ry, pickPose.rz);

            #endregion

            #region Place

            DescPose pPrePlace = new DescPose(placePose.x, placePose.y, placePose.z + 50, placePose.rx, placePose.ry, placePose.rz);
            DescPose pPlace = new DescPose(placePose.x, placePose.y, placePose.z, placePose.rx, placePose.ry, placePose.rz);
            DescPose pRetractPlace = new DescPose(placePose.x, placePose.y, placePose.z + 80, placePose.rx, placePose.ry, placePose.rz);
            DescPose pPush = new DescPose(pRetractPlace.tran.x, pRetractPlace.tran.y + 100, pRetractPlace.tran.z, pRetractPlace.rpy.rx, pRetractPlace.rpy.ry, pRetractPlace.rpy.rz);
            DescPose pHome = new DescPose(placeHome.x, placeHome.y, placeHome.z, placeHome.rx, placeHome.ry, placeHome.rz);

            #endregion

            #endregion

            while (true)
            {
                switch (stepDemo)
                {
                    case 0:
                        #region Avvicinamento al punto di Pull

                        // Cambio velocità ed accelerazione
                        acc = 100;
                        vel = 90;
                        Thread.Sleep(250);

                        // Movimento in Joint
                        RobotManager.robot.GetInverseKin(0, pPreApproachPull, -1, ref jPreApproachPull);
                        int ret = RobotManager.robot.GetForwardKin(jPreApproachPull, ref pose);
                        RobotManager.robot.MoveJ(jPreApproachPull, pose, tool, user, vel, acc, ovl, ePos, blendT, flag, offset);

                        // Assegno l'ending point
                        RobotManager.inPosition = false;
                        RobotManager.endingPoint = pPreApproachPull;
                        stepDemo = 5;

                        break;

                    #endregion

                    case 1:
                        #region Attesa inPosition

                        if (RobotManager.inPosition)
                        {
                            vel = 20;

                            stepDemo = 5;
                        }
                        break;

                    #endregion

                    case 5:
                        #region Posizionamento sopra la teglia

                        vel = 20;
                        RobotManager.robot.MoveCart(pApproachPull, tool, user, vel, acc, ovl, blendT, RobotManager.config);
                        stepDemo = 10;
                        
                        break;

                    #endregion

                    case 10:
                        #region Presa della teglia

                        vel = 10;

                        RobotManager.robot.MoveCart(pTakeTray, tool, user, vel, acc, ovl, blendT, RobotManager.config);

                        // Assegnazione ending point
                        RobotManager.inPosition = false;
                        RobotManager.endingPoint = pTakeTray;
                        stepDemo = 20;

                        break;

                    #endregion

                    case 20:
                        #region Attesa inPosition

                        if (RobotManager.inPosition)
                        {
                            await Task.Delay(250);
                            vel = 70;
                            stepDemo = 30;
                        }

                        break;

                    #endregion

                    case 30:
                        #region Pull della teglia
                        
                        RobotManager.robot.MoveCart(pPull, tool, user, vel, acc, ovl, blendT, RobotManager.config);

                        // Assegnazione ending point
                        RobotManager.inPosition = false;
                        RobotManager.endingPoint = pPull;
                        
                        stepDemo = 50;

                        break;

                    #endregion

                    case 35:
                        #region Attesa inPosition

                        if (RobotManager.inPosition)
                            stepDemo = 50;

                        break;

                    #endregion

                    case 50:
                        #region Spostamento dalla teglia

                        vel = 8;

                        RobotManager.robot.MoveCart(pRetract1, tool, user, vel, acc, ovl, blendT, RobotManager.config);
                        RobotManager.robot.MoveCart(pRetract2, tool, user, vel, acc, ovl, blendT, RobotManager.config);
                        RobotManager.robot.MoveCart(pRetract3, tool, user, vel, acc, ovl, blendT, RobotManager.config);
                        stepDemo = 55;

                        break;

                    #endregion

                    case 55:
                        #region Movimento al punto di pick

                        vel = 90;

                        // Movimento in joint al punto di avvicinamento Pick
                        RobotManager.robot.GetInverseKin(0, pApproachPick, -1, ref jApproachPick);
                        ret = RobotManager.robot.GetForwardKin(jApproachPick, ref pose);
                        RobotManager.robot.MoveJ(jApproachPick, pose, tool, user, vel, acc, ovl, ePos, blendT, flag, offset);

                        vel = 70;

                        RobotManager.robot.MoveCart(pPick, tool, user, vel, acc, ovl, blendT, RobotManager.config);
                        
                        // Assegnazione ending point
                        RobotManager.inPosition = false;
                        RobotManager.endingPoint = pPick;
                        stepDemo = 120;
                        break;

                    #endregion

                    case 120:
                        #region Attesa inPosition

                        if (RobotManager.inPosition)
                        {
                            await Task.Delay(500);
                            stepDemo = 130;
                        }
                        break;

                    #endregion

                    case 130:
                        #region Estrazione teglia

                        RobotManager.robot.MoveCart(pMovePick, tool, user, vel, acc, ovl, blendT, RobotManager.config);
                        
                        // Assegnazione ending point
                        RobotManager.inPosition = false;
                        RobotManager.endingPoint = pMovePick;

                        stepDemo = 150;

                        break;

                    #endregion

                    case 140:
                        #region Attesa inPosition

                        if (RobotManager.inPosition)
                        {
                            vel = 70;
                            stepDemo = 150;
                           
                        }

                        break;

                    #endregion

                    case 150:
                        #region Movimento a punto di place

                        RobotManager.robot.MoveCart(pPrePlace, tool, user, vel, acc, ovl, blendT, RobotManager.config);

                        vel = 5;

                        RobotManager.robot.MoveCart(pPlace, tool, user, vel, acc, ovl, blendT, RobotManager.config);
                        
                        // Assegnazione ending point
                        RobotManager.inPosition = false;
                        RobotManager.endingPoint = pPlace;

                        stepDemo = 155;

                        break;

                    #endregion

                    case 155:
                        #region Attesa inPosition

                        if (RobotManager.inPosition)
                        {
                            await Task.Delay(500);
                            vel = 20;
                            stepDemo = 160;
                        }

                        break;

                    #endregion

                    case 160:
                        #region Place
                       
                        RobotManager.robot.MoveCart(pRetractPlace, tool, user, vel, acc, ovl, blendT, RobotManager.config);
                        // Scommentando questa riga di codice il robot esegue anche la spinta della teglia dopo il place
                        //RobotManager.robot.MoveCart(pPush, RobotManager.tool, RobotManager.user, vel, RobotManager.acc, RobotManager.ovl, RobotManager.blendT, RobotManager.config);

                        acc = 100;
                        vel = 100;
                       // Thread.Sleep(250);
                        RobotManager.robot.MoveCart(pHome, tool, user, vel, acc, ovl, blendT, RobotManager.config);

                        // Assegnazione ending point
                        RobotManager.inPosition = false;
                        RobotManager.endingPoint = pHome;
                        stepDemo = 200;

                        break;

                    #endregion

                    case 200:
                        #region Attesa inPosition e riavvio ciclo

                        if (RobotManager.inPosition)
                        {
                            stepDemo = 0;
                        }
                        break;

                        #endregion
                }
                await Task.Delay(refreshDemoDelay);
            }
        }

        #endregion

        private void ClickEvent_openCleanPage(object sender, EventArgs e)
        {
            #region Punto start

            JointPos jointStart = new JointPos(0, 0, 0, 0, 0, 0);
            var start = ApplicationConfig.applicationsManager.GetPosition("Start", "RM");
            DescPose descPosStart = new DescPose(start.x, start.y, start.z, start.rx, start.ry, start.rz);
            RobotManager.robot.GetInverseKin(0, descPosStart, -1, ref jointStart);

            #endregion

            #region Angolo2

            // ----- Punto StartAngolo2  -----
            JointPos jointStartAngolo2 = new JointPos(0, 0, 0, 0, 0, 0);
            var startAngolo2 = ApplicationConfig.applicationsManager.GetPosition("StartAngolo2", "RM");
            DescPose descPosStartAngolo2 = new DescPose(startAngolo2.x, startAngolo2.y, startAngolo2.z, startAngolo2.rx, startAngolo2.ry, startAngolo2.rz);
            RobotManager.robot.GetInverseKin(0, descPosStartAngolo2, -1, ref jointStartAngolo2);

            // ----- Punto IntAngolo2  -----
            JointPos jointIntAngolo2 = new JointPos(0, 0, 0, 0, 0, 0);
            var IntAngolo2 = ApplicationConfig.applicationsManager.GetPosition("IntAngolo2", "RM");
            DescPose descPosIntAngolo2 = new DescPose(IntAngolo2.x, IntAngolo2.y, IntAngolo2.z, IntAngolo2.rx, IntAngolo2.ry, IntAngolo2.rz);
            RobotManager.robot.GetInverseKin(0, descPosIntAngolo2, -1, ref jointIntAngolo2);

            // ----- Punto EndAngolo2  -----
            JointPos jointEndAngolo2 = new JointPos(0, 0, 0, 0, 0, 0);
            var endAngolo2 = ApplicationConfig.applicationsManager.GetPosition("EndAngolo2", "RM");
            DescPose descPosEndAngolo2 = new DescPose(endAngolo2.x, endAngolo2.y, endAngolo2.z, endAngolo2.rx, endAngolo2.ry, endAngolo2.rz);
            RobotManager.robot.GetInverseKin(0, descPosEndAngolo2, -1, ref jointEndAngolo2);

            #endregion

            #region Angolo3

            // ----- Punto StartAngolo3  -----
            JointPos jointStartAngolo3 = new JointPos(0, 0, 0, 0, 0, 0);
            var startAngolo3 = ApplicationConfig.applicationsManager.GetPosition("StartAngolo3", "RM");
            DescPose descPosStartAngolo3 = new DescPose(startAngolo3.x, startAngolo3.y, startAngolo3.z, startAngolo3.rx, startAngolo3.ry, startAngolo3.rz);
            RobotManager.robot.GetInverseKin(0, descPosStartAngolo3, -1, ref jointStartAngolo3);

            // ----- Punto IntAngolo3  -----
            JointPos jointIntAngolo3 = new JointPos(0, 0, 0, 0, 0, 0);
            var IntAngolo3 = ApplicationConfig.applicationsManager.GetPosition("IntAngolo3", "RM");
            DescPose descPosIntAngolo3 = new DescPose(IntAngolo3.x, IntAngolo3.y, IntAngolo3.z, IntAngolo3.rx, IntAngolo3.ry, IntAngolo3.rz);
            RobotManager.robot.GetInverseKin(0, descPosIntAngolo3, -1, ref jointIntAngolo3);

            // ----- Punto EndAngolo3  -----
            JointPos jointEndAngolo3 = new JointPos(0, 0, 0, 0, 0, 0);
            var endAngolo3 = ApplicationConfig.applicationsManager.GetPosition("EndAngolo3", "RM");
            DescPose descPosEndAngolo3 = new DescPose(endAngolo3.x, endAngolo3.y, endAngolo3.z, endAngolo3.rx, endAngolo3.ry, endAngolo3.rz);
            RobotManager.robot.GetInverseKin(0, descPosEndAngolo3, -1, ref jointEndAngolo3);

            #endregion

            #region Angolo4

            // ----- Punto StartAngolo4  -----
            JointPos jointStartAngolo4 = new JointPos(0, 0, 0, 0, 0, 0);
            var startAngolo4 = ApplicationConfig.applicationsManager.GetPosition("StartAngolo4", "RM");
            DescPose descPosStartAngolo4 = new DescPose(startAngolo4.x, startAngolo4.y, startAngolo4.z, startAngolo4.rx, startAngolo4.ry, startAngolo4.rz);
            RobotManager.robot.GetInverseKin(0, descPosStartAngolo4, -1, ref jointStartAngolo4);

            // ----- Punto IntAngolo4  -----
            JointPos jointIntAngolo4 = new JointPos(0, 0, 0, 0, 0, 0);
            var IntAngolo4 = ApplicationConfig.applicationsManager.GetPosition("IntAngolo4", "RM");
            DescPose descPosIntAngolo4 = new DescPose(IntAngolo4.x, IntAngolo4.y, IntAngolo4.z, IntAngolo4.rx, IntAngolo4.ry, IntAngolo4.rz);
            RobotManager.robot.GetInverseKin(0, descPosIntAngolo4, -1, ref jointIntAngolo4);

            // ----- Punto EndAngolo4  -----
            JointPos jointEndAngolo4 = new JointPos(0, 0, 0, 0, 0, 0);
            var endAngolo4 = ApplicationConfig.applicationsManager.GetPosition("EndAngolo4", "RM");
            DescPose descPosEndAngolo4 = new DescPose(endAngolo4.x, endAngolo4.y, endAngolo4.z, endAngolo4.rx, endAngolo4.ry, endAngolo4.rz);
            RobotManager.robot.GetInverseKin(0, descPosEndAngolo4, -1, ref jointEndAngolo4);

            #endregion

            #region Punto end

            JointPos jointEnd = new JointPos(0, 0, 0, 0, 0, 0);
            var end = ApplicationConfig.applicationsManager.GetPosition("End", "RM");
            DescPose descPosEnd = new DescPose(end.x, end.y, end.z, end.rx, end.ry, end.rz);
            RobotManager.robot.GetInverseKin(0, descPosEnd, -1, ref jointEnd);

            #endregion

            #region Parametri movimento

            int tool = RobotManager.tool;       // Utensile selezionato
            int user = RobotManager.user;       // Sistema di coordinate del pezzo
            float vel = RobotManager.vel;  // Velocità del movimento (%)
            float acc = RobotManager.acc;  // Accelerazione (%)
            float ovl = RobotManager.ovl; // Override della velocità (%)
            float blendR = RobotManager.blendT; // Nessun blending (movimento bloccante)
            ExaxisPos epos = new ExaxisPos(0, 0, 0, 0); // Nessun asse esterno
            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset
            byte offsetFlag = 0; // Flag per offset (0 = disabilitato)

            #endregion

            // Movimento al punto di start
            RobotManager.robot.MoveCart(descPosStart, tool, user, vel, acc, ovl, blendR, RobotManager.config);

            // Movimento al punto di start di angolo2
            RobotManager.robot.MoveCart(descPosStartAngolo2, tool, user, vel, acc, ovl, blendR, RobotManager.config);

            // Movimento attorno all'angolo2
            int err = RobotManager.robot.MoveC(
               jointIntAngolo2, descPosIntAngolo2, tool, user, vel, acc, epos, offsetFlag, offset,
               jointEndAngolo2, descPosEndAngolo2, tool, user, vel, acc, epos, offsetFlag, offset,
               ovl, blendR
           );

            // Movimento al punto di start di angolo3
            RobotManager.robot.MoveCart(descPosStartAngolo3, tool, user, vel, acc, ovl, blendR, RobotManager.config);

            // Movimento attorno all'angolo3
            err = RobotManager.robot.MoveC(
               jointIntAngolo3, descPosIntAngolo3, tool, user, vel, acc, epos, offsetFlag, offset,
               jointEndAngolo3, descPosEndAngolo3, tool, user, vel, acc, epos, offsetFlag, offset,
               ovl, blendR
           );

            // Movimento al punto di start di angolo4
            RobotManager.robot.MoveCart(descPosStartAngolo4, tool, user, vel, acc, ovl, blendR, RobotManager.config);

            // Movimento attorno all'angolo4
            err = RobotManager.robot.MoveC(
               jointIntAngolo4, descPosIntAngolo4, tool, user, vel, acc, epos, offsetFlag, offset,
               jointEndAngolo4, descPosEndAngolo4, tool, user, vel, acc, epos, offsetFlag, offset,
               ovl, blendR
           );

            // Movimento al punto di end
            RobotManager.robot.MoveCart(descPosEnd, tool, user, vel, acc, ovl, blendR, RobotManager.config);



        }

        private void ClickEvent_openWeldingParameters(object sender, EventArgs e)
        {
            UC_weldingParameters uc;
            if (!FormHomePage.Instance.PnlContainer.Controls.ContainsKey("UC_weldingParameters"))
            {
                uc = new UC_weldingParameters()
                {
                    Dock = DockStyle.Fill
                };
                FormHomePage.Instance.PnlContainer.Controls.Add(uc);
            }
            FormHomePage.Instance.PnlContainer.Controls["UC_weldingParameters"].BringToFront();

            uc = (UC_weldingParameters)FormHomePage.Instance.PnlContainer.Controls["UC_weldingParameters"];
            //uc.ShowLoadingScreen();
        }

        private void ClickEvent_testFunction(object sender, EventArgs e)
        {
            RobotManager.robot.RobotEnable(1);
        }

        private void ClickEvent_openGripper(object sender, EventArgs e)
        {
            int gripperStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.GripperStatusIn)); // lettura

            if (gripperStatus == 0) // se è 0 scrivo 1
            {
                RefresherTask.AddUpdate(PLCTagName.GripperStatusOut, 1, "INT16");
            }
            else // se è 1 scrivo 0
            {
                RefresherTask.AddUpdate(PLCTagName.GripperStatusOut, 0, "INT16");
            }
        }

        private void ClickEvent_restoreScreenSaverManager(object sender, EventArgs e)
        {
            FormHomePage.Instance.ScreenSaverManagerForm.RestoreLocation();
        }

        private void ClickEvent_stopRobot(object sender, EventArgs e)
        {
            RobotManager.stopCycleRoutine = true; // Alzo richiesta per fermare thread di riproduzione ciclo
                                                  // Imposto a 0 (false) Automatic_Start che resetta anche il contatore dello spostamento della catena
            RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");

            // Imposto a 1 (true) Auto_Cycle_End che segnala che il ciclo automatico è terminato
            RefresherTask.AddUpdate(PLCTagName.Auto_Cycle_End, 1, "INT16");

            EnableCycleButton(0);

            RobotManager.currentIndex = -1; // Reset dell'indice corrente della posizione che riproduce il Robot

            RobotManager.robot.PauseMotion(); // Invio comando di pausa al robot
            Thread.Sleep(200); // Leggero ritardo per stabilizzare il robot
            RobotManager.robot.StopMotion(); // Stop Robot con conseguente cancellazione di coda di punti
        }

        private void ClickEvent_rallentaRobot(object sender, EventArgs e)
        {

        }
    }
}
