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
using RM.src.RM250728.Forms.Plant;
using RM.Properties;
using System.Drawing;
using RMLib.VATView;
using fairino;
using RM.src.RM250728.Classes.PLC;
using RM.src.RM250728.Forms.Plant.DragMode;
using RM.src.RM250728.Forms.ScreenSaver;
using RM.src.RM250728.Classes.FR20;
using System.Drawing.Drawing2D;

namespace RM.src.RM250728
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

        // Variabili per aumentare o decrementare la velocità del Robot
        private readonly System.Windows.Forms.Timer continuousAddSpeedTimer;
        private readonly System.Windows.Forms.Timer continuousRemoveSpeedTimer;
        private bool isMouseDown = false;

        /// <summary>
        /// Indice della posizione da selezionare su lw_positions
        /// </summary>
        public static int index = 0;

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

            
            RobotManager.EnableButtonHome += RobotManager_EnableButtonHome;

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
            //Task.Run(() => ApplyPanelTransparency());

            InitRobotModeButtons();

            ScreenSaverManager.AutoAddClickEvents(this);
        }

        private void GripperStatusOFFEvent(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                pnl_pinzeStatus.BackColor = Color.SeaGreen;
            });
        }

        private void GripperStatusONEvent(object sender, EventArgs e) 
        {
            Invoke((MethodInvoker)delegate
            {
                pnl_pinzeStatus.BackColor = Color.Firebrick;
            });
        }

        private void RobotInHomePositionEvent(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                pnl_homeStatus.BackColor = Color.SeaGreen;
            });
        }

        private void RobotNotInHomePositionEvent(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                pnl_homeStatus.BackColor = Color.Firebrick;
            });
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
            Invoke((MethodInvoker)delegate
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
            });
        }

        private void ChangeLblVelocity(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                lbl_velocity.Text = sender.ToString();
            });
        }

        private void RobotManager_EnableButtonHome(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                if (Convert.ToInt16(sender) == 1)
                {
                    btn_homePosition.Enabled = true;
                    btn_homePosition.BackColor = SystemColors.Control;
                    btn_homePosition.BackgroundImage = Resources.home;
                }
                else if (Convert.ToInt16(sender) == 0)
                {
                    btn_homePosition.Enabled = false;
                    btn_homePosition.BackColor = SystemColors.ControlDark;
                    btn_homePosition.BackgroundImage = null;
                }
            });
        }

        /// <summary>
        /// Gestisce l'attivazione e disattivazione dei tasti start, stop e pausa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RobotManager_EnableButtonCycleEvent(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
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
            });  
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
            //Invoke(new Action(() => ResumeLayout()));
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
        /// Evento generato quando ci si logga con un account di livello 3 o superiore
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRMLogin(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                //pnl_buttonVAT.Visible = true;
                btn_VAT.Visible = true;
                lbl_buttonVAT.Visible = true;
            });
        }

        /// <summary>
        /// Evento generato quando si fa il logout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRMLogout(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                //pnl_buttonVAT.Visible = false;
                btn_VAT.Visible = false;
                lbl_buttonVAT.Visible = false;
                VATViewManager.CloseAll();
            });
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
        /// Avvio dell'applicazione
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ClickEvent_startApp(object sender, EventArgs e)
        {
            log.Info("Richiesta di avvio ciclo");

            byte ris = 0;
            RobotManager.robot.GetDI(0, 1, ref ris);

            if (ris == 0)
            {
                CustomMessageBox.Show(MessageBoxTypeEnum.ERROR, "Pinza chiusa. Impossibile avviare il ciclo.");
                return;
            }

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

             //Start ciclo  --> bisogna scrivere su PLC?
             //await RobotManager.MainCycle();
             //RobotManager.taskManager.AddAndStartTask(nameof(RobotManager.MainCycle), RobotManager.MainCycle, TaskType.Default, false);
             RefresherTask.AddUpdate(PLCTagName.Hmi_startCycle, 1, "INT16");

            RobotManager.taskManager.AddAndStartTask(RobotManager.TaskPickAndPlaceTeglia3, RobotManager.PickAndPlaceTeglia3, TaskType.Default, false);
            RobotManager_EnableButtonCycleEvent(0,EventArgs.Empty);
        }

        /// <summary>
        /// Ritorno a casa del Robot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_GoToHomePosition(object sender, EventArgs e)
        {
            /*
            // Se il ciclo di main è in esecuzione non eseguo il metodo
            if (RobotManager.CycleRun_Main != 0)
                return;

            // Se la home routine è già in esecuzione non eseguo il metodo
            if (RobotManager.CycleRun_Home != 0)
                return;
            */

            // Chiedo conferma per avvio della HomeRoutine
            if (CustomMessageBox.Show(MessageBoxTypeEnum.WARNING, "Posizionare il Robot in Home position?") != DialogResult.OK)
            {
                return;
            }

            RobotManager.taskManager.AddAndStartTask(RobotManager.TaskHomeRoutine, RobotManager.HomeRoutine, TaskType.Default, false);
        }

        /// <summary>
        /// Stop applicazione
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_stopApp(object sender, EventArgs e)
        {
            // Alzo richiesta di stop ciclo
            RobotManager.stopCycleRequested = true;
        }

        /// <summary>
        ///  Alza richiesta per mettere in pausa il ciclo del Robot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEvent_pauseApp(object sender, EventArgs e)
        {
            //RobotManager.pauseCycleRequested = true; // Alzo richiesta di pausa ciclo
            //RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16"); // Reset della variabile che fa partire contatore catena
            RefresherTask.AddUpdate(PLCTagName.MovePause, 1, "INT16");
        }

        #endregion

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

        private void ClickEvent_openGripper(object sender, EventArgs e)
        {
            // scrittura su uscite digitali robot
            RobotManager.robot.SetDO(0, 0, 0, 0);
        }

        private void ClickEvent_restoreScreenSaverManager(object sender, EventArgs e)
        {
            //FormHomePage.Instance.ScreenSaverManagerForm.RestoreLocation();
        }

        private void ClickEvent_stopRobot(object sender, EventArgs e)
        {
            RobotManager.stopCycleRoutine = true; // Alzo richiesta per fermare thread di riproduzione ciclo
                                                  // Imposto a 0 (false) Automatic_Start che resetta anche il contatore dello spostamento della catena
           // RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");

            // Imposto a 1 (true) Auto_Cycle_End che segnala che il ciclo automatico è terminato
        //RefresherTask.AddUpdate(PLCTagName.Auto_Cycle_End, 1, "INT16");

            EnableCycleButton(0);

            RobotManager.currentIndex = -1; // Reset dell'indice corrente della posizione che riproduce il Robot

            RobotManager.robot.PauseMotion(); // Invio comando di pausa al robot
            Thread.Sleep(200); // Leggero ritardo per stabilizzare il robot
            RobotManager.robot.StopMotion(); // Stop Robot con conseguente cancellazione di coda di punti
        }

        private void btn_pauseRobot_Click(object sender, EventArgs e)
        {
            
            ROBOT_STATE_PKG robotState = new ROBOT_STATE_PKG();

            RobotManager.robot.GetRobotRealTimeState(ref robotState);
            MessageBox.Show(robotState.robot_state.ToString());

            return;
            

            if (!RobotManager.robotIsPaused) // Se il robot non è già in pausa
            {
                RobotManager.robot.PauseMotion(); // Metto in pausa il robot
                RobotManager.robotIsPaused = true; // Imposto a true il booleano che segnala che il robot è in pausa
            }
        }

        private void btn_resumeRobot_Click(object sender, EventArgs e)
        {
            if (RobotManager.robotIsPaused) // Se il robot era in pausa
            {
                RobotManager.robot.ResumeMotion();
                RobotManager.robotIsPaused = false; // Imposto a false il booleano che segnala che il robot è in pausa
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int movementResult; // Risultato del movimento del Robot

            #region Punto home

            var home = ApplicationConfig.applicationsManager.GetPosition("1", "RM");
            DescPose descPosHome = new DescPose(home.x, home.y, home.z, home.rx, home.ry, home.rz);

            #endregion

            #region Punto di pick

            //int xOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Pick_X));
            //int yOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Pick_Y));
            //int zOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Pick_Z));
            int xOffset = 0;
            int yOffset = 0;
            int zOffset = 0;

            JointPos jointPosPick = new JointPos(0, 0, 0, 0, 0, 0);
            var pick = ApplicationConfig.applicationsManager.GetPosition((Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_SelectedFormat)) % 1000).ToString(), "RM");
            DescPose descPosPick = new DescPose(pick.x + xOffset, pick.y + yOffset, pick.z + zOffset, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPick, -1, ref jointPosPick);

            #endregion

            #region Punto avvicinamento pick

            JointPos jointPosApproachPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPick = new DescPose(pick.x, pick.y, pick.z + 200, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPick, -1, ref jointPosApproachPick);

            #endregion

            #region Parametri movimento

            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset
            ExaxisPos epos = new ExaxisPos(0, 0, 0, 0); // Nessun asse esterno
            byte offsetFlag = 0; // Flag per offset (0 = disabilitato)


            // Invio punto di avvicinamento Pick
            movementResult = RobotManager.robot.MoveCart(descPosApproachPick, RobotManager.tool, RobotManager.user, RobotManager.vel,
                RobotManager.acc, RobotManager.ovl, RobotManager.blendT, RobotManager.config);

            //movementResult = robot.MoveCart(descPosPick, tool, user, vel, acc, ovl, blendT, config);
            //GetRobotMovementCode(movementResult); // Get del risultato del movimento del robot

            

            #endregion
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int movementResult; // Risultato del movimento del Robot
            /*
            #region Punto home

            var home = ApplicationConfig.applicationsManager.GetPosition("1", "RM");
            DescPose descPosHome = new DescPose(home.x, home.y, home.z, home.rx, home.ry, home.rz);

            #endregion

            #region Punto di pick

            //int xOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Pick_X));
            //int yOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Pick_Y));
            //int zOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Pick_Z));
            int xOffset = 0;
            int yOffset = 0;
            int zOffset = 0;

            JointPos jointPosPick = new JointPos(0, 0, 0, 0, 0, 0);
            var pick = ApplicationConfig.applicationsManager.GetPosition((Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_SelectedFormat)) % 1000).ToString(), "RM");
            DescPose descPosPick = new DescPose(pick.x + xOffset, pick.y + yOffset, pick.z + zOffset, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPick, -1, ref jointPosPick);

            #endregion

            #region Punto avvicinamento pick

            JointPos jointPosApproachPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPick = new DescPose(pick.x, pick.y, pick.z + 200, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPick, -1, ref jointPosApproachPick);

            #endregion

            #region Parametri movimento

            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset
            ExaxisPos epos = new ExaxisPos(0, 0, 0, 0); // Nessun asse esterno
            byte offsetFlag = 0; // Flag per offset (0 = disabilitato)

            // Invio punto di pick
            movementResult = RobotManager.robot.MoveL(jointPosPick, descPosPick, RobotManager.tool, RobotManager.user, RobotManager.vel,
                RobotManager.acc, RobotManager.ovl, RobotManager.blendT, epos, 0, offsetFlag, offset);
            #endregion
            */


            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset
            ExaxisPos epos = new ExaxisPos(0, 0, 0, 0); // Nessun asse esterno
            byte offsetFlag = 0; // Flag per offset (0 = disabilitato)

            DescPose actualPose = RobotManager.TCPCurrentPosition;
            DescPose targetPose = actualPose;
            JointPos jointTargetPose = new JointPos(0, 0, 0, 0, 0, 0);

            targetPose.tran.z += 200;

            RobotManager.robot.GetInverseKin(0, targetPose, -1, ref jointTargetPose);

            // Invio punto di pick
            movementResult = RobotManager.robot.MoveL(jointTargetPose, targetPose, RobotManager.tool, RobotManager.user, RobotManager.vel,
                RobotManager.acc, RobotManager.ovl, RobotManager.blendT, epos, 0, offsetFlag, offset);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var restPose = ApplicationConfig.applicationsManager.GetPosition("1", "RM");
            DescPose pHome = new DescPose(restPose.x, restPose.y, restPose.z, restPose.rx, restPose.ry, restPose.rz);

            int result = RobotManager.robot.MoveCart(pHome, RobotManager.tool, RobotManager.user, RobotManager.vel,
                RobotManager.acc, RobotManager.ovl, RobotManager.blendT, RobotManager.config);
        }

        private void button4_Click(object sender, EventArgs e)
        {
           RobotManager.robot.SetDO(0, 1, 0, 0);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            RobotManager.robot.SetDO(0, 0, 0, 0); // Apro la pinza

        }

        private void button6_Click(object sender, EventArgs e)
        {
            byte ris = 0;
            RobotManager.robot.GetDI(0, 1, ref ris);
            MessageBox.Show(ris.ToString());
        }
    } 
}
